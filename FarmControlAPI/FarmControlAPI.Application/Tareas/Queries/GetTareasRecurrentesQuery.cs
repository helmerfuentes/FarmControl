using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Tareas.Queries;

public record TareaRecurrenteDto(
	int Id,
	int ParcelaId,
	string ParcelaNombre,
	string Descripcion,
	int FrecuenciaDias,
	DateTime ProximaFecha,
	DateTime? UltimaEjecucion,
	bool Activa);

public record GetTareasRecurrentesQuery(int? ParcelaId) : IRequest<Result<List<TareaRecurrenteDto>>>;

public class GetTareasRecurrentesQueryHandler : IRequestHandler<GetTareasRecurrentesQuery, Result<List<TareaRecurrenteDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetTareasRecurrentesQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<TareaRecurrenteDto>>> Handle(GetTareasRecurrentesQuery request, CancellationToken cancellationToken)
	{
		var query = _context.TareasRecurrentes.Include(t => t.Parcela).AsQueryable();

		if (request.ParcelaId.HasValue)
		{
			query = query.Where(t => t.ParcelaId == request.ParcelaId.Value);
		}

		var tareas = await query
			.OrderBy(t => t.ProximaFecha)
			.Select(t => new TareaRecurrenteDto(
				t.Id, t.ParcelaId, t.Parcela.Nombre, t.Descripcion,
				t.FrecuenciaDias, t.ProximaFecha, t.UltimaEjecucion, t.Activa))
			.ToListAsync(cancellationToken);

		var result = Result<List<TareaRecurrenteDto>>.Success(tareas);
		return result;
	}
}
