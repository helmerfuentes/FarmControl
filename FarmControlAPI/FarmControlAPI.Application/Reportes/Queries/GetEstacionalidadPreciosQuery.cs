using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Reportes.Queries;

public record EstacionalidadMesDto(int Mes, decimal? PrecioPromedio, decimal? PrecioMin, decimal? PrecioMax, int NumVentas);

public record EstacionalidadPreciosDto(int ProductoId, string ProductoNombre, string? Clasificacion, List<EstacionalidadMesDto> PorMes);

public record GetEstacionalidadPreciosQuery(int ProductoId, string? Clasificacion) : IRequest<Result<EstacionalidadPreciosDto>>;

public class GetEstacionalidadPreciosQueryHandler : IRequestHandler<GetEstacionalidadPreciosQuery, Result<EstacionalidadPreciosDto>>
{
	private const int _MESES_EN_ANIO = 12;

	private readonly IFarmControlDbContext _context;

	public GetEstacionalidadPreciosQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<EstacionalidadPreciosDto>> Handle(GetEstacionalidadPreciosQuery request, CancellationToken cancellationToken)
	{
		Result<EstacionalidadPreciosDto> result;

		var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == request.ProductoId, cancellationToken);
		if (producto is null)
		{
			result = Result<EstacionalidadPreciosDto>.Failure($"Producto con Id {request.ProductoId} no encontrado.");
			return result;
		}

		var query = _context.HistorialPreciosVenta.Where(h => h.ProductoId == request.ProductoId);

		if (!string.IsNullOrWhiteSpace(request.Clasificacion))
		{
			query = query.Where(h => h.Clasificacion == request.Clasificacion);
		}

		var registros = await query.ToListAsync(cancellationToken);

		var porMesEncontrado = registros
			.GroupBy(h => h.Fecha.Month)
			.ToDictionary(g => g.Key, g => new EstacionalidadMesDto(
				g.Key,
				Math.Round(g.Average(h => h.PrecioUnitario), 2),
				g.Min(h => h.PrecioUnitario),
				g.Max(h => h.PrecioUnitario),
				g.Count()));

		var porMes = Enumerable.Range(1, _MESES_EN_ANIO)
			.Select(mes => porMesEncontrado.TryGetValue(mes, out var dto) ? dto : new EstacionalidadMesDto(mes, null, null, null, 0))
			.ToList();

		result = Result<EstacionalidadPreciosDto>.Success(new EstacionalidadPreciosDto(
			request.ProductoId, producto.Nombre, request.Clasificacion, porMes));
		return result;
	}
}
