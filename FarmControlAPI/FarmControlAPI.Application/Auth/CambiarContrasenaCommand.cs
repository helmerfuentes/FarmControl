using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Auth;

public record CambiarContrasenaCommand(string ContrasenaActual, string ContrasenaNueva) : IRequest<Result<bool>>;

public class CambiarContrasenaCommandHandler : IRequestHandler<CambiarContrasenaCommand, Result<bool>>
{
	private const int _LONGITUD_MINIMA = 6;

	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CambiarContrasenaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(CambiarContrasenaCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		if (_currentUser.PersonaId is null)
		{
			result = Result<bool>.Failure("Esta cuenta no puede cambiar su contraseña desde aquí.");
			return result;
		}

		if (string.IsNullOrWhiteSpace(request.ContrasenaNueva) || request.ContrasenaNueva.Length < _LONGITUD_MINIMA)
		{
			result = Result<bool>.Failure($"La nueva contraseña debe tener al menos {_LONGITUD_MINIMA} caracteres.");
			return result;
		}

		var persona = await _context.Personas
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(p => p.Id == _currentUser.PersonaId.Value, cancellationToken);

		if (persona is null || string.IsNullOrEmpty(persona.PasswordHash))
		{
			result = Result<bool>.Failure("No se pudo verificar la cuenta.");
			return result;
		}

		if (!BCrypt.Net.BCrypt.Verify(request.ContrasenaActual, persona.PasswordHash))
		{
			result = Result<bool>.Failure("La contraseña actual no es correcta.");
			return result;
		}

		persona.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.ContrasenaNueva);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Contraseña cambiada",
			$"'{persona.Nombre}' cambió su propia contraseña.",
			persona.ClienteId,
			cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
