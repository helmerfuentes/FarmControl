using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Insumos.Queries;

public record SaldoInsumoDto(int InsumoId, string InsumoNombre, string UnidadMedida, decimal Entradas, decimal Salidas, decimal Saldo, decimal? StockMinimo, bool SaldoBajo);

public record GetSaldosInsumoQuery : IRequest<Result<List<SaldoInsumoDto>>>;

public class GetSaldosInsumoQueryHandler : IRequestHandler<GetSaldosInsumoQuery, Result<List<SaldoInsumoDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetSaldosInsumoQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<SaldoInsumoDto>>> Handle(GetSaldosInsumoQuery request, CancellationToken cancellationToken)
	{
		var insumos = await _context.Insumos
			.OrderBy(i => i.Nombre)
			.ToListAsync(cancellationToken);

		var movimientos = await _context.MovimientosInsumo.ToListAsync(cancellationToken);

		var saldos = new List<SaldoInsumoDto>();

		foreach (var insumo in insumos)
		{
			var movimientosDelInsumo = movimientos.Where(m => m.InsumoId == insumo.Id).ToList();
			var entradas = movimientosDelInsumo
				.Where(m => m.TipoMovimiento == TipoMovimientoInsumo.Entrada)
				.Sum(m => m.Cantidad);
			var salidas = movimientosDelInsumo
				.Where(m => m.TipoMovimiento == TipoMovimientoInsumo.Salida)
				.Sum(m => m.Cantidad);

			var saldo = entradas - salidas;
			var saldoBajo = insumo.StockMinimo.HasValue && saldo <= insumo.StockMinimo.Value;

			saldos.Add(new SaldoInsumoDto(insumo.Id, insumo.Nombre, insumo.UnidadMedida, entradas, salidas, saldo, insumo.StockMinimo, saldoBajo));
		}

		var result = Result<List<SaldoInsumoDto>>.Success(saldos);
		return result;
	}
}
