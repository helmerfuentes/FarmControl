using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Asistencias.Commands;

public record DeleteAsistenciaCommand(int Id) : IRequest<Result<bool>>;

public class DeleteAsistenciaCommandHandler : IRequestHandler<DeleteAsistenciaCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteAsistenciaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteAsistenciaCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var asistencia = await _context.Asistencias
			.Include(a => a.Persona)
			.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

		if (asistencia is null)
		{
			result = Result<bool>.Failure($"Asistencia con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEliminarEnFinca(asistencia.FincaId))
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar registros en esta finca.");
			return result;
		}

		_context.Asistencias.Remove(asistencia);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Asistencia eliminada",
			$"Se eliminó la asistencia de '{asistencia.Persona.Nombre}' del {asistencia.Fecha:dd/MM/yyyy}.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
