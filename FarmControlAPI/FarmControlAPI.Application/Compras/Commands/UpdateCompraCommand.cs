using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Compras.Commands;

public record UpdateCompraCommand(
	int Id,
	int FincaId,
	int SocioId,
	int? ParcelaId,
	string Descripcion,
	decimal Valor,
	DateTime Fecha,
	TipoCompra TipoCompra,
	string? AdjuntoUrl,
	string? Proveedor = null) : IRequest<Result<CompraDto>>;

public class UpdateCompraCommandHandler : IRequestHandler<UpdateCompraCommand, Result<CompraDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public UpdateCompraCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<CompraDto>> Handle(UpdateCompraCommand request, CancellationToken cancellationToken)
	{
		Result<CompraDto> result;

		var compra = await _context.Compras
			.Include(c => c.Pagos)
			.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

		if (compra is null)
		{
			result = Result<CompraDto>.Failure($"Compra con Id {request.Id} no encontrada.");
			return result;
		}

		var socio = await _context.Personas.FirstOrDefaultAsync(p => p.Id == request.SocioId, cancellationToken);
		if (socio is null)
		{
			result = Result<CompraDto>.Failure($"Persona con Id {request.SocioId} no encontrada.");
			return result;
		}

		var finca = await _context.Fincas.FirstOrDefaultAsync(f => f.Id == request.FincaId, cancellationToken);
		if (finca is null)
		{
			result = Result<CompraDto>.Failure($"Finca con Id {request.FincaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(compra.FincaId) || !_currentUser.PuedeEscribirEnFinca(finca.Id))
		{
			result = Result<CompraDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		string? parcelaNombre = null;
		int? procesoId = null;

		if (request.ParcelaId.HasValue)
		{
			var parcela = await _context.Parcelas.FirstOrDefaultAsync(p => p.Id == request.ParcelaId.Value, cancellationToken);
			parcelaNombre = parcela?.Nombre;

			procesoId = await _context.ProcesosCultivo
				.Where(pc => pc.ParcelaId == request.ParcelaId.Value && pc.Estado == EstadoProceso.Activo)
				.Select(pc => (int?)pc.Id)
				.FirstOrDefaultAsync(cancellationToken);
		}

		compra.FincaId = request.FincaId;
		compra.SocioId = request.SocioId;
		compra.ParcelaId = request.ParcelaId;
		compra.ProcesoCultivoId = procesoId;
		compra.Descripcion = request.Descripcion;
		compra.Valor = request.Valor;
		compra.Fecha = request.Fecha;
		compra.TipoCompra = request.TipoCompra;
		compra.AdjuntoUrl = request.AdjuntoUrl;
		compra.Proveedor = request.Proveedor;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Compra actualizada",
			$"Se actualizó la compra '{compra.Descripcion}' por {compra.Valor:C0}.",
			cancellationToken: cancellationToken);

		var totalPagado = compra.Pagos.Sum(p => p.Monto);
		result = Result<CompraDto>.Success(new CompraDto(compra.Id, compra.FincaId, finca.Nombre, compra.SocioId, socio.Nombre, compra.Descripcion, compra.Valor, compra.Fecha, compra.TipoCompra, compra.AdjuntoUrl, compra.ParcelaId, parcelaNombre, compra.ProcesoCultivoId, compra.Proveedor, totalPagado, compra.Valor - totalPagado));
		return result;
	}
}
