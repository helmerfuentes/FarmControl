using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Fincas.Queries;

public record GetParcelasQuery(int FincaId) : IRequest<Result<List<ParcelaResumenDto>>>;

public class GetParcelasQueryHandler : IRequestHandler<GetParcelasQuery, Result<List<ParcelaResumenDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetParcelasQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<ParcelaResumenDto>>> Handle(GetParcelasQuery request, CancellationToken cancellationToken)
	{
		Result<List<ParcelaResumenDto>> result;

		var parcelas = await _context.Parcelas
			.Where(p => p.FincaId == request.FincaId)
			.OrderBy(p => p.Nombre)
			.Select(p => new ParcelaResumenDto(p.Id, p.Nombre, p.Area))
			.ToListAsync(cancellationToken);

		result = Result<List<ParcelaResumenDto>>.Success(parcelas);
		return result;
	}
}
