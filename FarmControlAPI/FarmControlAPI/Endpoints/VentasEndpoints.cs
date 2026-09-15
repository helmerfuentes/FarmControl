using FarmControlAPI.Application.Ventas.Commands;
using FarmControlAPI.Application.Ventas.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class VentasEndpoints
{
	internal static void MapVentasEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/ventas").RequireAuthorization();

		group.MapGet("/", async (int? parcelaId, DateTime? desde, DateTime? hasta, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetVentasQuery(parcelaId, desde, hasta));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreateVentaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/ventas/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteVentaCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapGet("/compradores-frecuentes", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetCompradoresFrecuentesQuery());
			return EndpointHelpers.ToResponse(result);
		});

		group.MapGet("/{id:int}/pagos", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetPagosVentaQuery(id));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/{id:int}/pagos", async (int id, RegistrarPagoVentaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { VentaId = id });
			return EndpointHelpers.ToCreated(result, $"/ventas/{id}/pagos/{result.Value?.Id}");
		}).RequireAuthorization("Admin");
	}
}
