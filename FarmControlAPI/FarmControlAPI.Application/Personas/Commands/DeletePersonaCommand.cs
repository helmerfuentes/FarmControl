using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Personas.Commands;

public record DeletePersonaCommand(int Id) : IRequest<Result<bool>>;

public class DeletePersonaCommandHandler : IRequestHandler<DeletePersonaCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public DeletePersonaCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeletePersonaCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var persona = await _context.Personas
			.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

		if (persona is null)
		{
			result = Result<bool>.Failure($"Persona con Id {request.Id} no encontrada.");
			return result;
		}

		if (persona.TipoPersona == TipoPersona.Admin)
		{
			result = Result<bool>.Failure("No se puede eliminar un usuario Administrador. Gestiónalo (habilitar/deshabilitar) desde el panel de Clientes.");
			return result;
		}

		_context.Personas.Remove(persona);

		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			result = Result<bool>.Failure("No se puede eliminar esta persona: tiene actividades, ventas o registros de mano de obra asociados. Elimina o reasigna esos registros primero.");
			return result;
		}

		await _bitacora.RegistrarAsync(
			"Persona eliminada",
			$"Se eliminó a '{persona.Nombre}' ({persona.TipoPersona}) del catálogo de personas.",
			persona.ClienteId,
			cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
