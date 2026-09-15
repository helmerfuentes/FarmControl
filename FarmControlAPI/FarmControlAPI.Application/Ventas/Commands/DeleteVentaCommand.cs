using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Ventas.Commands;

public record DeleteVentaCommand(int Id) : IRequest<Result<bool>>;

public class DeleteVentaCommandHandler : IRequestHandler<DeleteVentaCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteVentaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteVentaCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var venta = await _context.Ventas
			.Include(v => v.Parcela)
			.FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

		if (venta is null)
		{
			result = Result<bool>.Failure($"Venta con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEliminarEnFinca(venta.Parcela.FincaId))
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar registros en esta finca.");
			return result;
		}

		_context.Ventas.Remove(venta);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar esta venta: tiene registros asociados que lo impiden.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Venta eliminada",
			$"Se eliminó la venta del {venta.Fecha:dd/MM/yyyy} de la parcela '{venta.Parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
