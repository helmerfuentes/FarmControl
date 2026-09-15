using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Insumos.Queries;

public record MovimientoInsumoDto(
	int Id,
	int InsumoId,
	string InsumoNombre,
	int ParcelaId,
	string ParcelaNombre,
	decimal Cantidad,
	TipoMovimientoInsumo TipoMovimiento,
	DateTime Fecha,
	string? Observacion,
	decimal? PrecioUnitario);

public record GetMovimientosInsumoQuery(int? InsumoId) : IRequest<Result<List<MovimientoInsumoDto>>>;

public class GetMovimientosInsumoQueryHandler : IRequestHandler<GetMovimientosInsumoQuery, Result<List<MovimientoInsumoDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetMovimientosInsumoQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<MovimientoInsumoDto>>> Handle(GetMovimientosInsumoQuery request, CancellationToken cancellationToken)
	{
		Result<List<MovimientoInsumoDto>> result;

		var query = _context.MovimientosInsumo
			.Include(m => m.Insumo)
			.Include(m => m.Parcela)
			.AsQueryable();

		if (request.InsumoId.HasValue)
		{
			query = query.Where(m => m.InsumoId == request.InsumoId.Value);
		}

		var movimientos = await query
			.OrderByDescending(m => m.Fecha)
			.Select(m => new MovimientoInsumoDto(
				m.Id, m.InsumoId, m.Insumo.Nombre, m.ParcelaId, m.Parcela.Nombre,
				m.Cantidad, m.TipoMovimiento, m.Fecha, m.Observacion, m.PrecioUnitario))
			.ToListAsync(cancellationToken);

		result = Result<List<MovimientoInsumoDto>>.Success(movimientos);
		return result;
	}
}
