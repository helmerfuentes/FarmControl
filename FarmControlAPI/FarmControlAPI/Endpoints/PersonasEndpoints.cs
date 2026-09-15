using FarmControlAPI.Application.Personas.Commands;
using FarmControlAPI.Application.Personas.Queries;
using FarmControlAPI.Domain.Enums;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class PersonasEndpoints
{
	internal static void MapPersonasEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/personas").RequireAuthorization();

		group.MapGet("/", async (TipoPersona? tipoPersona, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetPersonasQuery(tipoPersona));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreatePersonaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/personas/{result.Value?.Id}");
		}).RequireAuthorization("Admin");

		group.MapPut("/{id:int}", async (int id, UpdatePersonaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { Id = id });
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeletePersonaCommand(id));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization("Admin");
	}
}
