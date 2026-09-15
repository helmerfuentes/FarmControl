using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Actividades.Commands;

public record ActividadDto(
	int Id,
	int ParcelaId,
	string TipoActividad,
	DateTime FechaInicio,
	DateTime? FechaFin,
	int? PersonaACargoId,
	string? PersonaACargoNombre,
	string? Descripcion,
	decimal ValorDiaUsado,
	decimal CostoCalculado,
	string ParcelaNombre = "",
	string FincaNombre = "",
	int? ProcesoCultivoId = null,
	bool Confirmada = false,
	DateTime? FechaConfirmacion = null,
	string? ConfirmadaPor = null);

public record CreateActividadCommand(
	int ParcelaId,
	string TipoActividad,
	DateTime FechaInicio,
	DateTime? FechaFin,
	int? PersonaACargoId,
	decimal ValorDiaActividad,
	string? Descripcion) : IRequest<Result<ActividadDto>>;

public class CreateActividadCommandHandler : IRequestHandler<CreateActividadCommand, Result<ActividadDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateActividadCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ActividadDto>> Handle(CreateActividadCommand request, CancellationToken cancellationToken)
	{
		Result<ActividadDto> result;

		var fincaId = await _context.Parcelas
			.Where(p => p.Id == request.ParcelaId)
			.Select(p => (int?)p.FincaId)
			.FirstOrDefaultAsync(cancellationToken);

		if (fincaId is null)
		{
			result = Result<ActividadDto>.Failure($"Parcela con Id {request.ParcelaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(fincaId.Value))
		{
			result = Result<ActividadDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		string? personaACargoNombre = null;
		if (request.PersonaACargoId.HasValue)
		{
			var persona = await _context.Personas.FindAsync([request.PersonaACargoId.Value], cancellationToken);
			personaACargoNombre = persona?.Nombre;
		}

		var procesoId = await _context.ProcesosCultivo
			.Where(pc => pc.ParcelaId == request.ParcelaId && pc.Estado == EstadoProceso.Activo)
			.Select(pc => (int?)pc.Id)
			.FirstOrDefaultAsync(cancellationToken);

		var actividad = new Actividad
		{
			ParcelaId = request.ParcelaId,
			TipoActividad = request.TipoActividad,
			FechaInicio = request.FechaInicio,
			FechaFin = request.FechaFin,
			PersonaACargoId = request.PersonaACargoId,
			ValorDiaUsado = request.ValorDiaActividad,
			Descripcion = request.Descripcion,
			ProcesoCultivoId = procesoId
		};

		_context.Actividades.Add(actividad);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Actividad creada",
			$"Se registró la actividad '{actividad.TipoActividad}'.",
			cancellationToken: cancellationToken);

		var costo = CalcularCosto(actividad.FechaInicio, actividad.FechaFin, actividad.ValorDiaUsado);
		result = Result<ActividadDto>.Success(new ActividadDto(
			actividad.Id, actividad.ParcelaId, actividad.TipoActividad,
			actividad.FechaInicio, actividad.FechaFin,
			actividad.PersonaACargoId, personaACargoNombre,
			actividad.Descripcion, actividad.ValorDiaUsado, costo,
			ProcesoCultivoId: procesoId));
		return result;
	}

	internal static decimal CalcularCosto(DateTime inicio, DateTime? fin, decimal valorDia)
	{
		if (fin is null || valorDia <= 0) { return 0; }
		var horas = (decimal)(fin.Value - inicio).TotalHours;
		return Math.Round(horas / 8m * valorDia, 0);
	}
}
