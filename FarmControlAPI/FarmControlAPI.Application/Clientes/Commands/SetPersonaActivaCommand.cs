using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Commands;

public record SetPersonaActivaCommand(int PersonaId, bool Activa) : IRequest<Result<bool>>;

public class SetPersonaActivaCommandHandler : IRequestHandler<SetPersonaActivaCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public SetPersonaActivaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(SetPersonaActivaCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var persona = await _context.Personas
			.FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

		if (persona is null)
		{
			result = Result<bool>.Failure($"Persona con Id {request.PersonaId} no encontrada.");
			return result;
		}

		if (!_currentUser.TieneAccesoGlobal && persona.TipoPersona == Domain.Enums.TipoPersona.Admin)
		{
			result = Result<bool>.Failure("No puede deshabilitar una cuenta de administrador. Contacta al proveedor del servicio.");
			return result;
		}

		persona.Activo = request.Activa;
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			request.Activa ? "Usuario habilitado" : "Usuario deshabilitado",
			$"'{persona.Nombre}' ({persona.NombreUsuario}) quedó {(request.Activa ? "habilitado" : "deshabilitado, en solo lectura en todas sus fincas")}.",
			persona.ClienteId,
			cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
