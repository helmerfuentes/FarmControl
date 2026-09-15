using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Insumos.Queries;

public record InsumoDto(int Id, string Nombre, int TipoInsumoId, string TipoInsumoNombre, string? Marca, string? Descripcion, decimal PrecioUnitario, string UnidadMedida, decimal? StockMinimo, DateTime? FechaVencimiento);

public record GetInsumosQuery(int? TipoInsumoId) : IRequest<Result<List<InsumoDto>>>;

public class GetInsumosQueryHandler : IRequestHandler<GetInsumosQuery, Result<List<InsumoDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetInsumosQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<InsumoDto>>> Handle(GetInsumosQuery request, CancellationToken cancellationToken)
	{
		Result<List<InsumoDto>> result;

		var query = _context.Insumos.Include(i => i.TipoInsumo).AsQueryable();

		if (request.TipoInsumoId.HasValue)
		{
			query = query.Where(i => i.TipoInsumoId == request.TipoInsumoId.Value);
		}

		var insumos = await query
			.OrderBy(i => i.Nombre)
			.Select(i => new InsumoDto(i.Id, i.Nombre, i.TipoInsumoId, i.TipoInsumo.Nombre, i.Marca, i.Descripcion, i.PrecioUnitario, i.UnidadMedida, i.StockMinimo, i.FechaVencimiento))
			.ToListAsync(cancellationToken);

		result = Result<List<InsumoDto>>.Success(insumos);
		return result;
	}
}
