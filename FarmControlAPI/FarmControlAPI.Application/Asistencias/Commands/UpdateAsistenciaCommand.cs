using FarmControlAPI.Application.Asistencias.Queries;
using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Asistencias.Commands;

public record UpdateAsistenciaCommand(
	int Id,
	TimeSpan? HoraEntrada,
	TimeSpan? HoraSalida,
	string? Observacion) : IRequest<Result<AsistenciaDto>>;

public class UpdateAsistenciaCommandHandler : IRequestHandler<UpdateAsistenciaCommand, Result<AsistenciaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public UpdateAsistenciaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<AsistenciaDto>> Handle(UpdateAsistenciaCommand request, CancellationToken cancellationToken)
	{
		Result<AsistenciaDto> result;

		var asistencia = await _context.Asistencias
			.Include(a => a.Persona)
			.Include(a => a.Finca)
			.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

		if (asistencia is null)
		{
			result = Result<AsistenciaDto>.Failure($"Asistencia con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(asistencia.FincaId))
		{
			result = Result<AsistenciaDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		asistencia.HoraEntrada = request.HoraEntrada;
		asistencia.HoraSalida = request.HoraSalida;
		asistencia.Observacion = request.Observacion;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Asistencia actualizada",
			$"Se actualizó la asistencia de '{asistencia.Persona.Nombre}' del {asistencia.Fecha:dd/MM/yyyy}.",
			cancellationToken: cancellationToken);

		result = Result<AsistenciaDto>.Success(new AsistenciaDto(
			asistencia.Id, asistencia.PersonaId, asistencia.Persona.Nombre, asistencia.FincaId, asistencia.Finca.Nombre,
			asistencia.Fecha, asistencia.HoraEntrada, asistencia.HoraSalida, asistencia.Observacion));
		return result;
	}
}
