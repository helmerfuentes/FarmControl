using FarmControlAPI.Application.Insumos.Commands;
using FarmControlAPI.Application.Insumos.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class InsumosEndpoints
{
	internal static void MapInsumosEndpoints(this WebApplication app)
	{
		var insumos = app.MapGroup("/insumos").RequireAuthorization();

		insumos.MapGet("/", async (int? tipoInsumoId, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetInsumosQuery(tipoInsumoId));
			return EndpointHelpers.ToResponse(result);
		});

		insumos.MapGet("/saldos", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetSaldosInsumoQuery());
			return EndpointHelpers.ToResponse(result);
		});

		insumos.MapGet("/{id:int}/historial-precios", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetHistorialPrecioInsumoQuery(id));
			return EndpointHelpers.ToResponse(result);
		});

		insumos.MapPost("/", async (CreateInsumoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/insumos/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		insumos.MapPut("/{id:int}", async (int id, UpdateInsumoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		insumos.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteInsumoCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		var movimientos = app.MapGroup("/movimientos-insumo").RequireAuthorization();

		movimientos.MapGet("/", async (int? insumoId, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetMovimientosInsumoQuery(insumoId));
			return EndpointHelpers.ToResponse(result);
		});

		movimientos.MapPost("/", async (CreateMovimientoInsumoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/movimientos-insumo/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		movimientos.MapPost("/transferir", async (TransferirInsumoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");
	}
}
