using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Actividades.Commands;

public record UpdateActividadCommand(
	int Id,
	int ParcelaId,
	string TipoActividad,
	DateTime FechaInicio,
	DateTime? FechaFin,
	int? PersonaACargoId,
	decimal ValorDiaActividad,
	string? Descripcion) : IRequest<Result<ActividadDto>>;

public class UpdateActividadCommandHandler : IRequestHandler<UpdateActividadCommand, Result<ActividadDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public UpdateActividadCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ActividadDto>> Handle(UpdateActividadCommand request, CancellationToken cancellationToken)
	{
		Result<ActividadDto> result;

		var actividad = await _context.Actividades
			.Include(a => a.Parcela)
			.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

		if (actividad is null)
		{
			result = Result<ActividadDto>.Failure($"Actividad con Id {request.Id} no encontrada.");
			return result;
		}

		var fincaDestino = await _context.Parcelas
			.Where(p => p.Id == request.ParcelaId)
			.Select(p => (int?)p.FincaId)
			.FirstOrDefaultAsync(cancellationToken);

		if (fincaDestino is null)
		{
			result = Result<ActividadDto>.Failure($"Parcela con Id {request.ParcelaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(actividad.Parcela.FincaId) || !_currentUser.PuedeEscribirEnFinca(fincaDestino.Value))
		{
			result = Result<ActividadDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		string? personaACargoNombre = null;
		if (request.PersonaACargoId.HasValue)
		{
			var persona = await _context.Personas.FindAsync([request.PersonaACargoId.Value], cancellationToken);
			personaACargoNombre = persona?.Nombre;
		}

		actividad.ParcelaId = request.ParcelaId;
		actividad.TipoActividad = request.TipoActividad;
		actividad.FechaInicio = request.FechaInicio;
		actividad.FechaFin = request.FechaFin;
		actividad.PersonaACargoId = request.PersonaACargoId;
		actividad.ValorDiaUsado = request.ValorDiaActividad;
		actividad.Descripcion = request.Descripcion;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Actividad actualizada",
			$"Se actualizó la actividad '{actividad.TipoActividad}'.",
			cancellationToken: cancellationToken);

		var costo = CreateActividadCommandHandler.CalcularCosto(actividad.FechaInicio, actividad.FechaFin, actividad.ValorDiaUsado);
		result = Result<ActividadDto>.Success(new ActividadDto(
			actividad.Id, actividad.ParcelaId, actividad.TipoActividad,
			actividad.FechaInicio, actividad.FechaFin,
			actividad.PersonaACargoId, personaACargoNombre,
			actividad.Descripcion, actividad.ValorDiaUsado, costo));
		return result;
	}
}
