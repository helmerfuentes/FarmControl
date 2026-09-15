using FarmControlAPI.Application.Bitacora.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class BitacoraEndpoints
{
	internal static void MapBitacoraEndpoints(this WebApplication app)
	{
		var bitacora = app.MapGroup("/bitacora").RequireAuthorization("Admin");

		bitacora.MapGet("/", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetBitacoraQuery());
			return EndpointHelpers.ToResponse(result);
		});
	}
}
