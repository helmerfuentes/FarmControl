using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Productos.Queries;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Productos.Commands;

public record UpdateProductoCommand(
	int Id,
	string Nombre,
	List<ClasificacionRequest> Clasificaciones) : IRequest<Result<ProductoDto>>;

public class UpdateProductoCommandHandler : IRequestHandler<UpdateProductoCommand, Result<ProductoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public UpdateProductoCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<ProductoDto>> Handle(UpdateProductoCommand request, CancellationToken cancellationToken)
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

		producto.Nombre = request.Nombre;

		_context.ClasificacionesProducto.RemoveRange(producto.Clasificaciones);

		foreach (var c in request.Clasificaciones ?? [])
		{
			producto.Clasificaciones.Add(new ClasificacionProducto
			{
				Nombre = c.Nombre,
				UnidadMedida = c.UnidadMedida,
				PesoUnidadKg = c.PesoUnidadKg
			});
		}

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Producto actualizado",
			$"Se actualizó el producto '{producto.Nombre}' del catálogo.",
			producto.ClienteId,
			cancellationToken);

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
