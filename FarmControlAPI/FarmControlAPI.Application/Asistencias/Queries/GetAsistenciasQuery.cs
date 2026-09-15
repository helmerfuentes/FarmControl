using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Asistencias.Queries;

public record AsistenciaDto(
	int Id,
	int PersonaId,
	string PersonaNombre,
	int FincaId,
	string FincaNombre,
	DateTime Fecha,
	TimeSpan? HoraEntrada,
	TimeSpan? HoraSalida,
	string? Observacion);

public record GetAsistenciasQuery(int? PersonaId, int? FincaId, DateTime? Desde, DateTime? Hasta) : IRequest<Result<List<AsistenciaDto>>>;

public class GetAsistenciasQueryHandler : IRequestHandler<GetAsistenciasQuery, Result<List<AsistenciaDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetAsistenciasQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<AsistenciaDto>>> Handle(GetAsistenciasQuery request, CancellationToken cancellationToken)
	{
		var query = _context.Asistencias
			.Include(a => a.Persona)
			.Include(a => a.Finca)
			.AsQueryable();

		if (request.PersonaId.HasValue)
		{
			query = query.Where(a => a.PersonaId == request.PersonaId.Value);
		}

		if (request.FincaId.HasValue)
		{
			query = query.Where(a => a.FincaId == request.FincaId.Value);
		}

		if (request.Desde.HasValue)
		{
			query = query.Where(a => a.Fecha >= request.Desde.Value);
		}

		if (request.Hasta.HasValue)
		{
			query = query.Where(a => a.Fecha <= request.Hasta.Value);
		}

		var asistencias = await query
			.OrderByDescending(a => a.Fecha)
			.Select(a => new AsistenciaDto(
				a.Id, a.PersonaId, a.Persona.Nombre, a.FincaId, a.Finca.Nombre,
				a.Fecha, a.HoraEntrada, a.HoraSalida, a.Observacion))
			.ToListAsync(cancellationToken);

		var result = Result<List<AsistenciaDto>>.Success(asistencias);
		return result;
	}
}
