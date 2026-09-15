using FarmControlAPI.Application.ProcesosCultivo.Commands;
using FarmControlAPI.Application.ProcesosCultivo.Queries;
using FarmControlAPI.Domain.Enums;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal record CerrarProcesoRequest(EstadoProceso Estado, DateTime FechaCierre);

internal static class ProcesosCultivoEndpoints
{
	internal static void MapProcesosCultivoEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/procesos-cultivo").RequireAuthorization();

		group.MapGet("/", async (int? parcelaId, EstadoProceso? estado, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetProcesosCultivoQuery(parcelaId, estado));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreateProcesoCultivoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/procesos-cultivo/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		group.MapPut("/{id:int}", async (int id, UpdateProcesoCultivoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapPost("/{id:int}/cerrar", async (int id, CerrarProcesoRequest req, IMediator mediator) =>
		{
			var result = await mediator.Send(new CerrarProcesoCultivoCommand(id, req.Estado, req.FechaCierre));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteProcesoCultivoCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");
	}
}
