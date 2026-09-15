using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Nomina.Queries;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Nomina.Commands;

public record CrearLiquidacionNominaCommand(
	int JornaleroId,
	DateTime FechaInicio,
	DateTime FechaFin,
	string? Observacion) : IRequest<Result<LiquidacionNominaDto>>;

public class CrearLiquidacionNominaCommandHandler : IRequestHandler<CrearLiquidacionNominaCommand, Result<LiquidacionNominaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public CrearLiquidacionNominaCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<LiquidacionNominaDto>> Handle(CrearLiquidacionNominaCommand request, CancellationToken cancellationToken)
	{
		Result<LiquidacionNominaDto> result;

		var jornalero = await _context.Personas.FirstOrDefaultAsync(p => p.Id == request.JornaleroId, cancellationToken);
		if (jornalero is null)
		{
			result = Result<LiquidacionNominaDto>.Failure($"Persona con Id {request.JornaleroId} no encontrada.");
			return result;
		}

		var registros = await _context.RegistrosManoObra
			.Include(r => r.Actividad)
			.Where(r => r.JornaleroId == request.JornaleroId
				&& r.LiquidacionNominaId == null
				&& r.Actividad.FechaInicio >= request.FechaInicio
				&& r.Actividad.FechaInicio <= request.FechaFin)
			.ToListAsync(cancellationToken);

		if (registros.Count == 0)
		{
			result = Result<LiquidacionNominaDto>.Failure("No hay registros de mano de obra pendientes de liquidar en ese rango.");
			return result;
		}

		var liquidacion = new LiquidacionNomina
		{
			JornaleroId = request.JornaleroId,
			FechaInicio = request.FechaInicio.Date,
			FechaFin = request.FechaFin.Date,
			TotalHoras = registros.Sum(r => r.NumHoras),
			TotalPagar = registros.Sum(r => r.NumHoras * r.ValorHora),
			FechaLiquidacion = DateTime.UtcNow,
			Observacion = request.Observacion
		};

		_context.LiquidacionesNomina.Add(liquidacion);

		foreach (var registro in registros)
		{
			registro.LiquidacionNomina = liquidacion;
		}

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Liquidación de nómina creada",
			$"Se liquidó la nómina de '{jornalero.Nombre}' del {liquidacion.FechaInicio:dd/MM/yyyy} al {liquidacion.FechaFin:dd/MM/yyyy} por un total de {liquidacion.TotalPagar:C}.",
			cancellationToken: cancellationToken);

		result = Result<LiquidacionNominaDto>.Success(new LiquidacionNominaDto(
			liquidacion.Id, liquidacion.JornaleroId, jornalero.Nombre, liquidacion.FechaInicio, liquidacion.FechaFin,
			liquidacion.TotalHoras, liquidacion.TotalPagar, liquidacion.FechaLiquidacion, liquidacion.Observacion));
		return result;
	}
}
