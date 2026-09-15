using FarmControlAPI.Application.Actividades.Commands;
using FarmControlAPI.Application.Actividades.Queries;
using MediatR;


namespace FarmControlAPI.Endpoints;

internal record ConfirmarActividadRequest(string ConfirmadaPor);

internal static class ActividadesEndpoints
{
	internal static void MapActividadesEndpoints(this WebApplication app)
	{
		var actividades = app.MapGroup("/actividades").RequireAuthorization();

		actividades.MapGet("/", async (int? parcelaId, DateTime? desde, DateTime? hasta, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetActividadesQuery(parcelaId, desde, hasta));
			return EndpointHelpers.ToResponse(result);
		});

		actividades.MapPost("/", async (CreateActividadCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/actividades/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		actividades.MapPut("/{id:int}", async (int id, UpdateActividadCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		actividades.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteActividadCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		actividades.MapPost("/{id:int}/confirmar", async (int id, ConfirmarActividadRequest body, IMediator mediator) =>
		{
			var result = await mediator.Send(new ConfirmarActividadCommand(id, body.ConfirmadaPor));
			return EndpointHelpers.ToResponse(result);
		});

		actividades.MapGet("/mis-actividades", async (DateTime? fecha, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetMisActividadesQuery(fecha));
			return EndpointHelpers.ToResponse(result);
		});

		var manoObra = app.MapGroup("/mano-obra").RequireAuthorization();

		manoObra.MapPost("/", async (CreateManoObraCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/mano-obra/{result.Value?.Id}");
		}).RequireAuthorization("Admin");
	}
}
