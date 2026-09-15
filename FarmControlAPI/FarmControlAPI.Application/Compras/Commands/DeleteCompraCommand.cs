using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Compras.Commands;

public record DeleteCompraCommand(int Id) : IRequest<Result<bool>>;

public class DeleteCompraCommandHandler : IRequestHandler<DeleteCompraCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteCompraCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteCompraCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var compra = await _context.Compras
			.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

		if (compra is null)
		{
			result = Result<bool>.Failure($"Compra con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEliminarEnFinca(compra.FincaId))
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar registros en esta finca.");
			return result;
		}

		_context.Compras.Remove(compra);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar esta compra: tiene registros asociados que lo impiden.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Compra eliminada",
			$"Se eliminó la compra '{compra.Descripcion}' por {compra.Valor:C0}.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
