using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Productos.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;

namespace FarmControlAPI.Application.Productos.Commands;

public record ClasificacionRequest(string Nombre, UnidadMedidaVenta UnidadMedida, decimal PesoUnidadKg);

public record CreateProductoCommand(
	string Nombre,
	List<ClasificacionRequest> Clasificaciones) : IRequest<Result<ProductoDto>>;

public class CreateProductoCommandHandler : IRequestHandler<CreateProductoCommand, Result<ProductoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateProductoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ProductoDto>> Handle(CreateProductoCommand request, CancellationToken cancellationToken)
	{
		Result<ProductoDto> result;

		if (_currentUser.ClienteId is null)
		{
			result = Result<ProductoDto>.Failure("El usuario actual no tiene un cliente asociado.");
			return result;
		}

		var producto = new Producto { ClienteId = _currentUser.ClienteId.Value, Nombre = request.Nombre };

		foreach (var c in request.Clasificaciones ?? [])
		{
			producto.Clasificaciones.Add(new ClasificacionProducto
			{
				Nombre = c.Nombre,
				UnidadMedida = c.UnidadMedida,
				PesoUnidadKg = c.PesoUnidadKg
			});
		}

		_context.Productos.Add(producto);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Producto creado",
			$"Se registró el producto '{producto.Nombre}' en el catálogo.",
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
