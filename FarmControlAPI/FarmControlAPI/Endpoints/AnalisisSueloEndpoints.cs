using FarmControlAPI.Application.AnalisisSuelos.Commands;
using FarmControlAPI.Application.AnalisisSuelos.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class AnalisisSueloEndpoints
{
	internal static void MapAnalisisSueloEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/analisis-suelo").RequireAuthorization();

		group.MapGet("/", async (int parcelaId, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetAnalisisSueloQuery(parcelaId));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreateAnalisisSueloCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/analisis-suelo/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteAnalisisSueloCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");
	}
}
