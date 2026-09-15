using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.ProcesosCultivo.Queries;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.ProcesosCultivo.Commands;

public record UpdateProcesoCultivoCommand(
	int Id,
	int ProductoId,
	int? SocioId,
	decimal CostoInicial,
	DateTime FechaInicio,
	DateTime? FechaEstimadaCosecha,
	DateTime? FechaCierre,
	EstadoProceso Estado) : IRequest<Result<ProcesoCultivoDto>>;

public class UpdateProcesoCultivoCommandHandler : IRequestHandler<UpdateProcesoCultivoCommand, Result<ProcesoCultivoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public UpdateProcesoCultivoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ProcesoCultivoDto>> Handle(UpdateProcesoCultivoCommand request, CancellationToken cancellationToken)
	{
		Result<ProcesoCultivoDto> result;

		var proceso = await _context.ProcesosCultivo
			.Include(pc => pc.Parcela)
			.FirstOrDefaultAsync(pc => pc.Id == request.Id, cancellationToken);

		if (proceso is null)
		{
			result = Result<ProcesoCultivoDto>.Failure($"Proceso de cultivo con Id {request.Id} no encontrado.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(proceso.Parcela.FincaId))
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

		proceso.ProductoId = request.ProductoId;
		proceso.SocioId = request.SocioId;
		proceso.CostoInicial = request.CostoInicial;
		proceso.FechaInicio = request.FechaInicio;
		proceso.FechaEstimadaCosecha = request.FechaEstimadaCosecha;
		proceso.FechaCierre = request.FechaCierre;
		proceso.Estado = request.Estado;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Proceso de cultivo actualizado",
			$"Se actualizó el proceso de cultivo de '{producto.Nombre}' en la parcela '{proceso.Parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<ProcesoCultivoDto>.Success(new ProcesoCultivoDto(
			proceso.Id, proceso.ParcelaId, proceso.Parcela.Nombre,
			proceso.ProductoId, producto.Nombre,
			proceso.SocioId, socioNombre,
			proceso.CostoInicial, proceso.FechaInicio, proceso.FechaEstimadaCosecha,
			proceso.FechaCierre, proceso.Estado));
		return result;
	}
}
