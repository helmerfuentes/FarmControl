using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.AnalisisSuelos.Commands;

public record DeleteAnalisisSueloCommand(int Id) : IRequest<Result<bool>>;

public class DeleteAnalisisSueloCommandHandler : IRequestHandler<DeleteAnalisisSueloCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteAnalisisSueloCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteAnalisisSueloCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var analisis = await _context.AnalisisSuelo
			.Include(a => a.Parcela)
			.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

		if (analisis is null)
		{
			result = Result<bool>.Failure($"Análisis de suelo con Id {request.Id} no encontrado.");
			return result;
		}

		if (!_currentUser.PuedeEliminarEnFinca(analisis.Parcela.FincaId))
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar registros en esta finca.");
			return result;
		}

		_context.AnalisisSuelo.Remove(analisis);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Análisis de suelo eliminado",
			$"Se eliminó el análisis de suelo del {analisis.Fecha:yyyy-MM-dd} de la parcela '{analisis.Parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
