using FarmControlAPI.Application.Planes.Commands;
using FarmControlAPI.Application.Planes.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class PlanesEndpoints
{
	internal static void MapPlanesEndpoints(this WebApplication app)
	{
		var planes = app.MapGroup("/planes").RequireAuthorization("SuperAdmin");

		planes.MapGet("/", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetPlanesQuery());
			return EndpointHelpers.ToResponse(result);
		});

		planes.MapPost("/", async (CreatePlanCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/planes/{result.Value?.Id}");
		});

		planes.MapPut("/{id:int}", async (int id, UpdatePlanCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		});

		planes.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeletePlanCommand(id));
			return EndpointHelpers.ToResponse(result);
		});
	}
}
