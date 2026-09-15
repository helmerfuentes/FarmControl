using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Reportes.Queries;

public record MesResumenDto(int Anio, int Mes, decimal TotalIngresos, decimal TotalCompras, decimal TotalManoObra, decimal Utilidad);

public record HistoricoFincaDto(int FincaId, string Nombre, List<MesResumenDto> Meses);

public record GetHistoricoFincaQuery(int FincaId, int MesesAtras = 12) : IRequest<Result<HistoricoFincaDto>>;

public class GetHistoricoFincaQueryHandler : IRequestHandler<GetHistoricoFincaQuery, Result<HistoricoFincaDto>>
{
	private readonly IFarmControlDbContext _context;

	public GetHistoricoFincaQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<HistoricoFincaDto>> Handle(GetHistoricoFincaQuery request, CancellationToken cancellationToken)
	{
		Result<HistoricoFincaDto> result;

		var finca = await _context.Fincas
			.Include(f => f.Parcelas)
				.ThenInclude(p => p.ProcesosCultivo)
			.FirstOrDefaultAsync(f => f.Id == request.FincaId, cancellationToken);

		if (finca is null)
		{
			result = Result<HistoricoFincaDto>.Failure($"Finca con Id {request.FincaId} no encontrada.");
			return result;
		}

		var parcelaIds = finca.Parcelas.Select(p => p.Id).ToList();
		var hoy = DateTime.UtcNow;
		var primerMes = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-(request.MesesAtras - 1));

		var ventas = await _context.Ventas
			.Where(v => parcelaIds.Contains(v.ParcelaId) && v.Fecha >= primerMes)
			.Include(v => v.Detalles)
			.ToListAsync(cancellationToken);

		var compras = await _context.Compras
			.Where(c => c.FincaId == request.FincaId && c.Fecha >= primerMes)
			.ToListAsync(cancellationToken);

		var actividades = await _context.Actividades
			.Where(a => parcelaIds.Contains(a.ParcelaId) && a.FechaInicio >= primerMes)
			.ToListAsync(cancellationToken);

		var costosIniciales = finca.Parcelas
			.SelectMany(p => p.ProcesosCultivo)
			.Where(pc => pc.FechaInicio >= primerMes)
			.ToList();

		var meses = new List<MesResumenDto>();

		for (var i = 0; i < request.MesesAtras; i++)
		{
			var mesActual = primerMes.AddMonths(i);

			var ingresosMes = ventas
				.Where(v => v.Fecha.Year == mesActual.Year && v.Fecha.Month == mesActual.Month)
				.Sum(v => v.Detalles.Sum(d => d.Subtotal));

			var comprasMes = compras
				.Where(c => c.Fecha.Year == mesActual.Year && c.Fecha.Month == mesActual.Month)
				.Sum(c => c.Valor);

			var costoInicialMes = costosIniciales
				.Where(pc => pc.FechaInicio.Year == mesActual.Year && pc.FechaInicio.Month == mesActual.Month)
				.Sum(pc => pc.CostoInicial);

			var manoObraMes = actividades
				.Where(a => a.FechaInicio.Year == mesActual.Year && a.FechaInicio.Month == mesActual.Month)
				.Sum(a => CalcularCostoActividad(a));

			var totalComprasMes = comprasMes + costoInicialMes;

			meses.Add(new MesResumenDto(
				mesActual.Year, mesActual.Month,
				ingresosMes, totalComprasMes, manoObraMes,
				ingresosMes - totalComprasMes - manoObraMes));
		}

		result = Result<HistoricoFincaDto>.Success(new HistoricoFincaDto(finca.Id, finca.Nombre, meses));
		return result;
	}

	private static decimal CalcularCostoActividad(Domain.Entities.Actividad actividad)
	{
		decimal costo = 0;
		if (actividad.FechaFin.HasValue && actividad.ValorDiaUsado > 0)
		{
			var horas = (decimal)(actividad.FechaFin.Value - actividad.FechaInicio).TotalHours;
			costo = Math.Round(horas / 8m * actividad.ValorDiaUsado, 0);
		}
		return costo;
	}
}
