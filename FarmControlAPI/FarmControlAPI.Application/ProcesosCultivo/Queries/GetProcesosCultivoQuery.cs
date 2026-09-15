using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.ProcesosCultivo.Queries;

public record ProcesoCultivoDto(
	int Id,
	int ParcelaId,
	string ParcelaNombre,
	int ProductoId,
	string ProductoNombre,
	int? SocioId,
	string? SocioNombre,
	decimal CostoInicial,
	DateTime FechaInicio,
	DateTime? FechaEstimadaCosecha,
	DateTime? FechaCierre,
	EstadoProceso Estado);

public record GetProcesosCultivoQuery(int? ParcelaId, EstadoProceso? Estado) : IRequest<Result<List<ProcesoCultivoDto>>>;

public class GetProcesosCultivoQueryHandler : IRequestHandler<GetProcesosCultivoQuery, Result<List<ProcesoCultivoDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetProcesosCultivoQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<ProcesoCultivoDto>>> Handle(GetProcesosCultivoQuery request, CancellationToken cancellationToken)
	{
		Result<List<ProcesoCultivoDto>> result;

		var query = _context.ProcesosCultivo
			.Include(pc => pc.Parcela)
			.Include(pc => pc.Producto)
			.Include(pc => pc.Socio)
			.AsQueryable();

		if (request.ParcelaId.HasValue)
		{
			query = query.Where(pc => pc.ParcelaId == request.ParcelaId.Value);
		}

		if (request.Estado.HasValue)
		{
			query = query.Where(pc => pc.Estado == request.Estado.Value);
		}

		var procesos = await query
			.OrderByDescending(pc => pc.FechaInicio)
			.Select(pc => new ProcesoCultivoDto(
				pc.Id,
				pc.ParcelaId,
				pc.Parcela.Nombre,
				pc.ProductoId,
				pc.Producto.Nombre,
				pc.SocioId,
				pc.Socio != null ? pc.Socio.Nombre : null,
				pc.CostoInicial,
				pc.FechaInicio,
				pc.FechaEstimadaCosecha,
				pc.FechaCierre,
				pc.Estado))
			.ToListAsync(cancellationToken);

		result = Result<List<ProcesoCultivoDto>>.Success(procesos);
		return result;
	}
}
