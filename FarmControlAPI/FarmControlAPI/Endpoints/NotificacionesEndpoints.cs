using FarmControlAPI.Application.Notificaciones.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class NotificacionesEndpoints
{
	internal static void MapNotificacionesEndpoints(this WebApplication app)
	{
		var notificaciones = app.MapGroup("/notificaciones").RequireAuthorization("Admin");

		notificaciones.MapGet("/", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetNotificacionesQuery());
			return EndpointHelpers.ToResponse(result);
		});
	}
}
