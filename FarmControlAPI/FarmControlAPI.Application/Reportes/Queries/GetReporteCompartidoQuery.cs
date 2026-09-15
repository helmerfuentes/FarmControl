using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Reportes.Queries;

public record GetReporteCompartidoQuery(string Token) : IRequest<Result<ResumenFincaDto>>;

/// <summary>
/// Resuelve un enlace público de solo lectura generado por CrearReporteCompartidoCommand.
/// Es deliberadamente anónimo (sin JWT), así que aquí SIEMPRE se usa IgnoreQueryFilters():
/// el aislamiento no viene del tenant del llamante (no existe) sino de que el Token es
/// impredecible y de un solo uso por finca. No reutiliza GetResumenFincaQueryHandler para
/// no tener que introducir un bypass de tenant en el handler que sí usan los usuarios autenticados.
/// </summary>
public class GetReporteCompartidoQueryHandler : IRequestHandler<GetReporteCompartidoQuery, Result<ResumenFincaDto>>
{
	private readonly IFarmControlDbContext _context;

	public GetReporteCompartidoQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<ResumenFincaDto>> Handle(GetReporteCompartidoQuery request, CancellationToken cancellationToken)
	{
		Result<ResumenFincaDto> result;

		var compartido = await _context.ReportesCompartidos
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(r => r.Token == request.Token, cancellationToken);

		if (compartido is null)
		{
			result = Result<ResumenFincaDto>.Failure("Este enlace no es válido.");
			return result;
		}

		if (compartido.FechaExpiracion.HasValue && compartido.FechaExpiracion.Value < DateTime.UtcNow)
		{
			result = Result<ResumenFincaDto>.Failure("Este enlace ha expirado.");
			return result;
		}

		var finca = await _context.Fincas
			.IgnoreQueryFilters()
			.Include(f => f.Parcelas)
				.ThenInclude(p => p.ProcesosCultivo)
				.ThenInclude(pc => pc.Producto)
			.FirstOrDefaultAsync(f => f.Id == compartido.FincaId, cancellationToken);

		if (finca is null)
		{
			result = Result<ResumenFincaDto>.Failure("La finca de este enlace ya no existe.");
			return result;
		}

		var parcelaIds = finca.Parcelas.Select(p => p.Id).ToList();

		var ventasFinca = await _context.Ventas
			.IgnoreQueryFilters()
			.Where(v => parcelaIds.Contains(v.ParcelaId))
			.Include(v => v.Detalles)
			.ToListAsync(cancellationToken);

		var totalIngresos = ventasFinca.Sum(v => v.Detalles.Sum(d => d.Subtotal));

		var totalCompras = (await _context.Compras
			.IgnoreQueryFilters()
			.Where(c => c.FincaId == compartido.FincaId)
			.Select(c => c.Valor)
			.ToListAsync(cancellationToken)).Sum();

		var totalCostosInicialesProcesos = finca.Parcelas
			.SelectMany(p => p.ProcesosCultivo)
			.Sum(pc => pc.CostoInicial);

		totalCompras += totalCostosInicialesProcesos;

		var actividadesFinca = await _context.Actividades
			.IgnoreQueryFilters()
			.Where(a => parcelaIds.Contains(a.ParcelaId))
			.ToListAsync(cancellationToken);

		var totalManoObra = actividadesFinca.Sum(a => CalcularCostoActividad(a));

		var parcelaResumenes = new List<ResumenParcelaDto>();

		foreach (var parcela in finca.Parcelas)
		{
			var ventasParcela = ventasFinca.Where(v => v.ParcelaId == parcela.Id).ToList();
			var ingresosParc = ventasParcela.Sum(v => v.Detalles.Sum(d => d.Subtotal));

			var actividadesParcela = actividadesFinca.Where(a => a.ParcelaId == parcela.Id).ToList();
			var manoObraParc = actividadesParcela.Sum(a => CalcularCostoActividad(a));

			var productoNombre = parcela.ProcesosCultivo
				.FirstOrDefault(pc => pc.Estado == EstadoProceso.Activo)?.Producto?.Nombre;

			parcelaResumenes.Add(new ResumenParcelaDto(
				parcela.Id, parcela.Nombre, productoNombre,
				parcela.Area, ingresosParc, manoObraParc));
		}

		var utilidad = totalIngresos - totalCompras - totalManoObra;

		result = Result<ResumenFincaDto>.Success(new ResumenFincaDto(
			finca.Id, finca.Nombre, finca.Ubicacion, finca.AreaTotal,
			totalIngresos, totalCompras, totalManoObra, utilidad, parcelaResumenes));
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
