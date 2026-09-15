using FarmControlAPI.Application.Asistencias.Queries;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Asistencias.Commands;

public record CreateAsistenciaCommand(
	int PersonaId,
	int FincaId,
	DateTime Fecha,
	TimeSpan? HoraEntrada,
	TimeSpan? HoraSalida,
	string? Observacion) : IRequest<Result<AsistenciaDto>>;

public class CreateAsistenciaCommandHandler : IRequestHandler<CreateAsistenciaCommand, Result<AsistenciaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateAsistenciaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<AsistenciaDto>> Handle(CreateAsistenciaCommand request, CancellationToken cancellationToken)
	{
		Result<AsistenciaDto> result;

		var finca = await _context.Fincas.FirstOrDefaultAsync(f => f.Id == request.FincaId, cancellationToken);
		if (finca is null)
		{
			result = Result<AsistenciaDto>.Failure($"Finca con Id {request.FincaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(request.FincaId))
		{
			result = Result<AsistenciaDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var persona = await _context.Personas.FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);
		if (persona is null)
		{
			result = Result<AsistenciaDto>.Failure($"Persona con Id {request.PersonaId} no encontrada.");
			return result;
		}

		var asistencia = new Asistencia
		{
			PersonaId = request.PersonaId,
			FincaId = request.FincaId,
			Fecha = request.Fecha.Date,
			HoraEntrada = request.HoraEntrada,
			HoraSalida = request.HoraSalida,
			Observacion = request.Observacion
		};

		_context.Asistencias.Add(asistencia);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Asistencia registrada",
			$"Se registró la asistencia de '{persona.Nombre}' en la finca '{finca.Nombre}' para el {asistencia.Fecha:dd/MM/yyyy}.",
			cancellationToken: cancellationToken);

		result = Result<AsistenciaDto>.Success(new AsistenciaDto(
			asistencia.Id, asistencia.PersonaId, persona.Nombre, asistencia.FincaId, finca.Nombre,
			asistencia.Fecha, asistencia.HoraEntrada, asistencia.HoraSalida, asistencia.Observacion));
		return result;
	}
}
