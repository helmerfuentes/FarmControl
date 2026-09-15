using FarmControlAPI.Application.Tareas.Commands;
using FarmControlAPI.Application.Tareas.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class TareasEndpoints
{
	internal static void MapTareasEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/tareas-recurrentes").RequireAuthorization();

		group.MapGet("/", async (int? parcelaId, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetTareasRecurrentesQuery(parcelaId));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreateTareaRecurrenteCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/tareas-recurrentes/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		group.MapPost("/{id:int}/completar", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new CompletarTareaRecurrenteCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteTareaRecurrenteCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");
	}
}
