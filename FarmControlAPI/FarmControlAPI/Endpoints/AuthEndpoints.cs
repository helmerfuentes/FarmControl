using FarmControlAPI.Application.Auth;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class AuthEndpoints
{
	internal static void MapAuthEndpoints(this WebApplication app)
	{
		app.MapPost("/auth/login", async (LoginCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToResponse(result);
		});

		app.MapPut("/auth/contrasena", async (CambiarContrasenaCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization();

		app.MapGet("/auth/preferencias-dashboard", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetPreferenciasDashboardQuery());
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization();

		app.MapPut("/auth/preferencias-dashboard", async (GuardarPreferenciasDashboardCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToResponse(result);
		}).RequireAuthorization();
	}
}
