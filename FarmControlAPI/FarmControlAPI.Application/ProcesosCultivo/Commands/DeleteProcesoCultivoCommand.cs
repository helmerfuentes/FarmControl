using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.ProcesosCultivo.Commands;

public record DeleteProcesoCultivoCommand(int Id) : IRequest<Result<bool>>;

public class DeleteProcesoCultivoCommandHandler : IRequestHandler<DeleteProcesoCultivoCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public DeleteProcesoCultivoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteProcesoCultivoCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var proceso = await _context.ProcesosCultivo
			.Include(pc => pc.Parcela)
			.FirstOrDefaultAsync(pc => pc.Id == request.Id, cancellationToken);

		if (proceso is null)
		{
			result = Result<bool>.Failure($"Proceso de cultivo con Id {request.Id} no encontrado.");
			return result;
		}

		if (!_currentUser.PuedeEliminarEnFinca(proceso.Parcela.FincaId))
		{
			result = Result<bool>.Failure("No tienes permiso para eliminar registros en esta finca.");
			return result;
		}

		_context.ProcesosCultivo.Remove(proceso);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar este proceso de cultivo: tiene actividades, compras o ventas asociadas.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Proceso de cultivo eliminado",
			$"Se eliminó el proceso de cultivo de la parcela '{proceso.Parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
