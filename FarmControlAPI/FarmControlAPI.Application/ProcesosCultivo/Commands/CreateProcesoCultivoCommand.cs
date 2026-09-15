using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.ProcesosCultivo.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.ProcesosCultivo.Commands;

public record CreateProcesoCultivoCommand(
	int ParcelaId,
	int ProductoId,
	int? SocioId,
	decimal CostoInicial,
	DateTime FechaInicio,
	DateTime? FechaEstimadaCosecha) : IRequest<Result<ProcesoCultivoDto>>;

public class CreateProcesoCultivoCommandHandler : IRequestHandler<CreateProcesoCultivoCommand, Result<ProcesoCultivoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateProcesoCultivoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ProcesoCultivoDto>> Handle(CreateProcesoCultivoCommand request, CancellationToken cancellationToken)
	{
		Result<ProcesoCultivoDto> result;

		var parcela = await _context.Parcelas.FirstOrDefaultAsync(p => p.Id == request.ParcelaId, cancellationToken);
		if (parcela is null)
		{
			result = Result<ProcesoCultivoDto>.Failure($"Parcela con Id {request.ParcelaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(parcela.FincaId))
		{
			result = Result<ProcesoCultivoDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == request.ProductoId, cancellationToken);
		if (producto is null)
		{
			result = Result<ProcesoCultivoDto>.Failure($"Producto con Id {request.ProductoId} no encontrado.");
			return result;
		}

		string? socioNombre = null;
		if (request.SocioId.HasValue)
		{
			var socio = await _context.Personas.FirstOrDefaultAsync(p => p.Id == request.SocioId.Value, cancellationToken);
			if (socio is null)
			{
				result = Result<ProcesoCultivoDto>.Failure($"Socio con Id {request.SocioId.Value} no encontrado.");
				return result;
			}
			socioNombre = socio.Nombre;
		}

		var tieneActivo = await _context.ProcesosCultivo
			.AnyAsync(pc => pc.ParcelaId == request.ParcelaId && pc.Estado == EstadoProceso.Activo, cancellationToken);

		if (tieneActivo)
		{
			result = Result<ProcesoCultivoDto>.Failure("La parcela ya tiene un proceso activo. Ciérralo antes de abrir uno nuevo.");
			return result;
		}

		var proceso = new ProcesoCultivo
		{
			ParcelaId = request.ParcelaId,
			ProductoId = request.ProductoId,
			SocioId = request.SocioId,
			CostoInicial = request.CostoInicial,
			FechaInicio = request.FechaInicio,
			FechaEstimadaCosecha = request.FechaEstimadaCosecha,
			Estado = EstadoProceso.Activo
		};

		_context.ProcesosCultivo.Add(proceso);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Proceso de cultivo creado",
			$"Se abrió un proceso de cultivo de '{producto.Nombre}' en la parcela '{parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<ProcesoCultivoDto>.Success(new ProcesoCultivoDto(
			proceso.Id, proceso.ParcelaId, parcela.Nombre,
			proceso.ProductoId, producto.Nombre,
			proceso.SocioId, socioNombre,
			proceso.CostoInicial, proceso.FechaInicio, proceso.FechaEstimadaCosecha,
			proceso.FechaCierre, proceso.Estado));
		return result;
	}
}
