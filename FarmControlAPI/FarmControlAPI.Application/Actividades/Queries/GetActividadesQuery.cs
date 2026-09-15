using FarmControlAPI.Application.Actividades.Commands;
using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Actividades.Queries;

public record GetActividadesQuery(int? ParcelaId, DateTime? Desde, DateTime? Hasta) : IRequest<Result<List<ActividadDto>>>;

public class GetActividadesQueryHandler : IRequestHandler<GetActividadesQuery, Result<List<ActividadDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetActividadesQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<ActividadDto>>> Handle(GetActividadesQuery request, CancellationToken cancellationToken)
	{
		Result<List<ActividadDto>> result;

		var query = _context.Actividades.AsQueryable();

		if (request.ParcelaId.HasValue)
		{
			query = query.Where(a => a.ParcelaId == request.ParcelaId.Value);
		}

		if (request.Desde.HasValue)
		{
			query = query.Where(a => a.FechaInicio >= request.Desde.Value);
		}

		if (request.Hasta.HasValue)
		{
			query = query.Where(a => a.FechaInicio <= request.Hasta.Value);
		}

		var actividades = await query
			.Include(a => a.PersonaACargo)
			.Include(a => a.Parcela).ThenInclude(p => p.Finca)
			.OrderByDescending(a => a.FechaInicio)
			.ToListAsync(cancellationToken);

		var dtos = actividades.Select(a => new ActividadDto(
			a.Id, a.ParcelaId, a.TipoActividad,
			a.FechaInicio, a.FechaFin,
			a.PersonaACargoId,
			a.PersonaACargo?.Nombre,
			a.Descripcion,
			a.ValorDiaUsado,
			CreateActividadCommandHandler.CalcularCosto(a.FechaInicio, a.FechaFin, a.ValorDiaUsado),
			a.Parcela.Nombre,
			a.Parcela.Finca.Nombre,
			a.ProcesoCultivoId,
			a.Confirmada,
			a.FechaConfirmacion,
			a.ConfirmadaPor))
			.ToList();

		result = Result<List<ActividadDto>>.Success(dtos);
		return result;
	}
}
