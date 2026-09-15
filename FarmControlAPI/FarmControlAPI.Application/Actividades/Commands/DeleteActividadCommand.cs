using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Actividades.Commands;

public record DeleteActividadCommand(int Id) : IRequest<Result<bool>>;

public class DeleteActividadCommandHandler : IRequestHandler<DeleteActividadCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteActividadCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteActividadCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var actividad = await _context.Actividades
			.Include(a => a.Parcela)
			.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

		if (actividad is null)
		{
			result = Result<bool>.Failure($"Actividad con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEliminarEnFinca(actividad.Parcela.FincaId))
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar registros en esta finca.");
			return result;
		}

		_context.Actividades.Remove(actividad);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar esta actividad: tiene registros asociados que lo impiden.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Actividad eliminada",
			$"Se eliminó la actividad '{actividad.TipoActividad}' de la parcela '{actividad.Parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
