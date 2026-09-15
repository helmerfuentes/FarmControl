using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Fincas.Commands;

public record DeleteParcelaCommand(int Id) : IRequest<Result<bool>>;

public class DeleteParcelaCommandHandler : IRequestHandler<DeleteParcelaCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteParcelaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteParcelaCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var parcela = await _context.Parcelas
			.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

		if (parcela is null)
		{
			result = Result<bool>.Failure($"Parcela con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEliminarEnFinca(parcela.FincaId))
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar registros en esta finca.");
			return result;
		}

		_context.Parcelas.Remove(parcela);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar esta parcela: tiene actividades, compras, ventas u otros registros asociados.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Parcela eliminada",
			$"Se eliminó la parcela '{parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
