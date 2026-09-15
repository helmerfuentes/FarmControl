using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Ventas.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Ventas.Queries;

public record GetPagosVentaQuery(int VentaId) : IRequest<Result<List<PagoVentaDto>>>;

public class GetPagosVentaQueryHandler : IRequestHandler<GetPagosVentaQuery, Result<List<PagoVentaDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetPagosVentaQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<PagoVentaDto>>> Handle(GetPagosVentaQuery request, CancellationToken cancellationToken)
	{
		var pagos = await _context.PagosVenta
			.Where(p => p.VentaId == request.VentaId)
			.OrderByDescending(p => p.Fecha)
			.Select(p => new PagoVentaDto(p.Id, p.VentaId, p.Monto, p.Fecha, p.Observacion))
			.ToListAsync(cancellationToken);

		var result = Result<List<PagoVentaDto>>.Success(pagos);
		return result;
	}
}
