using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Commands;

public record SetPermisoFincaCommand(int PersonaId, int FincaId, bool SoloLectura, bool PuedeEliminar) : IRequest<Result<bool>>;

public class SetPermisoFincaCommandHandler : IRequestHandler<SetPermisoFincaCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public SetPermisoFincaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(SetPermisoFincaCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		if (!_currentUser.TieneAccesoGlobal && !_currentUser.FincaIds.Contains(request.FincaId))
		{
			result = Result<bool>.Failure("No tiene acceso a esta finca.");
			return result;
		}

		var persona = await _context.Personas
			.FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);
		if (persona is null)
		{
			result = Result<bool>.Failure($"Persona con Id {request.PersonaId} no encontrada.");
			return result;
		}

		var finca = await _context.Fincas
			.FirstOrDefaultAsync(f => f.Id == request.FincaId, cancellationToken);
		if (finca is null)
		{
			result = Result<bool>.Failure($"Finca con Id {request.FincaId} no encontrada.");
			return result;
		}

		var puedeEliminar = !request.SoloLectura && request.PuedeEliminar;

		var asignacion = await _context.PersonasFincas
			.FirstOrDefaultAsync(pf => pf.PersonaId == request.PersonaId && pf.FincaId == request.FincaId, cancellationToken);

		if (asignacion is null)
		{
			_context.PersonasFincas.Add(new PersonaFinca
			{
				PersonaId = request.PersonaId,
				FincaId = request.FincaId,
				FechaAsignacion = DateTime.UtcNow,
				SoloLectura = request.SoloLectura,
				PuedeEliminar = puedeEliminar
			});
		}
		else
		{
			asignacion.SoloLectura = request.SoloLectura;
			asignacion.PuedeEliminar = puedeEliminar;
		}

		await _context.SaveChangesAsync(cancellationToken);

		var nivelTexto = request.SoloLectura
			? "solo lectura"
			: puedeEliminar ? "lectura, escritura y eliminación" : "lectura y escritura (sin eliminar)";

		await _bitacora.RegistrarAsync(
			"Permiso de finca actualizado",
			$"'{persona.Nombre}' quedó con acceso de '{nivelTexto}' en la finca '{finca.Nombre}'.",
			persona.ClienteId,
			cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
