using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Auth;

public record RefrescarTokenCommand : IRequest<Result<LoginResponse>>;

/// <summary>
/// Reemite el JWT de la persona autenticada con sus claims (fincaIds, niveles de acceso) al día.
/// Necesario porque esos claims se fijan en el token al iniciar sesión: cuando un Admin crea su
/// propia finca (autoservicio) o se le cambia el acceso a una finca, su sesión actual queda
/// desactualizada hasta que se reemite el token — evita forzar un logout/login manual.
/// </summary>
public class RefrescarTokenCommandHandler : IRequestHandler<RefrescarTokenCommand, Result<LoginResponse>>
{
	private readonly ITokenService _tokenService;
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;

	public RefrescarTokenCommandHandler(ITokenService tokenService, IFarmControlDbContext context, ICurrentUserContext currentUser)
	{
		_tokenService = tokenService;
		_context = context;
		_currentUser = currentUser;
	}

	public async Task<Result<LoginResponse>> Handle(RefrescarTokenCommand request, CancellationToken cancellationToken)
	{
		Result<LoginResponse> result;

		if (_currentUser.PersonaId is not int personaId)
		{
			result = Result<LoginResponse>.Failure("No aplica para este tipo de sesión.");
			return result;
		}

		var persona = await _context.Personas
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(p => p.Id == personaId, cancellationToken);

		if (persona is null || string.IsNullOrEmpty(persona.NombreUsuario))
		{
			result = Result<LoginResponse>.Failure("Usuario no encontrado.");
			return result;
		}

		var asignaciones = await _context.PersonasFincas
			.IgnoreQueryFilters()
			.Where(pf => pf.PersonaId == persona.Id)
			.Select(pf => new { pf.FincaId, pf.SoloLectura, pf.PuedeEliminar })
			.ToListAsync(cancellationToken);

		var fincaIds = asignaciones.Select(a => a.FincaId).ToList();
		var fincaIdsSoloLectura = persona.Activo
			? asignaciones.Where(a => a.SoloLectura).Select(a => a.FincaId).ToList()
			: fincaIds;
		var fincaIdsSinEliminar = persona.Activo
			? asignaciones.Where(a => !a.PuedeEliminar).Select(a => a.FincaId).ToList()
			: fincaIds;

		var token = _tokenService.GenerateToken(
			persona.NombreUsuario, persona.TipoPersona.ToString(), persona.Id,
			persona.ClienteId, fincaIds, persona.Activo, fincaIdsSoloLectura, fincaIdsSinEliminar);

		result = Result<LoginResponse>.Success(new LoginResponse(token, persona.TipoPersona.ToString()));
		return result;
	}
}
