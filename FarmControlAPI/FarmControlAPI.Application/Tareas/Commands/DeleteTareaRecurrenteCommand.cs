using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Tareas.Commands;

public record DeleteTareaRecurrenteCommand(int Id) : IRequest<Result<bool>>;

public class DeleteTareaRecurrenteCommandHandler : IRequestHandler<DeleteTareaRecurrenteCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteTareaRecurrenteCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteTareaRecurrenteCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var tarea = await _context.TareasRecurrentes
			.Include(t => t.Parcela)
			.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

		if (tarea is null)
		{
			result = Result<bool>.Failure($"Tarea con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEliminarEnFinca(tarea.Parcela.FincaId))
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar registros en esta finca.");
			return result;
		}

		_context.TareasRecurrentes.Remove(tarea);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Tarea recurrente eliminada",
			$"Se eliminó la tarea '{tarea.Descripcion}' de la parcela '{tarea.Parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
