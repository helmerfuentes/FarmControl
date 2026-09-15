using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.TiposInsumo.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.TiposInsumo.Commands;

public record UpdateTipoInsumoCommand(int Id, string Nombre, string Descripcion) : IRequest<Result<TipoInsumoDto>>;

public class UpdateTipoInsumoCommandHandler : IRequestHandler<UpdateTipoInsumoCommand, Result<TipoInsumoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public UpdateTipoInsumoCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<TipoInsumoDto>> Handle(UpdateTipoInsumoCommand request, CancellationToken cancellationToken)
	{
		Result<TipoInsumoDto> result;

		var tipoInsumo = await _context.TiposInsumo
			.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

		if (tipoInsumo is null)
		{
			result = Result<TipoInsumoDto>.Failure($"TipoInsumo con Id {request.Id} no encontrado.");
			return result;
		}

		tipoInsumo.Nombre = request.Nombre;
		tipoInsumo.Descripcion = request.Descripcion;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Tipo de insumo actualizado",
			$"Se actualizó el tipo de insumo '{tipoInsumo.Nombre}' del catálogo.",
			tipoInsumo.ClienteId,
			cancellationToken);

		result = Result<TipoInsumoDto>.Success(new TipoInsumoDto(tipoInsumo.Id, tipoInsumo.Nombre, tipoInsumo.Descripcion));
		return result;
	}
}
