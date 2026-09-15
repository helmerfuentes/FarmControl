using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Productos.Queries;

public record ClasificacionDto(int Id, string Nombre, string UnidadMedida, decimal PesoUnidadKg);

public record ProductoDto(int Id, string Nombre, List<ClasificacionDto> Clasificaciones);

public record GetProductosQuery : IRequest<Result<List<ProductoDto>>>;

public class GetProductosQueryHandler : IRequestHandler<GetProductosQuery, Result<List<ProductoDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetProductosQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<ProductoDto>>> Handle(GetProductosQuery request, CancellationToken cancellationToken)
	{
		Result<List<ProductoDto>> result;

		var productos = await _context.Productos
			.Include(p => p.Clasificaciones)
			.OrderBy(p => p.Nombre)
			.Select(p => new ProductoDto(
				p.Id,
				p.Nombre,
				p.Clasificaciones
					.OrderBy(c => c.Nombre)
					.Select(c => new ClasificacionDto(c.Id, c.Nombre, c.UnidadMedida.ToString(), c.PesoUnidadKg))
					.ToList()))
			.ToListAsync(cancellationToken);

		result = Result<List<ProductoDto>>.Success(productos);
		return result;
	}
}
