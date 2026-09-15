using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Planes.Queries;

public record PlanDto(int Id, string Nombre, string? Descripcion, int MaxFincas, int MaxUsuarios);

public record GetPlanesQuery : IRequest<Result<List<PlanDto>>>;

public class GetPlanesQueryHandler : IRequestHandler<GetPlanesQuery, Result<List<PlanDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetPlanesQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<PlanDto>>> Handle(GetPlanesQuery request, CancellationToken cancellationToken)
	{
		Result<List<PlanDto>> result;

		var planes = await _context.Planes
			.OrderBy(p => p.Nombre)
			.Select(p => new PlanDto(p.Id, p.Nombre, p.Descripcion, p.MaxFincas, p.MaxUsuarios))
			.ToListAsync(cancellationToken);

		result = Result<List<PlanDto>>.Success(planes);
		return result;
	}
}
