using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Commands;

public record AsignarCredencialesCommand(int PersonaId, string NombreUsuario, string Contrasena, List<AccesoFincaInput>? Accesos = null) : IRequest<Result<bool>>;

public class AsignarCredencialesCommandHandler : IRequestHandler<AsignarCredencialesCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	private const int _LONGITUD_MINIMA_CONTRASENA = 6;

	public AsignarCredencialesCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(AsignarCredencialesCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		if (string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.Contrasena))
		{
			result = Result<bool>.Failure("Debe indicar nombre de usuario y contraseña.");
			return result;
		}

		if (request.Contrasena.Length < _LONGITUD_MINIMA_CONTRASENA)
		{
			result = Result<bool>.Failure($"La contraseña debe tener al menos {_LONGITUD_MINIMA_CONTRASENA} caracteres.");
			return result;
		}

		var persona = await _context.Personas.FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);
		if (persona is null)
		{
			result = Result<bool>.Failure($"Persona con Id {request.PersonaId} no encontrada.");
			return result;
		}

		if (!_currentUser.TieneAccesoGlobal && persona.ClienteId != _currentUser.ClienteId)
		{
			result = Result<bool>.Failure("No tiene acceso a esta persona.");
			return result;
		}

		var usuarioEnUso = await _context.Personas
			.AnyAsync(p => p.Id != request.PersonaId && p.NombreUsuario == request.NombreUsuario, cancellationToken);
		if (usuarioEnUso)
		{
			result = Result<bool>.Failure($"El nombre de usuario '{request.NombreUsuario}' ya está en uso.");
			return result;
		}

		var accesosNuevos = (request.Accesos ?? []).GroupBy(a => a.FincaId).Select(g => g.First()).ToList();

		if (accesosNuevos.Count > 0)
		{
			var fincaIds = accesosNuevos.Select(a => a.FincaId).ToList();

			if (!_currentUser.TieneAccesoGlobal && fincaIds.Any(id => !_currentUser.FincaIds.Contains(id)))
			{
				result = Result<bool>.Failure("No tiene acceso a una o más de las fincas indicadas.");
				return result;
			}

			var fincasDelCliente = await _context.Fincas
				.Where(f => fincaIds.Contains(f.Id) && f.ClienteId == persona.ClienteId)
				.Select(f => f.Id)
				.ToListAsync(cancellationToken);

			if (fincasDelCliente.Count != fincaIds.Count)
			{
				result = Result<bool>.Failure("Una o más fincas indicadas no existen o no pertenecen a este cliente.");
				return result;
			}

			var accesosExistentes = await _context.PersonasFincas
				.Where(pf => pf.PersonaId == persona.Id && fincaIds.Contains(pf.FincaId))
				.ToListAsync(cancellationToken);

			foreach (var acceso in accesosNuevos)
			{
				var existente = accesosExistentes.FirstOrDefault(pf => pf.FincaId == acceso.FincaId);
				if (existente is not null)
				{
					existente.SoloLectura = acceso.SoloLectura;
					existente.PuedeEliminar = !acceso.SoloLectura && acceso.PuedeEliminar;
				}
				else
				{
					_context.PersonasFincas.Add(new PersonaFinca
					{
						PersonaId = persona.Id,
						FincaId = acceso.FincaId,
						FechaAsignacion = DateTime.UtcNow,
						SoloLectura = acceso.SoloLectura,
						PuedeEliminar = !acceso.SoloLectura && acceso.PuedeEliminar
					});
				}
			}
		}

		var tieneAccesoAFinca = accesosNuevos.Count > 0 ||
			await _context.PersonasFincas.AnyAsync(pf => pf.PersonaId == persona.Id, cancellationToken);
		if (!tieneAccesoAFinca)
		{
			result = Result<bool>.Failure("Esta persona no tiene acceso asignado a ninguna finca. Asigne al menos una finca.");
			return result;
		}

		persona.NombreUsuario = request.NombreUsuario;
		persona.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena);

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Credenciales asignadas",
			$"Se asignó acceso de usuario ('{request.NombreUsuario}') a '{persona.Nombre}'.",
			persona.ClienteId,
			cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
