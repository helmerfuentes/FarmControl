using FarmControlAPI.Application.Compras.Commands;
using FarmControlAPI.Application.Compras.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class ComprasEndpoints
{
	internal static void MapComprasEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/compras").RequireAuthorization();

		group.MapGet("/", async (int? fincaId, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetComprasQuery(fincaId));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreateCompraCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/compras/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		group.MapPut("/{id:int}", async (int id, UpdateCompraCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteCompraCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapGet("/{id:int}/pagos", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetPagosCompraQuery(id));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/{id:int}/pagos", async (int id, RegistrarPagoCompraCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { CompraId = id });
			return EndpointHelpers.ToCreated(result, $"/compras/{id}/pagos/{result.Value?.Id}");
		}).RequireAuthorization("Admin");
	}
}
