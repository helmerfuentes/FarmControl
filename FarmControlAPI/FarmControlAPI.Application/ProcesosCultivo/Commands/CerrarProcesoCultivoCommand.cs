using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.ProcesosCultivo.Queries;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.ProcesosCultivo.Commands;

public record CerrarProcesoCultivoCommand(int Id, EstadoProceso Estado, DateTime FechaCierre) : IRequest<Result<ProcesoCultivoDto>>;

public class CerrarProcesoCultivoCommandHandler : IRequestHandler<CerrarProcesoCultivoCommand, Result<ProcesoCultivoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CerrarProcesoCultivoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ProcesoCultivoDto>> Handle(CerrarProcesoCultivoCommand request, CancellationToken cancellationToken)
	{
		Result<ProcesoCultivoDto> result;

		if (request.Estado == EstadoProceso.Activo)
		{
			result = Result<ProcesoCultivoDto>.Failure("El estado de cierre debe ser Cosechado o Cerrado.");
			return result;
		}

		var proceso = await _context.ProcesosCultivo
			.Include(pc => pc.Parcela)
			.Include(pc => pc.Producto)
			.Include(pc => pc.Socio)
			.FirstOrDefaultAsync(pc => pc.Id == request.Id, cancellationToken);

		if (proceso is null)
		{
			result = Result<ProcesoCultivoDto>.Failure($"Proceso de cultivo con Id {request.Id} no encontrado.");
			return result;
		}

		if (proceso.Estado != EstadoProceso.Activo)
		{
			result = Result<ProcesoCultivoDto>.Failure("El proceso no está activo y no puede cerrarse.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(proceso.Parcela.FincaId))
		{
			result = Result<ProcesoCultivoDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		proceso.Estado = request.Estado;
		proceso.FechaCierre = request.FechaCierre;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Proceso de cultivo cerrado",
			$"Se cerró el proceso de cultivo de '{proceso.Producto.Nombre}' en la parcela '{proceso.Parcela.Nombre}' como '{proceso.Estado}'.",
			cancellationToken: cancellationToken);

		result = Result<ProcesoCultivoDto>.Success(new ProcesoCultivoDto(
			proceso.Id, proceso.ParcelaId, proceso.Parcela.Nombre,
			proceso.ProductoId, proceso.Producto.Nombre,
			proceso.SocioId, proceso.Socio?.Nombre,
			proceso.CostoInicial, proceso.FechaInicio, proceso.FechaEstimadaCosecha,
			proceso.FechaCierre, proceso.Estado));
		return result;
	}
}
