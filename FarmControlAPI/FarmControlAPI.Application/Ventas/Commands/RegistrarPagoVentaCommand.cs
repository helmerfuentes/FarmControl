using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Ventas.Commands;

public record PagoVentaDto(int Id, int VentaId, decimal Monto, DateTime Fecha, string? Observacion);

public record RegistrarPagoVentaCommand(int VentaId, decimal Monto, DateTime Fecha, string? Observacion) : IRequest<Result<PagoVentaDto>>;

public class RegistrarPagoVentaCommandHandler : IRequestHandler<RegistrarPagoVentaCommand, Result<PagoVentaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public RegistrarPagoVentaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<PagoVentaDto>> Handle(RegistrarPagoVentaCommand request, CancellationToken cancellationToken)
	{
		Result<PagoVentaDto> result;

		if (request.Monto <= 0)
		{
			result = Result<PagoVentaDto>.Failure("El monto del pago debe ser mayor a cero.");
			return result;
		}

		var venta = await _context.Ventas
			.Include(v => v.Parcela)
			.Include(v => v.Pagos)
			.FirstOrDefaultAsync(v => v.Id == request.VentaId, cancellationToken);

		if (venta is null)
		{
			result = Result<PagoVentaDto>.Failure($"Venta con Id {request.VentaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(venta.Parcela.FincaId))
		{
			result = Result<PagoVentaDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var saldoActual = venta.Total - venta.Pagos.Sum(p => p.Monto);
		if (request.Monto > saldoActual)
		{
			result = Result<PagoVentaDto>.Failure($"El pago ({request.Monto:C0}) supera el saldo pendiente ({saldoActual:C0}).");
			return result;
		}

		var pago = new PagoVenta
		{
			VentaId = request.VentaId,
			Monto = request.Monto,
			Fecha = request.Fecha,
			Observacion = request.Observacion
		};

		_context.PagosVenta.Add(pago);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Pago de venta registrado",
			$"Se registró un pago de {pago.Monto:C0} para la venta del {venta.Fecha:dd/MM/yyyy}.",
			cancellationToken: cancellationToken);

		result = Result<PagoVentaDto>.Success(new PagoVentaDto(pago.Id, pago.VentaId, pago.Monto, pago.Fecha, pago.Observacion));
		return result;
	}
}
