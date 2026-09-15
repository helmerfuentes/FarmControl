using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Compras.Commands;

public record CompraDto(int Id, int FincaId, string FincaNombre, int SocioId, string SocioNombre, string Descripcion, decimal Valor, DateTime Fecha, TipoCompra TipoCompra, string? AdjuntoUrl, int? ParcelaId = null, string? ParcelaNombre = null, int? ProcesoCultivoId = null, string? Proveedor = null, decimal TotalPagado = 0, decimal SaldoPendiente = 0);

public record CreateCompraCommand(
	int FincaId,
	int SocioId,
	int? ParcelaId,
	string Descripcion,
	decimal Valor,
	DateTime Fecha,
	TipoCompra TipoCompra,
	string? AdjuntoUrl,
	string? Proveedor = null) : IRequest<Result<CompraDto>>;

public class CreateCompraCommandHandler : IRequestHandler<CreateCompraCommand, Result<CompraDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateCompraCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<CompraDto>> Handle(CreateCompraCommand request, CancellationToken cancellationToken)
	{
		Result<CompraDto> result;

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

		if (!_currentUser.PuedeEscribirEnFinca(finca.Id))
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

		var compra = new Compra
		{
			FincaId = request.FincaId,
			SocioId = request.SocioId,
			ParcelaId = request.ParcelaId,
			ProcesoCultivoId = procesoId,
			Descripcion = request.Descripcion,
			Valor = request.Valor,
			Fecha = request.Fecha,
			TipoCompra = request.TipoCompra,
			AdjuntoUrl = request.AdjuntoUrl,
			Proveedor = request.Proveedor
		};

		_context.Compras.Add(compra);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Compra creada",
			$"Se registró la compra '{compra.Descripcion}' por {compra.Valor:C0}.",
			cancellationToken: cancellationToken);

		result = Result<CompraDto>.Success(new CompraDto(compra.Id, compra.FincaId, finca.Nombre, compra.SocioId, socio.Nombre, compra.Descripcion, compra.Valor, compra.Fecha, compra.TipoCompra, compra.AdjuntoUrl, compra.ParcelaId, parcelaNombre, compra.ProcesoCultivoId, compra.Proveedor));
		return result;
	}
}
