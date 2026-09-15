using FarmControlAPI.Application.Reportes.Commands;
using FarmControlAPI.Application.Reportes.Queries;
using MediatR;

namespace FarmControlAPI.Endpoints;

internal static class ReportesEndpoints
{
	internal static void MapReportesEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/reportes").RequireAuthorization();

		group.MapGet("/finca/{id:int}/resumen", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetResumenFincaQuery(id));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapGet("/finca/{id:int}/historico", async (int id, int? meses, IMediator mediator) =>
		{
			var result = await mediator.Send(meses.HasValue ? new GetHistoricoFincaQuery(id, meses.Value) : new GetHistoricoFincaQuery(id));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapGet("/proceso-cultivo/{id:int}/resumen", async (int id, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetResumenProcesoCultivoQuery(id));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapGet("/consolidado", async (IMediator mediator) =>
		{
			var result = await mediator.Send(new GetResumenConsolidadoQuery());
			return EndpointHelpers.ToResponse(result);
		});

		group.MapGet("/precios", async (int productoId, string? clasificacion, int? anio, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetHistorialPreciosQuery(productoId, clasificacion, anio));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapGet("/estacionalidad", async (int productoId, string? clasificacion, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetEstacionalidadPreciosQuery(productoId, clasificacion));
			return EndpointHelpers.ToResponse(result);
		});

		group.MapPost("/finca/{id:int}/compartir", async (int id, CrearReporteCompartidoRequest? body, IMediator mediator) =>
		{
			var result = await mediator.Send(new CrearReporteCompartidoCommand(id, body?.DiasValidez));
			return EndpointHelpers.ToResponse(result);
		});

		// Público: sin autenticación, protegido por el token impredecible del enlace.
		app.MapGet("/reportes/compartido/{token}", async (string token, IMediator mediator) =>
		{
			var result = await mediator.Send(new GetReporteCompartidoQuery(token));
			return EndpointHelpers.ToResponse(result);
		}).AllowAnonymous();
	}
}

internal record CrearReporteCompartidoRequest(int? DiasValidez);
