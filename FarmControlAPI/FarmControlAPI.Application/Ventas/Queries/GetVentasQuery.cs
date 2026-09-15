using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Ventas.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Ventas.Queries;

public record GetVentasQuery(int? ParcelaId, DateTime? Desde, DateTime? Hasta) : IRequest<Result<List<VentaDto>>>;

public class GetVentasQueryHandler : IRequestHandler<GetVentasQuery, Result<List<VentaDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetVentasQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<VentaDto>>> Handle(GetVentasQuery request, CancellationToken cancellationToken)
	{
		Result<List<VentaDto>> result;

		var query = _context.Ventas
			.Include(v => v.Comprador)
			.Include(v => v.Detalles)
			.Include(v => v.Pagos)
			.Include(v => v.Parcela).ThenInclude(p => p.Finca)
			.AsQueryable();

		if (request.ParcelaId.HasValue)
		{
			query = query.Where(v => v.ParcelaId == request.ParcelaId.Value);
		}

		if (request.Desde.HasValue)
		{
			query = query.Where(v => v.Fecha >= request.Desde.Value);
		}

		if (request.Hasta.HasValue)
		{
			query = query.Where(v => v.Fecha <= request.Hasta.Value);
		}

		var ventasEntidades = await query
			.OrderByDescending(v => v.Fecha)
			.ToListAsync(cancellationToken);

		var ventas = ventasEntidades.Select(v =>
		{
			var totalPagado = v.Pagos.Sum(p => p.Monto);
			return new VentaDto(
				v.Id, v.ParcelaId, v.CompradorId, v.Comprador.Nombre, v.Fecha, v.ValorTransporte, v.Total,
				v.Detalles.Select(d => new DetalleVentaDto(d.Id, d.Clasificacion, d.Cantidad, d.UnidadMedida, d.PrecioUnitario, d.Subtotal)).ToList(),
				v.Parcela.Nombre,
				v.Parcela.Finca.Nombre,
				v.ProcesoCultivoId,
				totalPagado,
				v.Total - totalPagado);
		}).ToList();

		result = Result<List<VentaDto>>.Success(ventas);
		return result;
	}
}
