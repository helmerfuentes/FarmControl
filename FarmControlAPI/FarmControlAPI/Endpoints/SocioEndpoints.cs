using FarmControlAPI.Application.ProcesosCultivo.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class SocioEndpoints
{
	internal static void MapSocioEndpoints(this WebApplication app)
	{
		app.MapGet("/socio/mis-procesos", async (HttpContext context, IMediator mediator) =>
		{
			var personaIdClaim = context.User.FindFirst("personaId")?.Value;
			if (!int.TryParse(personaIdClaim, out var personaId))
			{
				return Results.Unauthorized();
			}
			var result = await mediator.Send(new GetMisProcesosQuery(personaId));
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization();
	}
}
