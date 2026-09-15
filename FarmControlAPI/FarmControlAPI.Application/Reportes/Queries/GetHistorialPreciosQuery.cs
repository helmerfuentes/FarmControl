using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Reportes.Queries;

public record PrecioPorMesDto(int Mes, int Anio, decimal PrecioPromedio, decimal PrecioMin, decimal PrecioMax, int NumVentas);

public record HistorialPreciosDto(int ProductoId, string ProductoNombre, string Clasificacion, UnidadMedidaVenta UnidadMedida, List<PrecioPorMesDto> PorMes);

public record GetHistorialPreciosQuery(int ProductoId, string? Clasificacion, int? Anio) : IRequest<Result<List<HistorialPreciosDto>>>;

public class GetHistorialPreciosQueryHandler : IRequestHandler<GetHistorialPreciosQuery, Result<List<HistorialPreciosDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetHistorialPreciosQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<HistorialPreciosDto>>> Handle(GetHistorialPreciosQuery request, CancellationToken cancellationToken)
	{
		Result<List<HistorialPreciosDto>> result;

		var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == request.ProductoId, cancellationToken);
		if (producto is null)
		{
			result = Result<List<HistorialPreciosDto>>.Failure($"Producto con Id {request.ProductoId} no encontrado.");
			return result;
		}

		var query = _context.HistorialPreciosVenta
			.Where(h => h.ProductoId == request.ProductoId)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(request.Clasificacion))
		{
			query = query.Where(h => h.Clasificacion == request.Clasificacion);
		}

		if (request.Anio.HasValue)
		{
			query = query.Where(h => h.Fecha.Year == request.Anio.Value);
		}

		var registros = await query.ToListAsync(cancellationToken);

		var agrupado = registros
			.GroupBy(h => new { h.Clasificacion, h.UnidadMedida })
			.Select(g => new HistorialPreciosDto(
				request.ProductoId,
				producto.Nombre,
				g.Key.Clasificacion,
				g.Key.UnidadMedida,
				g.GroupBy(h => new { h.Fecha.Month, h.Fecha.Year })
					.Select(m => new PrecioPorMesDto(
						m.Key.Month,
						m.Key.Year,
						Math.Round(m.Average(h => h.PrecioUnitario), 2),
						m.Min(h => h.PrecioUnitario),
						m.Max(h => h.PrecioUnitario),
						m.Count()))
					.OrderBy(m => m.Anio).ThenBy(m => m.Mes)
					.ToList()))
			.ToList();

		result = Result<List<HistorialPreciosDto>>.Success(agrupado);
		return result;
	}
}
