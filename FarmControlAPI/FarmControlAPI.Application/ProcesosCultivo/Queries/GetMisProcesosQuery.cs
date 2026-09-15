using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.ProcesosCultivo.Queries;

public record GetMisProcesosQuery(int SocioId) : IRequest<Result<List<ProcesoCultivoDto>>>;

public class GetMisProcesosQueryHandler : IRequestHandler<GetMisProcesosQuery, Result<List<ProcesoCultivoDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetMisProcesosQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<ProcesoCultivoDto>>> Handle(GetMisProcesosQuery request, CancellationToken cancellationToken)
	{
		Result<List<ProcesoCultivoDto>> result;

		var procesos = await _context.ProcesosCultivo
			.Include(pc => pc.Parcela)
			.Include(pc => pc.Producto)
			.Include(pc => pc.Socio)
			.Where(pc => pc.SocioId == request.SocioId)
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
