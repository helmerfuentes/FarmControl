using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Auth;

public record LoginCommand(string Usuario, string Contrasena) : IRequest<Result<LoginResponse>>;

public record LoginResponse(string Token, string Rol);

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
	private static readonly Dictionary<string, (string Hash, string Rol)> _USUARIOS_SISTEMA = new()
	{
		["admin"]   = ("admin123",   "SuperAdmin"),
		["auditor"] = ("auditor123", "Auditor")
	};

	private readonly ITokenService _tokenService;
	private readonly IFarmControlDbContext _context;

	public LoginCommandHandler(ITokenService tokenService, IFarmControlDbContext context)
	{
		_tokenService = tokenService;
		_context = context;
	}

	public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
	{
		Result<LoginResponse> result;

		if (_USUARIOS_SISTEMA.TryGetValue(request.Usuario, out var credenciales) && credenciales.Hash == request.Contrasena)
		{
			var token = _tokenService.GenerateToken(request.Usuario, credenciales.Rol);
			result = Result<LoginResponse>.Success(new LoginResponse(token, credenciales.Rol));
			return result;
		}

		var persona = await _context.Personas
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(p => p.NombreUsuario == request.Usuario, cancellationToken);

		if (persona is null || string.IsNullOrEmpty(persona.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Contrasena, persona.PasswordHash))
		{
			result = Result<LoginResponse>.Failure("Usuario o contraseña incorrectos.");
			return result;
		}

		var clienteActivo = await _context.Clientes
			.Where(c => c.Id == persona.ClienteId)
			.Select(c => c.Activo)
			.FirstOrDefaultAsync(cancellationToken);

		if (!clienteActivo)
		{
			result = Result<LoginResponse>.Failure("El servicio de tu cliente no está activo. Contacta al administrador.");
			return result;
		}

		var asignaciones = await _context.PersonasFincas
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

		var personaToken = _tokenService.GenerateToken(
			persona.NombreUsuario!, persona.TipoPersona.ToString(), persona.Id,
			persona.ClienteId, fincaIds, persona.Activo, fincaIdsSoloLectura, fincaIdsSinEliminar);
		result = Result<LoginResponse>.Success(new LoginResponse(personaToken, persona.TipoPersona.ToString()));
		return result;
	}
}
