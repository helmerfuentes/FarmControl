using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Comentarios.Commands;

public record DeleteComentarioCommand(int Id) : IRequest<Result<bool>>;

public class DeleteComentarioCommandHandler : IRequestHandler<DeleteComentarioCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteComentarioCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteComentarioCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var comentario = await _context.Comentarios.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
		if (comentario is null)
		{
			result = Result<bool>.Failure($"Comentario con Id {request.Id} no encontrado.");
			return result;
		}

		var esAutor = _currentUser.PersonaId == comentario.AutorPersonaId;
		var puedeComoAdmin = _currentUser.TieneAccesoGlobal ||
			await ComentariosAccesoHelper.PuedeEliminarEnEntidadAsync(_context, _currentUser, comentario.TipoEntidad, comentario.EntidadId, cancellationToken);

		if (!esAutor && !puedeComoAdmin)
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar este comentario.");
			return result;
		}

		_context.Comentarios.Remove(comentario);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Comentario eliminado",
			$"Se eliminó un comentario en {comentario.TipoEntidad} #{comentario.EntidadId}.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
