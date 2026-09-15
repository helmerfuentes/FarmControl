using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Insumos.Queries;

public record PrecioInsumoPorMesDto(int Anio, int Mes, decimal PrecioPromedio, decimal PrecioMin, decimal PrecioMax, int NumCompras);

public record HistorialPrecioInsumoDto(int InsumoId, string InsumoNombre, string UnidadMedida, List<PrecioInsumoPorMesDto> PorMes);

public record GetHistorialPrecioInsumoQuery(int InsumoId) : IRequest<Result<HistorialPrecioInsumoDto>>;

public class GetHistorialPrecioInsumoQueryHandler : IRequestHandler<GetHistorialPrecioInsumoQuery, Result<HistorialPrecioInsumoDto>>
{
	private readonly IFarmControlDbContext _context;

	public GetHistorialPrecioInsumoQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<HistorialPrecioInsumoDto>> Handle(GetHistorialPrecioInsumoQuery request, CancellationToken cancellationToken)
	{
		Result<HistorialPrecioInsumoDto> result;

		var insumo = await _context.Insumos.FirstOrDefaultAsync(i => i.Id == request.InsumoId, cancellationToken);
		if (insumo is null)
		{
			result = Result<HistorialPrecioInsumoDto>.Failure($"Insumo con Id {request.InsumoId} no encontrado.");
			return result;
		}

		var registros = await _context.MovimientosInsumo
			.Where(m => m.InsumoId == request.InsumoId
				&& m.TipoMovimiento == TipoMovimientoInsumo.Entrada
				&& m.PrecioUnitario != null)
			.ToListAsync(cancellationToken);

		var porMes = registros
			.GroupBy(m => new { m.Fecha.Year, m.Fecha.Month })
			.Select(g => new PrecioInsumoPorMesDto(
				g.Key.Year,
				g.Key.Month,
				Math.Round(g.Average(m => m.PrecioUnitario!.Value), 2),
				g.Min(m => m.PrecioUnitario!.Value),
				g.Max(m => m.PrecioUnitario!.Value),
				g.Count()))
			.OrderBy(m => m.Anio).ThenBy(m => m.Mes)
			.ToList();

		result = Result<HistorialPrecioInsumoDto>.Success(new HistorialPrecioInsumoDto(insumo.Id, insumo.Nombre, insumo.UnidadMedida, porMes));
		return result;
	}
}
