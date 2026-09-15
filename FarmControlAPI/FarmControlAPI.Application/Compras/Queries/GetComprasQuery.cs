using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Compras.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Compras.Queries;

public record GetComprasQuery(int? FincaId) : IRequest<Result<List<CompraDto>>>;

public class GetComprasQueryHandler : IRequestHandler<GetComprasQuery, Result<List<CompraDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetComprasQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<CompraDto>>> Handle(GetComprasQuery request, CancellationToken cancellationToken)
	{
		Result<List<CompraDto>> result;

		var query = _context.Compras.Include(c => c.Socio).Include(c => c.Finca).Include(c => c.Parcela).Include(c => c.Pagos).AsQueryable();

		if (request.FincaId.HasValue)
		{
			query = query.Where(c => c.FincaId == request.FincaId.Value);
		}

		var comprasEntidades = await query
			.OrderByDescending(c => c.Fecha)
			.ToListAsync(cancellationToken);

		var compras = comprasEntidades.Select(c =>
		{
			var totalPagado = c.Pagos.Sum(p => p.Monto);
			return new CompraDto(c.Id, c.FincaId, c.Finca.Nombre, c.SocioId, c.Socio.Nombre, c.Descripcion, c.Valor, c.Fecha, c.TipoCompra, c.AdjuntoUrl, c.ParcelaId, c.Parcela != null ? c.Parcela.Nombre : null, c.ProcesoCultivoId, c.Proveedor, totalPagado, c.Valor - totalPagado);
		}).ToList();

		result = Result<List<CompraDto>>.Success(compras);
		return result;
	}
}
