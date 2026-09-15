using FarmControlAPI.Application.TiposInsumo.Commands;
using FarmControlAPI.Application.TiposInsumo.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class TiposInsumoEndpoints
{
	internal static void MapTiposInsumoEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/tipos-insumo").RequireAuthorization();

		group.MapGet("/", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetTiposInsumoQuery());
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreateTipoInsumoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/tipos-insumo/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		group.MapPut("/{id:int}", async (int id, UpdateTipoInsumoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteTipoInsumoCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");
	}
}
