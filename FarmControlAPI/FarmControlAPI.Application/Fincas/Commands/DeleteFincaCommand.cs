using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Fincas.Commands;

public record DeleteFincaCommand(int Id) : IRequest<Result<bool>>;

public class DeleteFincaCommandHandler : IRequestHandler<DeleteFincaCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteFincaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteFincaCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var finca = await _context.Fincas
			.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

		if (finca is null)
		{
			result = Result<bool>.Failure($"Finca con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEliminarEnFinca(finca.Id))
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar esta finca.");
			return result;
		}

		_context.Fincas.Remove(finca);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar esta finca: tiene parcelas, compras u otros registros asociados.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Finca eliminada",
			$"Se eliminó la finca '{finca.Nombre}'.",
			finca.ClienteId,
			cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
