using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Insumos.Queries;
using FarmControlAPI.Application.ProcesosCultivo.Queries;
using FarmControlAPI.Application.Tareas.Queries;
using FarmControlAPI.Domain.Enums;
using MediatR;

namespace FarmControlAPI.Application.Notificaciones.Queries;

public record NotificacionDto(string Tipo, string Titulo, string Mensaje, string RutaDestino);

public record GetNotificacionesQuery : IRequest<Result<List<NotificacionDto>>>;

public class GetNotificacionesQueryHandler : IRequestHandler<GetNotificacionesQuery, Result<List<NotificacionDto>>>
{
	private const int DIAS_ALERTA_COSECHA = 15;
	private const int DIAS_ALERTA_VENCIMIENTO_INSUMO = 30;

	private readonly IMediator _mediator;

	public GetNotificacionesQueryHandler(IMediator mediator)
	{
		_mediator = mediator;
	}

	public async Task<Result<List<NotificacionDto>>> Handle(GetNotificacionesQuery request, CancellationToken cancellationToken)
	{
		var notificaciones = new List<NotificacionDto>();

		await AgregarCosechasProximasAsync(notificaciones, cancellationToken);
		await AgregarSaldosBajosAsync(notificaciones, cancellationToken);
		await AgregarInsumosPorVencerAsync(notificaciones, cancellationToken);
		await AgregarTareasPendientesAsync(notificaciones, cancellationToken);

		var result = Result<List<NotificacionDto>>.Success(notificaciones);
		return result;
	}

	private async Task AgregarCosechasProximasAsync(List<NotificacionDto> notificaciones, CancellationToken cancellationToken)
	{
		var procesosResult = await _mediator.Send(new GetProcesosCultivoQuery(null, EstadoProceso.Activo), cancellationToken);
		if (!procesosResult.IsSuccess || procesosResult.Value is null)
		{
			return;
		}

		var limite = DateTime.UtcNow.AddDays(DIAS_ALERTA_COSECHA);

		foreach (var proceso in procesosResult.Value)
		{
			if (proceso.FechaEstimadaCosecha.HasValue && proceso.FechaEstimadaCosecha.Value <= limite)
			{
				notificaciones.Add(new NotificacionDto(
					"cosecha",
					"Cosecha próxima",
					$"{proceso.ParcelaNombre} — {proceso.ProductoNombre} ({proceso.FechaEstimadaCosecha.Value:dd/MM/yyyy})",
					"/app/procesos-cultivo"));
			}
		}
	}

	private async Task AgregarSaldosBajosAsync(List<NotificacionDto> notificaciones, CancellationToken cancellationToken)
	{
		var saldosResult = await _mediator.Send(new GetSaldosInsumoQuery(), cancellationToken);
		if (!saldosResult.IsSuccess || saldosResult.Value is null)
		{
			return;
		}

		foreach (var saldo in saldosResult.Value)
		{
			if (saldo.SaldoBajo)
			{
				notificaciones.Add(new NotificacionDto(
					"saldo-bajo",
					"Saldo de insumo bajo",
					$"{saldo.InsumoNombre}: {saldo.Saldo} {saldo.UnidadMedida} (mínimo {saldo.StockMinimo})",
					"/app/inventario"));
			}
		}
	}

	private async Task AgregarInsumosPorVencerAsync(List<NotificacionDto> notificaciones, CancellationToken cancellationToken)
	{
		var insumosResult = await _mediator.Send(new GetInsumosQuery(null), cancellationToken);
		if (!insumosResult.IsSuccess || insumosResult.Value is null)
		{
			return;
		}

		var limite = DateTime.UtcNow.AddDays(DIAS_ALERTA_VENCIMIENTO_INSUMO);

		foreach (var insumo in insumosResult.Value)
		{
			if (insumo.FechaVencimiento.HasValue && insumo.FechaVencimiento.Value <= limite)
			{
				var vencido = insumo.FechaVencimiento.Value < DateTime.UtcNow;
				notificaciones.Add(new NotificacionDto(
					vencido ? "insumo-vencido" : "insumo-por-vencer",
					vencido ? "Insumo vencido" : "Insumo próximo a vencer",
					$"{insumo.Nombre} vence el {insumo.FechaVencimiento.Value:dd/MM/yyyy}",
					"/app/inventario"));
			}
		}
	}

	private async Task AgregarTareasPendientesAsync(List<NotificacionDto> notificaciones, CancellationToken cancellationToken)
	{
		var tareasResult = await _mediator.Send(new GetTareasRecurrentesQuery(null), cancellationToken);
		if (!tareasResult.IsSuccess || tareasResult.Value is null)
		{
			return;
		}

		var hoy = DateTime.UtcNow.Date;

		foreach (var tarea in tareasResult.Value)
		{
			if (tarea.Activa && tarea.ProximaFecha.Date <= hoy)
			{
				notificaciones.Add(new NotificacionDto(
					"tarea-pendiente",
					"Tarea pendiente",
					$"{tarea.Descripcion} — {tarea.ParcelaNombre} (desde {tarea.ProximaFecha:dd/MM/yyyy})",
					"/app/fincas"));
			}
		}
	}
}
