using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Insumos.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Insumos.Commands;

public record CreateMovimientoInsumoCommand(
	int InsumoId,
	int ParcelaId,
	decimal Cantidad,
	TipoMovimientoInsumo TipoMovimiento,
	DateTime Fecha,
	string? Observacion,
	decimal? PrecioUnitario = null) : IRequest<Result<MovimientoInsumoDto>>;

public class CreateMovimientoInsumoCommandHandler : IRequestHandler<CreateMovimientoInsumoCommand, Result<MovimientoInsumoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateMovimientoInsumoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<MovimientoInsumoDto>> Handle(CreateMovimientoInsumoCommand request, CancellationToken cancellationToken)
	{
		Result<MovimientoInsumoDto> result;

		var insumo = await _context.Insumos.FirstOrDefaultAsync(i => i.Id == request.InsumoId, cancellationToken);
		if (insumo is null)
		{
			result = Result<MovimientoInsumoDto>.Failure($"Insumo con Id {request.InsumoId} no encontrado.");
			return result;
		}

		var parcela = await _context.Parcelas.FirstOrDefaultAsync(p => p.Id == request.ParcelaId, cancellationToken);
		if (parcela is null)
		{
			result = Result<MovimientoInsumoDto>.Failure($"Parcela con Id {request.ParcelaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(parcela.FincaId))
		{
			result = Result<MovimientoInsumoDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var movimiento = new MovimientoInsumo
		{
			InsumoId = request.InsumoId,
			ParcelaId = request.ParcelaId,
			Cantidad = request.Cantidad,
			TipoMovimiento = request.TipoMovimiento,
			Fecha = request.Fecha,
			Observacion = request.Observacion,
			PrecioUnitario = request.TipoMovimiento == TipoMovimientoInsumo.Entrada ? request.PrecioUnitario : null
		};

		_context.MovimientosInsumo.Add(movimiento);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			$"Movimiento de {movimiento.TipoMovimiento} registrado",
			$"{movimiento.TipoMovimiento} de {movimiento.Cantidad} de '{insumo.Nombre}' en la parcela '{parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<MovimientoInsumoDto>.Success(new MovimientoInsumoDto(
			movimiento.Id, movimiento.InsumoId, insumo.Nombre,
			movimiento.ParcelaId, parcela.Nombre,
			movimiento.Cantidad, movimiento.TipoMovimiento, movimiento.Fecha, movimiento.Observacion, movimiento.PrecioUnitario));
		return result;
	}
}
