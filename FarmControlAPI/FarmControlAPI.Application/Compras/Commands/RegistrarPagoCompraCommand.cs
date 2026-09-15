using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Compras.Commands;

public record PagoCompraDto(int Id, int CompraId, decimal Monto, DateTime Fecha, string? Observacion);

public record RegistrarPagoCompraCommand(int CompraId, decimal Monto, DateTime Fecha, string? Observacion) : IRequest<Result<PagoCompraDto>>;

public class RegistrarPagoCompraCommandHandler : IRequestHandler<RegistrarPagoCompraCommand, Result<PagoCompraDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public RegistrarPagoCompraCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<PagoCompraDto>> Handle(RegistrarPagoCompraCommand request, CancellationToken cancellationToken)
	{
		Result<PagoCompraDto> result;

		if (request.Monto <= 0)
		{
			result = Result<PagoCompraDto>.Failure("El monto del pago debe ser mayor a cero.");
			return result;
		}

		var compra = await _context.Compras
			.Include(c => c.Pagos)
			.FirstOrDefaultAsync(c => c.Id == request.CompraId, cancellationToken);

		if (compra is null)
		{
			result = Result<PagoCompraDto>.Failure($"Compra con Id {request.CompraId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(compra.FincaId))
		{
			result = Result<PagoCompraDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var saldoActual = compra.Valor - compra.Pagos.Sum(p => p.Monto);
		if (request.Monto > saldoActual)
		{
			result = Result<PagoCompraDto>.Failure($"El pago ({request.Monto:C0}) supera el saldo pendiente ({saldoActual:C0}).");
			return result;
		}

		var pago = new PagoCompra
		{
			CompraId = request.CompraId,
			Monto = request.Monto,
			Fecha = request.Fecha,
			Observacion = request.Observacion
		};

		_context.PagosCompra.Add(pago);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Pago de compra registrado",
			$"Se registró un pago de {pago.Monto:C0} para la compra '{compra.Descripcion}'.",
			cancellationToken: cancellationToken);

		result = Result<PagoCompraDto>.Success(new PagoCompraDto(pago.Id, pago.CompraId, pago.Monto, pago.Fecha, pago.Observacion));
		return result;
	}
}
