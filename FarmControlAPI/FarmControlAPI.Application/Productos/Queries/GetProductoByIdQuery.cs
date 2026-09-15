using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Productos.Queries;

public record GetProductoByIdQuery(int Id) : IRequest<Result<ProductoDto>>;

public class GetProductoByIdQueryHandler : IRequestHandler<GetProductoByIdQuery, Result<ProductoDto>>
{
	private readonly IFarmControlDbContext _context;

	public GetProductoByIdQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<ProductoDto>> Handle(GetProductoByIdQuery request, CancellationToken cancellationToken)
	{
		Result<ProductoDto> result;

		var producto = await _context.Productos
			.Include(p => p.Clasificaciones)
			.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

		if (producto is null)
		{
			result = Result<ProductoDto>.Failure($"Producto con Id {request.Id} no encontrado.");
			return result;
		}

		var dto = new ProductoDto(
			producto.Id,
			producto.Nombre,
			producto.Clasificaciones
				.OrderBy(c => c.Nombre)
				.Select(c => new ClasificacionDto(c.Id, c.Nombre, c.UnidadMedida.ToString(), c.PesoUnidadKg))
				.ToList());

		result = Result<ProductoDto>.Success(dto);
		return result;
	}
}
