using FarmControlAPI.Application.Fincas.Commands;
using FarmControlAPI.Application.Fincas.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class FincasEndpoints
{
	internal static void MapFincasEndpoints(this WebApplication app)
	{
		var fincas = app.MapGroup("/fincas").RequireAuthorization();

		fincas.MapGet("/", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetFincasQuery());
			return EndpointHelpers.ToResponse(result);
		});

		fincas.MapPost("/", async (CreateFincaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/fincas/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		fincas.MapPut("/{id:int}", async (int id, UpdateFincaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		fincas.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteFincaCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("SuperAdmin");

		fincas.MapGet("/{fincaId:int}/parcelas", async (int fincaId, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetParcelasQuery(fincaId));
			return EndpointHelpers.ToResponse(result);
		});

		fincas.MapPost("/{fincaId:int}/parcelas", async (int fincaId, CreateParcelaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { FincaId = fincaId });
			return EndpointHelpers.ToCreated(result, $"/fincas/{fincaId}/parcelas/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		var parcelas = app.MapGroup("/parcelas").RequireAuthorization();

		parcelas.MapPut("/{id:int}", async (int id, UpdateParcelaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		parcelas.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteParcelaCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");
	}
}
