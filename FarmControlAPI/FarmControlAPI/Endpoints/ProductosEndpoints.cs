using FarmControlAPI.Application.Productos.Commands;
using FarmControlAPI.Application.Productos.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class ProductosEndpoints
{
	internal static void MapProductosEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/productos").RequireAuthorization();

		group.MapGet("/", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetProductosQuery());
			return EndpointHelpers.ToResponse(result);
		});

		group.MapGet("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetProductoByIdQuery(id));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreateProductoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/productos/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		group.MapPut("/{id:int}", async (int id, UpdateProductoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteProductoCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");
	}
}
