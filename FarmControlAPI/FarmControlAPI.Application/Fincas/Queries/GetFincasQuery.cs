using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Fincas.Queries;

public record ParcelaResumenDto(int Id, string Nombre, decimal Area);

public record FincaDto(int Id, string Nombre, string? Ubicacion, decimal AreaTotal, decimal CostoTerreno, List<ParcelaResumenDto> Parcelas);

public record GetFincasQuery : IRequest<Result<List<FincaDto>>>;

public class GetFincasQueryHandler : IRequestHandler<GetFincasQuery, Result<List<FincaDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetFincasQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<FincaDto>>> Handle(GetFincasQuery request, CancellationToken cancellationToken)
	{
		Result<List<FincaDto>> result;

		var fincas = await _context.Fincas
			.Include(f => f.Parcelas)
			.OrderBy(f => f.Nombre)
			.Select(f => new FincaDto(
				f.Id,
				f.Nombre,
				f.Ubicacion,
				f.AreaTotal,
				f.CostoTerreno,
				f.Parcelas.Select(p => new ParcelaResumenDto(p.Id, p.Nombre, p.Area)).ToList()))
			.ToListAsync(cancellationToken);

		result = Result<List<FincaDto>>.Success(fincas);
		return result;
	}
}
