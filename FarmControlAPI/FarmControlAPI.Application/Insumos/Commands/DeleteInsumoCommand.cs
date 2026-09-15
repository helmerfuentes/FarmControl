using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Insumos.Commands;

public record DeleteInsumoCommand(int Id) : IRequest<Result<bool>>;

public class DeleteInsumoCommandHandler : IRequestHandler<DeleteInsumoCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public DeleteInsumoCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteInsumoCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var insumo = await _context.Insumos
			.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

		if (insumo is null)
		{
			result = Result<bool>.Failure($"Insumo con Id {request.Id} no encontrado.");
			return result;
		}

		_context.Insumos.Remove(insumo);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar este insumo: tiene movimientos de inventario asociados.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Insumo eliminado",
			$"Se eliminó el insumo '{insumo.Nombre}' del catálogo.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
