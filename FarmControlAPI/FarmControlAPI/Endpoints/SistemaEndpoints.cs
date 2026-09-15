using FarmControlAPI.Application.Sistema.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class SistemaEndpoints
{
	internal static void MapSistemaEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/sistema").RequireAuthorization("SuperAdmin");

		group.MapGet("/salud", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetSaludSistemaQuery());
			return EndpointHelpers.ToResponse(result);
		});
	}
}
