using FarmControlAPI.Application.Clientes.Commands;
using FarmControlAPI.Application.Clientes.Queries;
using FarmControlAPI.Application.Common;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal record SetPersonaActivaRequest(bool Activa);
internal record SetPermisoFincaRequest(bool SoloLectura, bool PuedeEliminar);
internal record AsignarCredencialesRequest(string NombreUsuario, string Contrasena, List<AccesoFincaInput>? Accesos = null);
internal record AsignarPlanClienteRequest(int? PlanId);

internal static class ClientesEndpoints
{
	internal static void MapClientesEndpoints(this WebApplication app)
	{
		var clientes = app.MapGroup("/clientes").RequireAuthorization("SuperAdmin");

		clientes.MapGet("/", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetClientesQuery());
			return EndpointHelpers.ToResponse(result);
		});

		clientes.MapGet("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetClienteDetalleQuery(id));
			return EndpointHelpers.ToResponse(result);
		});

		clientes.MapPost("/", async (CreateClienteCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/clientes/{result.Value?.Id}");
		});

		clientes.MapPut("/{clienteId:int}/contrato", async (int clienteId, UpdateContratoClienteCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { ClienteId = clienteId });
			return EndpointHelpers.ToResponse(result);
		});

		clientes.MapPut("/{clienteId:int}/plan", async (int clienteId, AsignarPlanClienteRequest body, IMediator mediator) =>
		{
			var result = await mediator.Send(new AsignarPlanClienteCommand(clienteId, body.PlanId));
			return EndpointHelpers.ToResponse(result);
		});

		clientes.MapPost("/{clienteId:int}/fincas", async (int clienteId, CreateFincaForClienteCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { ClienteId = clienteId });
			return EndpointHelpers.ToCreated(result, $"/clientes/{clienteId}/fincas/{result.Value?.Id}");
		});

		clientes.MapPost("/{clienteId:int}/usuarios", async (int clienteId, CreatePersonaConAccesoCommand command, IMediator mediator) =>
		{
			var result = await mediator.Send(command with { ClienteId = clienteId });
			return EndpointHelpers.ToCreated(result, $"/personas/{result.Value?.Id}");
		});

		clientes.MapGet("/{clienteId:int}/usuarios", async (int clienteId, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetPersonasDelClienteQuery(clienteId));
			return EndpointHelpers.ToResponse(result);
		});

		clientes.MapGet("/{id:int}/exportar", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new ExportarClienteQuery(id));
			return EndpointHelpers.ToResponse(result);
		});

		var personasFinca = app.MapGroup("/personas-finca").RequireAuthorization("SuperAdmin");

		personasFinca.MapPut("/{personaId:int}/{fincaId:int}", async (int personaId, int fincaId, SetPermisoFincaRequest body, IMediator mediator) =>
		{
			var result = await mediator.Send(new SetPermisoFincaCommand(personaId, fincaId, body.SoloLectura, body.PuedeEliminar));
			return EndpointHelpers.ToResponse(result);
		});

		var personasEstado = app.MapGroup("/personas").RequireAuthorization("SuperAdmin");

		personasEstado.MapPut("/{personaId:int}/activo", async (int personaId, SetPersonaActivaRequest body, IMediator mediator) =>
		{
			var result = await mediator.Send(new SetPersonaActivaCommand(personaId, body.Activa));
			return EndpointHelpers.ToResponse(result);
		});

		// Autogestión: el Admin de un cliente administra sus propios usuarios, sin depender del SuperAdmin.
		var miCliente = app.MapGroup("/mi-cliente").RequireAuthorization("Admin");

		miCliente.MapGet("/usuarios", async (ICurrentUserContext currentUser, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetPersonasDelClienteQuery(currentUser.ClienteId ?? 0));
			return EndpointHelpers.ToResponse(result);
		});

		miCliente.MapPost("/usuarios", async (UsuarioMiClienteRequest body, ICurrentUserContext currentUser, IMediator mediator) =>
		{
			var command = new CreatePersonaConAccesoCommand(
				currentUser.ClienteId ?? 0,
				body.Nombre, body.Documento, body.Telefono, body.Email,
				body.TipoPersona, body.NombreUsuario, body.Contrasena, body.Accesos);
			var result = await mediator.Send(command);
			return EndpointHelpers.ToCreated(result, $"/personas/{result.Value?.Id}");
		});

		miCliente.MapPut("/usuarios/{personaId:int}/fincas/{fincaId:int}", async (int personaId, int fincaId, SetPermisoFincaRequest body, IMediator mediator) =>
		{
			var result = await mediator.Send(new SetPermisoFincaCommand(personaId, fincaId, body.SoloLectura, body.PuedeEliminar));
			return EndpointHelpers.ToResponse(result);
		});

		miCliente.MapPut("/usuarios/{personaId:int}/activo", async (int personaId, SetPersonaActivaRequest body, IMediator mediator) =>
		{
			var result = await mediator.Send(new SetPersonaActivaCommand(personaId, body.Activa));
			return EndpointHelpers.ToResponse(result);
		});

		miCliente.MapPut("/personas/{personaId:int}/credenciales", async (int personaId, AsignarCredencialesRequest body, IMediator mediator) =>
		{
			var result = await mediator.Send(new AsignarCredencialesCommand(personaId, body.NombreUsuario, body.Contrasena, body.Accesos));
			return EndpointHelpers.ToResponse(result);
		});
	}
}
