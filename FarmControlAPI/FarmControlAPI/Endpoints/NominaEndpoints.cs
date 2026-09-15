using FarmControlAPI.Application.Nomina.Commands;
using FarmControlAPI.Application.Nomina.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class NominaEndpoints
{
	internal static void MapNominaEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/nomina").RequireAuthorization("Admin");

		group.MapGet("/pendientes", async (int jornaleroId, DateTime fechaInicio, DateTime fechaFin, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetRegistrosPendientesLiquidacionQuery(jornaleroId, fechaInicio, fechaFin));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/liquidaciones", async (CrearLiquidacionNominaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/nomina/liquidaciones/{result.Value?.Id}");
		});

		group.MapGet("/liquidaciones", async (int? jornaleroId, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetLiquidacionesQuery(jornaleroId));
			return EndpointHelpers.ToResponse(result);
		});
	}
}
