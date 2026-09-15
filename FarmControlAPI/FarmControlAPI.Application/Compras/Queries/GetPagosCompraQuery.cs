using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Compras.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Compras.Queries;

public record GetPagosCompraQuery(int CompraId) : IRequest<Result<List<PagoCompraDto>>>;

public class GetPagosCompraQueryHandler : IRequestHandler<GetPagosCompraQuery, Result<List<PagoCompraDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetPagosCompraQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<PagoCompraDto>>> Handle(GetPagosCompraQuery request, CancellationToken cancellationToken)
	{
		var pagos = await _context.PagosCompra
			.Where(p => p.CompraId == request.CompraId)
			.OrderByDescending(p => p.Fecha)
			.Select(p => new PagoCompraDto(p.Id, p.CompraId, p.Monto, p.Fecha, p.Observacion))
			.ToListAsync(cancellationToken);

		var result = Result<List<PagoCompraDto>>.Success(pagos);
		return result;
	}
}
