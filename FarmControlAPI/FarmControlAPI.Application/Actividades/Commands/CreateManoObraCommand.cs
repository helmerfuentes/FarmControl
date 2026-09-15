using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Actividades.Commands;

public record ManoObraDto(
	int Id, int ActividadId, int JornaleroId, string JornaleroNombre,
	int SocioId, string SocioNombre,
	decimal ValorHora, decimal NumHoras,
	string HoraInicio, string HoraSalida);

public record CreateManoObraCommand(
	int ActividadId,
	int JornaleroId,
	int SocioId,
	decimal ValorHora,
	decimal NumHoras,
	string? HoraInicio,
	string? HoraSalida) : IRequest<Result<ManoObraDto>>;

public class CreateManoObraCommandHandler : IRequestHandler<CreateManoObraCommand, Result<ManoObraDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateManoObraCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ManoObraDto>> Handle(CreateManoObraCommand request, CancellationToken cancellationToken)
	{
		Result<ManoObraDto> result;

		var fincaId = await _context.Actividades
			.Where(a => a.Id == request.ActividadId)
			.Select(a => (int?)a.Parcela.FincaId)
			.FirstOrDefaultAsync(cancellationToken);

		if (fincaId is null)
		{
			result = Result<ManoObraDto>.Failure($"Actividad con Id {request.ActividadId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(fincaId.Value))
		{
			result = Result<ManoObraDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var jornalero = await _context.Personas.FirstOrDefaultAsync(p => p.Id == request.JornaleroId, cancellationToken);
		if (jornalero is null)
		{
			result = Result<ManoObraDto>.Failure($"Jornalero con Id {request.JornaleroId} no encontrado.");
			return result;
		}

		var socio = await _context.Personas.FirstOrDefaultAsync(p => p.Id == request.SocioId, cancellationToken);
		if (socio is null)
		{
			result = Result<ManoObraDto>.Failure($"Socio con Id {request.SocioId} no encontrado.");
			return result;
		}

		var horaInicio = TimeSpan.TryParse(request.HoraInicio, out var hi) ? hi : TimeSpan.Zero;
		var horaSalida = TimeSpan.TryParse(request.HoraSalida, out var hs) ? hs : TimeSpan.Zero;

		var registro = new RegistroManoObra
		{
			ActividadId = request.ActividadId,
			JornaleroId = request.JornaleroId,
			SocioId = request.SocioId,
			ValorHora = request.ValorHora,
			NumHoras = request.NumHoras,
			HoraInicio = horaInicio,
			HoraSalida = horaSalida
		};

		_context.RegistrosManoObra.Add(registro);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Mano de obra registrada",
			$"Se registró mano de obra de '{jornalero.Nombre}' ({registro.NumHoras}h).",
			cancellationToken: cancellationToken);

		result = Result<ManoObraDto>.Success(new ManoObraDto(
			registro.Id, registro.ActividadId,
			registro.JornaleroId, jornalero.Nombre,
			registro.SocioId, socio.Nombre,
			registro.ValorHora, registro.NumHoras,
			registro.HoraInicio.ToString(), registro.HoraSalida.ToString()));
		return result;
	}
}
