using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.TiposInsumo.Queries;

public record TipoInsumoDto(int Id, string Nombre, string? Descripcion);

public record GetTiposInsumoQuery : IRequest<Result<List<TipoInsumoDto>>>;

public class GetTiposInsumoQueryHandler : IRequestHandler<GetTiposInsumoQuery, Result<List<TipoInsumoDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetTiposInsumoQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<TipoInsumoDto>>> Handle(GetTiposInsumoQuery request, CancellationToken cancellationToken)
	{
		Result<List<TipoInsumoDto>> result;

		var tipos = await _context.TiposInsumo
			.OrderBy(t => t.Nombre)
			.Select(t => new TipoInsumoDto(t.Id, t.Nombre, t.Descripcion))
			.ToListAsync(cancellationToken);

		result = Result<List<TipoInsumoDto>>.Success(tipos);
		return result;
	}
}
