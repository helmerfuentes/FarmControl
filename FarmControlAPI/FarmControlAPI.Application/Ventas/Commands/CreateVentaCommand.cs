using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Ventas.Commands;

public record DetalleVentaRequest(string Clasificacion, decimal Cantidad, UnidadMedidaVenta UnidadMedida, decimal PrecioUnitario);

public record DetalleVentaDto(int Id, string Clasificacion, decimal Cantidad, UnidadMedidaVenta UnidadMedida, decimal PrecioUnitario, decimal Subtotal);

public record VentaDto(int Id, int ParcelaId, int CompradorId, string CompradorNombre, DateTime Fecha, decimal ValorTransporte, decimal Total, List<DetalleVentaDto> Detalles, string ParcelaNombre = "", string FincaNombre = "", int? ProcesoCultivoId = null, decimal TotalPagado = 0, decimal SaldoPendiente = 0);

public record CreateVentaCommand(
	int ParcelaId,
	int CompradorId,
	DateTime Fecha,
	decimal ValorTransporte,
	List<DetalleVentaRequest> Detalles) : IRequest<Result<VentaDto>>;

public class CreateVentaCommandHandler : IRequestHandler<CreateVentaCommand, Result<VentaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateVentaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<VentaDto>> Handle(CreateVentaCommand request, CancellationToken cancellationToken)
	{
		Result<VentaDto> result;

		var comprador = await _context.Personas.FirstOrDefaultAsync(p => p.Id == request.CompradorId, cancellationToken);
		if (comprador is null)
		{
			result = Result<VentaDto>.Failure($"Persona (comprador) con Id {request.CompradorId} no encontrada.");
			return result;
		}

		var fincaId = await _context.Parcelas
			.Where(p => p.Id == request.ParcelaId)
			.Select(p => (int?)p.FincaId)
			.FirstOrDefaultAsync(cancellationToken);

		if (fincaId is null)
		{
			result = Result<VentaDto>.Failure($"Parcela con Id {request.ParcelaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(fincaId.Value))
		{
			result = Result<VentaDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var detalles = request.Detalles.Select(d => new DetalleVenta
		{
			Clasificacion = d.Clasificacion,
			Cantidad = d.Cantidad,
			UnidadMedida = d.UnidadMedida,
			PrecioUnitario = d.PrecioUnitario,
			Subtotal = d.Cantidad * d.PrecioUnitario
		}).ToList();

		var total = detalles.Sum(d => d.Subtotal) - request.ValorTransporte;

		var procesoCultivo = await _context.ProcesosCultivo
			.Where(pc => pc.ParcelaId == request.ParcelaId && pc.Estado == EstadoProceso.Activo)
			.FirstOrDefaultAsync(cancellationToken);

		var venta = new Venta
		{
			ParcelaId = request.ParcelaId,
			CompradorId = request.CompradorId,
			Fecha = request.Fecha,
			ValorTransporte = request.ValorTransporte,
			Total = total,
			Detalles = detalles,
			ProcesoCultivoId = procesoCultivo?.Id
		};

		_context.Ventas.Add(venta);
		await _context.SaveChangesAsync(cancellationToken);

		if (procesoCultivo is not null)
		{
			foreach (var detalle in detalles)
			{
				_context.HistorialPreciosVenta.Add(new Domain.Entities.HistorialPrecioVenta
				{
					ProductoId = procesoCultivo.ProductoId,
					Clasificacion = detalle.Clasificacion,
					PrecioUnitario = detalle.PrecioUnitario,
					UnidadMedida = detalle.UnidadMedida,
					Fecha = request.Fecha,
					VentaId = venta.Id
				});
			}
			await _context.SaveChangesAsync(cancellationToken);
		}

		await _bitacora.RegistrarAsync(
			"Venta registrada",
			$"Se registró una venta del {venta.Fecha:dd/MM/yyyy} por {venta.Total:C0}.",
			cancellationToken: cancellationToken);

		var dto = new VentaDto(
			venta.Id, venta.ParcelaId, venta.CompradorId, comprador.Nombre, venta.Fecha, venta.ValorTransporte, venta.Total,
			detalles.Select(d => new DetalleVentaDto(d.Id, d.Clasificacion, d.Cantidad, d.UnidadMedida, d.PrecioUnitario, d.Subtotal)).ToList(),
			ProcesoCultivoId: venta.ProcesoCultivoId);

		result = Result<VentaDto>.Success(dto);
		return result;
	}
}
