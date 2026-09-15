using FarmControlAPI.Application.Comentarios.Commands;
using FarmControlAPI.Application.Comentarios.Queries;
using FarmControlAPI.Domain.Enums;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal record CreateComentarioRequest(TipoEntidadComentario TipoEntidad, int EntidadId, string Texto);

internal static class ComentariosEndpoints
{
	internal static void MapComentariosEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/comentarios").RequireAuthorization();

		group.MapGet("/", async (TipoEntidadComentario tipoEntidad, int entidadId, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetComentariosQuery(tipoEntidad, entidadId));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/", async (CreateComentarioRequest body, IMediator mediator) =>
		{
			var result = await mediator.Send(new CreateComentarioCommand(body.TipoEntidad, body.EntidadId, body.Texto));
			return EndpointHelpers.ToCreated(result, $"/comentarios/{result.Value?.Id}");
		});

		group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteComentarioCommand(id));
			return EndpointHelpers.ToResponse(result);
		});
	}
}
