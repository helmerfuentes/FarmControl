using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.AnalisisSuelos.Queries;

public record AnalisisSueloDto(
	int Id,
	int ParcelaId,
	DateTime Fecha,
	decimal? Ph,
	decimal? MateriaOrganica,
	decimal? Nitrogeno,
	decimal? Fosforo,
	decimal? Potasio,
	string? Observacion);

public record GetAnalisisSueloQuery(int ParcelaId) : IRequest<Result<List<AnalisisSueloDto>>>;

public class GetAnalisisSueloQueryHandler : IRequestHandler<GetAnalisisSueloQuery, Result<List<AnalisisSueloDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetAnalisisSueloQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<AnalisisSueloDto>>> Handle(GetAnalisisSueloQuery request, CancellationToken cancellationToken)
	{
		var analisis = await _context.AnalisisSuelo
			.Where(a => a.ParcelaId == request.ParcelaId)
			.OrderByDescending(a => a.Fecha)
			.Select(a => new AnalisisSueloDto(
				a.Id, a.ParcelaId, a.Fecha, a.Ph, a.MateriaOrganica,
				a.Nitrogeno, a.Fosforo, a.Potasio, a.Observacion))
			.ToListAsync(cancellationToken);

		var result = Result<List<AnalisisSueloDto>>.Success(analisis);
		return result;
	}
}
