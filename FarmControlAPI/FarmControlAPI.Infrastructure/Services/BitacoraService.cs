using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Infrastructure.Services;

public class BitacoraService : IBitacoraService
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;

	public BitacoraService(IFarmControlDbContext context, ICurrentUserContext currentUser)
	{
		_context = context;
		_currentUser = currentUser;
	}

	public async Task RegistrarAsync(string accion, string detalle, int? clienteId = null, CancellationToken cancellationToken = default)
	{
		var actorNombre = await ResolverActorNombreAsync(cancellationToken);

		_context.BitacoraEntries.Add(new BitacoraEntry
		{
			FechaHora = DateTime.UtcNow,
			ClienteId = clienteId ?? _currentUser.ClienteId,
			ActorPersonaId = _currentUser.PersonaId,
			ActorNombre = actorNombre,
			Accion = accion,
			Detalle = detalle
		});

		await _context.SaveChangesAsync(cancellationToken);
	}

	private async Task<string> ResolverActorNombreAsync(CancellationToken cancellationToken)
	{
		string actorNombre;

		if (_currentUser.PersonaId.HasValue)
		{
			actorNombre = await _context.Personas
				.IgnoreQueryFilters()
				.Where(p => p.Id == _currentUser.PersonaId.Value)
				.Select(p => p.Nombre)
				.FirstOrDefaultAsync(cancellationToken) ?? "Usuario";
		}
		else
		{
			actorNombre = "SuperAdmin";
		}

		return actorNombre;
	}
}
