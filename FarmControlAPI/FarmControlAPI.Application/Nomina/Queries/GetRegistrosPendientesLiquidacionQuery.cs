using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Nomina.Queries;

public record RegistroPendienteLiquidacionDto(
	int RegistroId,
	DateTime FechaActividad,
	decimal NumHoras,
	decimal ValorHora,
	decimal Subtotal);

public record PendientesLiquidacionDto(
	List<RegistroPendienteLiquidacionDto> Registros,
	decimal TotalHoras,
	decimal TotalPagar);

public record GetRegistrosPendientesLiquidacionQuery(int JornaleroId, DateTime FechaInicio, DateTime FechaFin)
	: IRequest<Result<PendientesLiquidacionDto>>;

public class GetRegistrosPendientesLiquidacionQueryHandler : IRequestHandler<GetRegistrosPendientesLiquidacionQuery, Result<PendientesLiquidacionDto>>
{
	private readonly IFarmControlDbContext _context;

	public GetRegistrosPendientesLiquidacionQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<PendientesLiquidacionDto>> Handle(GetRegistrosPendientesLiquidacionQuery request, CancellationToken cancellationToken)
	{
		var registros = await _context.RegistrosManoObra
			.Include(r => r.Actividad)
			.Where(r => r.JornaleroId == request.JornaleroId
				&& r.LiquidacionNominaId == null
				&& r.Actividad.FechaInicio >= request.FechaInicio
				&& r.Actividad.FechaInicio <= request.FechaFin)
			.OrderBy(r => r.Actividad.FechaInicio)
			.Select(r => new RegistroPendienteLiquidacionDto(
				r.Id, r.Actividad.FechaInicio, r.NumHoras, r.ValorHora, r.NumHoras * r.ValorHora))
			.ToListAsync(cancellationToken);

		var dto = new PendientesLiquidacionDto(
			registros,
			registros.Sum(r => r.NumHoras),
			registros.Sum(r => r.Subtotal));

		var result = Result<PendientesLiquidacionDto>.Success(dto);
		return result;
	}
}
