using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Reportes.Commands;

public record ReporteCompartidoDto(string Token, int FincaId, DateTime FechaCreacion, DateTime? FechaExpiracion);

public record CrearReporteCompartidoCommand(int FincaId, int? DiasValidez) : IRequest<Result<ReporteCompartidoDto>>;

public class CrearReporteCompartidoCommandHandler : IRequestHandler<CrearReporteCompartidoCommand, Result<ReporteCompartidoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CrearReporteCompartidoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ReporteCompartidoDto>> Handle(CrearReporteCompartidoCommand request, CancellationToken cancellationToken)
	{
		Result<ReporteCompartidoDto> result;

		var finca = await _context.Fincas.FirstOrDefaultAsync(f => f.Id == request.FincaId, cancellationToken);
		if (finca is null)
		{
			result = Result<ReporteCompartidoDto>.Failure($"Finca con Id {request.FincaId} no encontrada.");
			return result;
		}

		if (!_currentUser.TieneAccesoAFinca(request.FincaId))
		{
			result = Result<ReporteCompartidoDto>.Failure("No tienes acceso a esta finca.");
			return result;
		}

		var compartido = new ReporteCompartido
		{
			Token = Guid.NewGuid().ToString("N"),
			FincaId = request.FincaId,
			FechaCreacion = DateTime.UtcNow,
			FechaExpiracion = request.DiasValidez.HasValue ? DateTime.UtcNow.AddDays(request.DiasValidez.Value) : null,
			CreadoPorPersonaId = _currentUser.PersonaId
		};

		_context.ReportesCompartidos.Add(compartido);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Reporte compartido creado",
			$"Se generó un enlace público de solo lectura para el reporte de la finca '{finca.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<ReporteCompartidoDto>.Success(new ReporteCompartidoDto(
			compartido.Token, compartido.FincaId, compartido.FechaCreacion, compartido.FechaExpiracion));
		return result;
	}
}
