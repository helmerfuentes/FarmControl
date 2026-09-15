using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Insumos.Commands;

public record TransferirInsumoDto(int MovimientoSalidaId, int MovimientoEntradaId);

public record TransferirInsumoCommand(
	int InsumoId,
	int ParcelaOrigenId,
	int ParcelaDestinoId,
	decimal Cantidad,
	DateTime Fecha,
	string? Observacion) : IRequest<Result<TransferirInsumoDto>>;

public class TransferirInsumoCommandHandler : IRequestHandler<TransferirInsumoCommand, Result<TransferirInsumoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public TransferirInsumoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<TransferirInsumoDto>> Handle(TransferirInsumoCommand request, CancellationToken cancellationToken)
	{
		Result<TransferirInsumoDto> result;

		if (request.ParcelaOrigenId == request.ParcelaDestinoId)
		{
			result = Result<TransferirInsumoDto>.Failure("La parcela de origen y destino no pueden ser la misma.");
			return result;
		}

		if (request.Cantidad <= 0)
		{
			result = Result<TransferirInsumoDto>.Failure("La cantidad a transferir debe ser mayor a cero.");
			return result;
		}

		var insumo = await _context.Insumos.FirstOrDefaultAsync(i => i.Id == request.InsumoId, cancellationToken);
		if (insumo is null)
		{
			result = Result<TransferirInsumoDto>.Failure($"Insumo con Id {request.InsumoId} no encontrado.");
			return result;
		}

		var origen = await _context.Parcelas.FirstOrDefaultAsync(p => p.Id == request.ParcelaOrigenId, cancellationToken);
		if (origen is null)
		{
			result = Result<TransferirInsumoDto>.Failure($"Parcela de origen con Id {request.ParcelaOrigenId} no encontrada.");
			return result;
		}

		var destino = await _context.Parcelas.FirstOrDefaultAsync(p => p.Id == request.ParcelaDestinoId, cancellationToken);
		if (destino is null)
		{
			result = Result<TransferirInsumoDto>.Failure($"Parcela de destino con Id {request.ParcelaDestinoId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(origen.FincaId) || !_currentUser.PuedeEscribirEnFinca(destino.FincaId))
		{
			result = Result<TransferirInsumoDto>.Failure("No tienes permiso de escritura en una de las dos fincas.");
			return result;
		}

		var observacionTransferencia = string.IsNullOrWhiteSpace(request.Observacion)
			? $"Transferencia entre '{origen.Nombre}' y '{destino.Nombre}'."
			: request.Observacion;

		var salida = new MovimientoInsumo
		{
			InsumoId = request.InsumoId,
			ParcelaId = request.ParcelaOrigenId,
			Cantidad = request.Cantidad,
			TipoMovimiento = TipoMovimientoInsumo.Salida,
			Fecha = request.Fecha,
			Observacion = $"Transferencia a '{destino.Nombre}'. {observacionTransferencia}"
		};

		var entrada = new MovimientoInsumo
		{
			InsumoId = request.InsumoId,
			ParcelaId = request.ParcelaDestinoId,
			Cantidad = request.Cantidad,
			TipoMovimiento = TipoMovimientoInsumo.Entrada,
			Fecha = request.Fecha,
			Observacion = $"Transferencia desde '{origen.Nombre}'. {observacionTransferencia}"
		};

		_context.MovimientosInsumo.Add(salida);
		_context.MovimientosInsumo.Add(entrada);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Insumo transferido",
			$"Se transfirieron {request.Cantidad} de '{insumo.Nombre}' de '{origen.Nombre}' a '{destino.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<TransferirInsumoDto>.Success(new TransferirInsumoDto(salida.Id, entrada.Id));
		return result;
	}
}
