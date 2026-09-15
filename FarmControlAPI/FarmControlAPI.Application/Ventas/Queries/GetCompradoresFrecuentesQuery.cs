using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Ventas.Queries;

public record CompradorFrecuenteDto(int CompradorId, string CompradorNombre, int NumVentas, decimal VolumenTotal, DateTime UltimaVenta);

public record GetCompradoresFrecuentesQuery : IRequest<Result<List<CompradorFrecuenteDto>>>;

public class GetCompradoresFrecuentesQueryHandler : IRequestHandler<GetCompradoresFrecuentesQuery, Result<List<CompradorFrecuenteDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetCompradoresFrecuentesQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<CompradorFrecuenteDto>>> Handle(GetCompradoresFrecuentesQuery request, CancellationToken cancellationToken)
	{
		var ventas = await _context.Ventas
			.Include(v => v.Comprador)
			.Include(v => v.Detalles)
			.ToListAsync(cancellationToken);

		var compradores = ventas
			.GroupBy(v => new { v.CompradorId, v.Comprador.Nombre })
			.Select(g => new CompradorFrecuenteDto(
				g.Key.CompradorId,
				g.Key.Nombre,
				g.Count(),
				g.Sum(v => v.Detalles.Sum(d => d.Subtotal)),
				g.Max(v => v.Fecha)))
			.OrderByDescending(c => c.VolumenTotal)
			.ToList();

		var result = Result<List<CompradorFrecuenteDto>>.Success(compradores);
		return result;
	}
}
