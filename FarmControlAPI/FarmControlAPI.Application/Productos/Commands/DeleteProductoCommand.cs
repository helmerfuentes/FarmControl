using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Productos.Commands;

public record DeleteProductoCommand(int Id) : IRequest<Result<bool>>;

public class DeleteProductoCommandHandler : IRequestHandler<DeleteProductoCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public DeleteProductoCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteProductoCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var producto = await _context.Productos
			.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

		if (producto is null)
		{
			result = Result<bool>.Failure($"Producto con Id {request.Id} no encontrado.");
			return result;
		}

		_context.Productos.Remove(producto);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar este producto: está en uso en parcelas, procesos de cultivo o el historial de ventas.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Producto eliminado",
			$"Se eliminó el producto '{producto.Nombre}' del catálogo.",
			producto.ClienteId,
			cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
