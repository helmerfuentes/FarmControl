using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Actividades.Commands;

public record ConfirmarActividadCommand(int Id, string ConfirmadaPor) : IRequest<Result<ActividadDto>>;

public class ConfirmarActividadCommandHandler : IRequestHandler<ConfirmarActividadCommand, Result<ActividadDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public ConfirmarActividadCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ActividadDto>> Handle(ConfirmarActividadCommand request, CancellationToken cancellationToken)
	{
		Result<ActividadDto> result;

		if (string.IsNullOrWhiteSpace(request.ConfirmadaPor))
		{
			result = Result<ActividadDto>.Failure("Debe indicar quién confirma la actividad.");
			return result;
		}

		var actividad = await _context.Actividades
			.Include(a => a.Parcela)
			.Include(a => a.PersonaACargo)
			.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

		if (actividad is null)
		{
			result = Result<ActividadDto>.Failure($"Actividad con Id {request.Id} no encontrada.");
			return result;
		}

		var esResponsableAsignado = actividad.PersonaACargoId.HasValue && _currentUser.PersonaId == actividad.PersonaACargoId.Value;
		var puedeComoAdmin = _currentUser.TieneAccesoGlobal || _currentUser.PuedeEscribirEnFinca(actividad.Parcela.FincaId);

		if (!esResponsableAsignado && !puedeComoAdmin)
		{
			result = Result<ActividadDto>.Failure("No tiene permiso para confirmar esta actividad.");
			return result;
		}

		actividad.Confirmada = true;
		actividad.FechaConfirmacion = DateTime.UtcNow;
		actividad.ConfirmadaPor = request.ConfirmadaPor;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Actividad confirmada",
			$"'{request.ConfirmadaPor}' confirmó la actividad '{actividad.TipoActividad}'.",
			cancellationToken: cancellationToken);

		var costo = CreateActividadCommandHandler.CalcularCosto(actividad.FechaInicio, actividad.FechaFin, actividad.ValorDiaUsado);
		result = Result<ActividadDto>.Success(new ActividadDto(
			actividad.Id, actividad.ParcelaId, actividad.TipoActividad,
			actividad.FechaInicio, actividad.FechaFin,
			actividad.PersonaACargoId, actividad.PersonaACargo?.Nombre,
			actividad.Descripcion, actividad.ValorDiaUsado, costo,
			ProcesoCultivoId: actividad.ProcesoCultivoId,
			Confirmada: actividad.Confirmada,
			FechaConfirmacion: actividad.FechaConfirmacion,
			ConfirmadaPor: actividad.ConfirmadaPor));
		return result;
	}
}
