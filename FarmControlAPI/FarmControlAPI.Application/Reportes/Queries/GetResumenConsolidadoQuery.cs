using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Reportes.Queries;

public record ResumenConsolidadoFincaDto(
	int FincaId,
	string Nombre,
	decimal TotalIngresos,
	decimal TotalCompras,
	decimal TotalManoObra,
	decimal Utilidad);

public record ResumenConsolidadoDto(
	decimal TotalIngresos,
	decimal TotalCompras,
	decimal TotalManoObra,
	decimal Utilidad,
	List<ResumenConsolidadoFincaDto> Fincas);

public record GetResumenConsolidadoQuery : IRequest<Result<ResumenConsolidadoDto>>;

public class GetResumenConsolidadoQueryHandler : IRequestHandler<GetResumenConsolidadoQuery, Result<ResumenConsolidadoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IMediator _mediator;

	public GetResumenConsolidadoQueryHandler(IFarmControlDbContext context, IMediator mediator)
	{
		_context = context;
		_mediator = mediator;
	}

	public async Task<Result<ResumenConsolidadoDto>> Handle(GetResumenConsolidadoQuery request, CancellationToken cancellationToken)
	{
		var fincaIds = await _context.Fincas
			.OrderBy(f => f.Nombre)
			.Select(f => f.Id)
			.ToListAsync(cancellationToken);

		var resumenesFinca = new List<ResumenConsolidadoFincaDto>();

		foreach (var fincaId in fincaIds)
		{
			var resumenFinca = await _mediator.Send(new GetResumenFincaQuery(fincaId), cancellationToken);
			if (resumenFinca.IsSuccess && resumenFinca.Value is not null)
			{
				resumenesFinca.Add(new ResumenConsolidadoFincaDto(
					resumenFinca.Value.FincaId,
					resumenFinca.Value.Nombre,
					resumenFinca.Value.TotalIngresos,
					resumenFinca.Value.TotalCompras,
					resumenFinca.Value.TotalManoObra,
					resumenFinca.Value.Utilidad));
			}
		}

		var consolidado = new ResumenConsolidadoDto(
			resumenesFinca.Sum(r => r.TotalIngresos),
			resumenesFinca.Sum(r => r.TotalCompras),
			resumenesFinca.Sum(r => r.TotalManoObra),
			resumenesFinca.Sum(r => r.Utilidad),
			resumenesFinca);

		var result = Result<ResumenConsolidadoDto>.Success(consolidado);
		return result;
	}
}
