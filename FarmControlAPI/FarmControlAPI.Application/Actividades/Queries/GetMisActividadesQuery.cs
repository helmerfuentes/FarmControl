using FarmControlAPI.Application.Actividades.Commands;
using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Actividades.Queries;

public record GetMisActividadesQuery(DateTime? Fecha) : IRequest<Result<List<ActividadDto>>>;

public class GetMisActividadesQueryHandler : IRequestHandler<GetMisActividadesQuery, Result<List<ActividadDto>>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;

	public GetMisActividadesQueryHandler(IFarmControlDbContext context, ICurrentUserContext currentUser)
	{
		_context = context;
		_currentUser = currentUser;
	}

	public async Task<Result<List<ActividadDto>>> Handle(GetMisActividadesQuery request, CancellationToken cancellationToken)
	{
		Result<List<ActividadDto>> result;

		if (_currentUser.PersonaId is null)
		{
			result = Result<List<ActividadDto>>.Success([]);
			return result;
		}

		var fecha = (request.Fecha ?? DateTime.UtcNow).Date;

		var actividades = await _context.Actividades
			.Where(a => a.PersonaACargoId == _currentUser.PersonaId.Value && a.FechaInicio.Date == fecha)
			.Include(a => a.PersonaACargo)
			.Include(a => a.Parcela).ThenInclude(p => p.Finca)
			.OrderBy(a => a.FechaInicio)
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
