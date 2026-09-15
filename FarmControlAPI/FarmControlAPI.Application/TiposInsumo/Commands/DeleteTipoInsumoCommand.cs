using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.TiposInsumo.Commands;

public record DeleteTipoInsumoCommand(int Id) : IRequest<Result<bool>>;

public class DeleteTipoInsumoCommandHandler : IRequestHandler<DeleteTipoInsumoCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public DeleteTipoInsumoCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeleteTipoInsumoCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var tipoInsumo = await _context.TiposInsumo
			.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

		if (tipoInsumo is null)
		{
			result = Result<bool>.Failure($"TipoInsumo con Id {request.Id} no encontrado.");
			return result;
		}

		_context.TiposInsumo.Remove(tipoInsumo);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar este tipo de insumo: tiene insumos asociados.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Tipo de insumo eliminado",
			$"Se eliminó el tipo de insumo '{tipoInsumo.Nombre}' del catálogo.",
			tipoInsumo.ClienteId,
			cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
