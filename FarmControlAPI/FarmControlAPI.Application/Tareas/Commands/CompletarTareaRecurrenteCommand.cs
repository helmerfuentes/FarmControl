using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Tareas.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Tareas.Commands;

public record CompletarTareaRecurrenteCommand(int Id) : IRequest<Result<TareaRecurrenteDto>>;

public class CompletarTareaRecurrenteCommandHandler : IRequestHandler<CompletarTareaRecurrenteCommand, Result<TareaRecurrenteDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CompletarTareaRecurrenteCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<TareaRecurrenteDto>> Handle(CompletarTareaRecurrenteCommand request, CancellationToken cancellationToken)
	{
		Result<TareaRecurrenteDto> result;

		var tarea = await _context.TareasRecurrentes
			.Include(t => t.Parcela)
			.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

		if (tarea is null)
		{
			result = Result<TareaRecurrenteDto>.Failure($"Tarea con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(tarea.Parcela.FincaId))
		{
			result = Result<TareaRecurrenteDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var hoy = DateTime.UtcNow.Date;
		tarea.UltimaEjecucion = hoy;
		tarea.ProximaFecha = hoy.AddDays(tarea.FrecuenciaDias);

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Tarea recurrente completada",
			$"Se marcó como hecha la tarea '{tarea.Descripcion}' en la parcela '{tarea.Parcela.Nombre}'. Próxima: {tarea.ProximaFecha:dd/MM/yyyy}.",
			cancellationToken: cancellationToken);

		result = Result<TareaRecurrenteDto>.Success(new TareaRecurrenteDto(
			tarea.Id, tarea.ParcelaId, tarea.Parcela.Nombre, tarea.Descripcion,
			tarea.FrecuenciaDias, tarea.ProximaFecha, tarea.UltimaEjecucion, tarea.Activa));
		return result;
	}
}
