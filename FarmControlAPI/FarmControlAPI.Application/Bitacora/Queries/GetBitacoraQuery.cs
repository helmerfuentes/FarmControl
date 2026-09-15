using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Bitacora.Queries;

public record BitacoraEntryDto(int Id, DateTime FechaHora, string ActorNombre, string Accion, string Detalle);

public record GetBitacoraQuery : IRequest<Result<List<BitacoraEntryDto>>>;

public class GetBitacoraQueryHandler : IRequestHandler<GetBitacoraQuery, Result<List<BitacoraEntryDto>>>
{
	private const int MAX_ENTRADAS = 200;

	private readonly IFarmControlDbContext _context;

	public GetBitacoraQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<BitacoraEntryDto>>> Handle(GetBitacoraQuery request, CancellationToken cancellationToken)
	{
		var entradas = await _context.BitacoraEntries
			.OrderByDescending(b => b.FechaHora)
			.Take(MAX_ENTRADAS)
			.Select(b => new BitacoraEntryDto(b.Id, b.FechaHora, b.ActorNombre, b.Accion, b.Detalle))
			.ToListAsync(cancellationToken);

		var result = Result<List<BitacoraEntryDto>>.Success(entradas);
		return result;
	}
}
