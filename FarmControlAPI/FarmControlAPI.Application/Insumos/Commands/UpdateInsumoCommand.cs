using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Insumos.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Insumos.Commands;

public record UpdateInsumoCommand(
	int Id,
	int TipoInsumoId,
	string Nombre,
	string? Marca,
	string? Descripcion,
	decimal PrecioUnitario,
	string UnidadMedida,
	decimal? StockMinimo,
	DateTime? FechaVencimiento = null) : IRequest<Result<InsumoDto>>;

public class UpdateInsumoCommandHandler : IRequestHandler<UpdateInsumoCommand, Result<InsumoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public UpdateInsumoCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<InsumoDto>> Handle(UpdateInsumoCommand request, CancellationToken cancellationToken)
	{
		Result<InsumoDto> result;

		var insumo = await _context.Insumos
			.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

		if (insumo is null)
		{
			result = Result<InsumoDto>.Failure($"Insumo con Id {request.Id} no encontrado.");
			return result;
		}

		var tipoInsumo = await _context.TiposInsumo
			.FirstOrDefaultAsync(t => t.Id == request.TipoInsumoId, cancellationToken);

		if (tipoInsumo is null)
		{
			result = Result<InsumoDto>.Failure($"TipoInsumo con Id {request.TipoInsumoId} no encontrado.");
			return result;
		}

		insumo.TipoInsumoId = request.TipoInsumoId;
		insumo.Nombre = request.Nombre;
		insumo.Marca = request.Marca;
		insumo.Descripcion = request.Descripcion;
		insumo.PrecioUnitario = request.PrecioUnitario;
		insumo.UnidadMedida = request.UnidadMedida;
		insumo.StockMinimo = request.StockMinimo;
		insumo.FechaVencimiento = request.FechaVencimiento;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Insumo actualizado",
			$"Se actualizó el insumo '{insumo.Nombre}' del catálogo.",
			cancellationToken: cancellationToken);

		result = Result<InsumoDto>.Success(new InsumoDto(insumo.Id, insumo.Nombre, insumo.TipoInsumoId, tipoInsumo.Nombre, insumo.Marca, insumo.Descripcion, insumo.PrecioUnitario, insumo.UnidadMedida, insumo.StockMinimo, insumo.FechaVencimiento));
		return result;
	}
}
