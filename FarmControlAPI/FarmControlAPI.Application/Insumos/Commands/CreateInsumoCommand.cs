using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Insumos.Queries;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Insumos.Commands;

public record CreateInsumoCommand(
	int TipoInsumoId,
	string Nombre,
	string? Marca,
	string? Descripcion,
	decimal PrecioUnitario,
	string UnidadMedida,
	decimal? StockMinimo,
	DateTime? FechaVencimiento = null) : IRequest<Result<InsumoDto>>;

public class CreateInsumoCommandHandler : IRequestHandler<CreateInsumoCommand, Result<InsumoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public CreateInsumoCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<InsumoDto>> Handle(CreateInsumoCommand request, CancellationToken cancellationToken)
	{
		Result<InsumoDto> result;

		var tipoInsumo = await _context.TiposInsumo.FirstOrDefaultAsync(t => t.Id == request.TipoInsumoId, cancellationToken);
		if (tipoInsumo is null)
		{
			result = Result<InsumoDto>.Failure($"TipoInsumo con Id {request.TipoInsumoId} no encontrado.");
			return result;
		}

		var insumo = new Insumo
		{
			TipoInsumoId = request.TipoInsumoId,
			Nombre = request.Nombre,
			Marca = request.Marca,
			Descripcion = request.Descripcion,
			PrecioUnitario = request.PrecioUnitario,
			UnidadMedida = request.UnidadMedida,
			StockMinimo = request.StockMinimo,
			FechaVencimiento = request.FechaVencimiento
		};

		_context.Insumos.Add(insumo);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Insumo creado",
			$"Se registró el insumo '{insumo.Nombre}' en el catálogo.",
			cancellationToken: cancellationToken);

		result = Result<InsumoDto>.Success(new InsumoDto(insumo.Id, insumo.Nombre, insumo.TipoInsumoId, tipoInsumo.Nombre, insumo.Marca, insumo.Descripcion, insumo.PrecioUnitario, insumo.UnidadMedida, insumo.StockMinimo, insumo.FechaVencimiento));
		return result;
	}
}
