using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Tareas.Queries;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Tareas.Commands;

public record CreateTareaRecurrenteCommand(
	int ParcelaId,
	string Descripcion,
	int FrecuenciaDias,
	DateTime ProximaFecha) : IRequest<Result<TareaRecurrenteDto>>;

public class CreateTareaRecurrenteCommandHandler : IRequestHandler<CreateTareaRecurrenteCommand, Result<TareaRecurrenteDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateTareaRecurrenteCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<TareaRecurrenteDto>> Handle(CreateTareaRecurrenteCommand request, CancellationToken cancellationToken)
	{
		Result<TareaRecurrenteDto> result;

		if (request.FrecuenciaDias <= 0)
		{
			result = Result<TareaRecurrenteDto>.Failure("La frecuencia debe ser mayor a cero días.");
			return result;
		}

		var parcela = await _context.Parcelas.FirstOrDefaultAsync(p => p.Id == request.ParcelaId, cancellationToken);
		if (parcela is null)
		{
			result = Result<TareaRecurrenteDto>.Failure($"Parcela con Id {request.ParcelaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(parcela.FincaId))
		{
			result = Result<TareaRecurrenteDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var tarea = new TareaRecurrente
		{
			ParcelaId = request.ParcelaId,
			Descripcion = request.Descripcion,
			FrecuenciaDias = request.FrecuenciaDias,
			ProximaFecha = request.ProximaFecha,
			Activa = true
		};

		_context.TareasRecurrentes.Add(tarea);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Tarea recurrente creada",
			$"Se creó la tarea '{tarea.Descripcion}' cada {tarea.FrecuenciaDias} día(s) en la parcela '{parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<TareaRecurrenteDto>.Success(new TareaRecurrenteDto(
			tarea.Id, tarea.ParcelaId, parcela.Nombre, tarea.Descripcion,
			tarea.FrecuenciaDias, tarea.ProximaFecha, tarea.UltimaEjecucion, tarea.Activa));
		return result;
	}
}
