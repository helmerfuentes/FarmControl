using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Nomina.Queries;

public record LiquidacionNominaDto(
	int Id,
	int JornaleroId,
	string JornaleroNombre,
	DateTime FechaInicio,
	DateTime FechaFin,
	decimal TotalHoras,
	decimal TotalPagar,
	DateTime FechaLiquidacion,
	string? Observacion);

public record GetLiquidacionesQuery(int? JornaleroId) : IRequest<Result<List<LiquidacionNominaDto>>>;

public class GetLiquidacionesQueryHandler : IRequestHandler<GetLiquidacionesQuery, Result<List<LiquidacionNominaDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetLiquidacionesQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<LiquidacionNominaDto>>> Handle(GetLiquidacionesQuery request, CancellationToken cancellationToken)
	{
		var query = _context.LiquidacionesNomina.Include(l => l.Jornalero).AsQueryable();

		if (request.JornaleroId.HasValue)
		{
			query = query.Where(l => l.JornaleroId == request.JornaleroId.Value);
		}

		var liquidaciones = await query
			.OrderByDescending(l => l.FechaLiquidacion)
			.Select(l => new LiquidacionNominaDto(
				l.Id, l.JornaleroId, l.Jornalero.Nombre, l.FechaInicio, l.FechaFin,
				l.TotalHoras, l.TotalPagar, l.FechaLiquidacion, l.Observacion))
			.ToListAsync(cancellationToken);

		var result = Result<List<LiquidacionNominaDto>>.Success(liquidaciones);
		return result;
	}
}
