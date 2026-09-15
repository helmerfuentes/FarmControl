using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Reportes.Queries;

public record ResumenParcelaDto(
	int Id,
	string Nombre,
	string? Producto,
	decimal Area,
	decimal TotalIngresos,
	decimal TotalManoObra);

public record ResumenFincaDto(
	int FincaId,
	string Nombre,
	string? Ubicacion,
	decimal AreaTotal,
	decimal TotalIngresos,
	decimal TotalCompras,
	decimal TotalManoObra,
	decimal Utilidad,
	List<ResumenParcelaDto> Parcelas);

public record GetResumenFincaQuery(int FincaId) : IRequest<Result<ResumenFincaDto>>;

public class GetResumenFincaQueryHandler : IRequestHandler<GetResumenFincaQuery, Result<ResumenFincaDto>>
{
	private readonly IFarmControlDbContext _context;

	public GetResumenFincaQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<ResumenFincaDto>> Handle(GetResumenFincaQuery request, CancellationToken cancellationToken)
	{
		Result<ResumenFincaDto> result;

		var finca = await _context.Fincas
			.Include(f => f.Parcelas)
				.ThenInclude(p => p.ProcesosCultivo)
				.ThenInclude(pc => pc.Producto)
			.FirstOrDefaultAsync(f => f.Id == request.FincaId, cancellationToken);

		if (finca is null)
		{
			result = Result<ResumenFincaDto>.Failure($"Finca con Id {request.FincaId} no encontrada.");
			return result;
		}

		var parcelaIds = finca.Parcelas.Select(p => p.Id).ToList();

		var ventasFinca = await _context.Ventas
			.Where(v => parcelaIds.Contains(v.ParcelaId))
			.Include(v => v.Detalles)
			.ToListAsync(cancellationToken);

		var totalIngresos = ventasFinca.Sum(v => v.Detalles.Sum(d => d.Subtotal));

		var totalCompras = (await _context.Compras
			.Where(c => c.FincaId == request.FincaId)
			.Select(c => c.Valor)
			.ToListAsync(cancellationToken)).Sum();

		var totalCostosInicialesProcesos = finca.Parcelas
			.SelectMany(p => p.ProcesosCultivo)
			.Sum(pc => pc.CostoInicial);

		totalCompras += totalCostosInicialesProcesos;

		var actividadesFinca = await _context.Actividades
			.Where(a => parcelaIds.Contains(a.ParcelaId))
			.ToListAsync(cancellationToken);

		var totalManoObra = actividadesFinca.Sum(a => CalcularCostoActividad(a));

		var parcelaResumenes = new List<ResumenParcelaDto>();

		foreach (var parcela in finca.Parcelas)
		{
			var ventasParcela = ventasFinca.Where(v => v.ParcelaId == parcela.Id).ToList();
			var ingresosParc = ventasParcela.Sum(v => v.Detalles.Sum(d => d.Subtotal));

			var actividadesParcela = actividadesFinca
				.Where(a => a.ParcelaId == parcela.Id)
				.ToList();

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
