using FarmControlAPI.Application.Asistencias.Commands;
using FarmControlAPI.Application.Asistencias.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class AsistenciasEndpoints
{
	internal static void MapAsistenciasEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/asistencias").RequireAuthorization();

		group.MapGet("/", async (int? personaId, int? fincaId, DateTime? desde, DateTime? hasta, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetAsistenciasQuery(personaId, fincaId, desde, hasta));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreateAsistenciaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/asistencias/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		group.MapPut("/{id:int}", async (int id, UpdateAsistenciaRequest request, IMediator mediator) =>
		{
			var result = await mediator.Send(new UpdateAsistenciaCommand(id, request.HoraEntrada, request.HoraSalida, request.Observacion));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteAsistenciaCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");
	}
}

internal record UpdateAsistenciaRequest(TimeSpan? HoraEntrada, TimeSpan? HoraSalida, string? Observacion);
