using FarmControlAPI.Application.Actividades.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Reportes.Queries;

public record ResumenProcesoCultivoDto(
	int ProcesoCultivoId,
	string ParcelaNombre,
	string ProductoNombre,
	EstadoProceso Estado,
	DateTime FechaInicio,
	DateTime? FechaEstimadaCosecha,
	DateTime? FechaCierre,
	decimal CostoInicial,
	decimal TotalIngresos,
	decimal TotalCompras,
	decimal TotalManoObra,
	decimal Utilidad);

public record GetResumenProcesoCultivoQuery(int ProcesoCultivoId) : IRequest<Result<ResumenProcesoCultivoDto>>;

public class GetResumenProcesoCultivoQueryHandler : IRequestHandler<GetResumenProcesoCultivoQuery, Result<ResumenProcesoCultivoDto>>
{
	private readonly IFarmControlDbContext _context;

	public GetResumenProcesoCultivoQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<ResumenProcesoCultivoDto>> Handle(GetResumenProcesoCultivoQuery request, CancellationToken cancellationToken)
	{
		Result<ResumenProcesoCultivoDto> result;

		var proceso = await _context.ProcesosCultivo
			.Include(pc => pc.Parcela)
			.Include(pc => pc.Producto)
			.FirstOrDefaultAsync(pc => pc.Id == request.ProcesoCultivoId, cancellationToken);

		if (proceso is null)
		{
			result = Result<ResumenProcesoCultivoDto>.Failure($"Proceso de cultivo con Id {request.ProcesoCultivoId} no encontrado.");
			return result;
		}

		var totalIngresos = (await _context.Ventas
			.Where(v => v.ProcesoCultivoId == request.ProcesoCultivoId)
			.Include(v => v.Detalles)
			.ToListAsync(cancellationToken))
			.Sum(v => v.Detalles.Sum(d => d.Subtotal));

		var totalComprasDirectas = (await _context.Compras
			.Where(c => c.ProcesoCultivoId == request.ProcesoCultivoId)
			.Select(c => c.Valor)
			.ToListAsync(cancellationToken))
			.Sum();

		var totalCompras = totalComprasDirectas + proceso.CostoInicial;

		var totalManoObra = (await _context.Actividades
			.Where(a => a.ProcesoCultivoId == request.ProcesoCultivoId)
			.ToListAsync(cancellationToken))
			.Sum(a => CreateActividadCommandHandler.CalcularCosto(a.FechaInicio, a.FechaFin, a.ValorDiaUsado));

		var utilidad = totalIngresos - totalCompras - totalManoObra;

		result = Result<ResumenProcesoCultivoDto>.Success(new ResumenProcesoCultivoDto(
			proceso.Id, proceso.Parcela.Nombre, proceso.Producto.Nombre, proceso.Estado,
			proceso.FechaInicio, proceso.FechaEstimadaCosecha, proceso.FechaCierre,
			proceso.CostoInicial, totalIngresos, totalCompras, totalManoObra, utilidad));
		return result;
	}
}
