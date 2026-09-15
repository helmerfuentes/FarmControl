using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Personas.DTOs;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Commands;

public record AccesoFincaInput(int FincaId, bool SoloLectura, bool PuedeEliminar);

public record UsuarioMiClienteRequest(
	string Nombre,
	string Documento,
	string Telefono,
	string? Email,
	TipoPersona TipoPersona,
	string NombreUsuario,
	string Contrasena,
	List<AccesoFincaInput> Accesos);

public record CreatePersonaConAccesoCommand(
	int ClienteId,
	string Nombre,
	string Documento,
	string Telefono,
	string? Email,
	TipoPersona TipoPersona,
	string NombreUsuario,
	string Contrasena,
	List<AccesoFincaInput> Accesos) : IRequest<Result<PersonaDto>>;

public class CreatePersonaConAccesoCommandHandler : IRequestHandler<CreatePersonaConAccesoCommand, Result<PersonaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreatePersonaConAccesoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<PersonaDto>> Handle(CreatePersonaConAccesoCommand request, CancellationToken cancellationToken)
	{
		Result<PersonaDto> result;

		var accesos = (request.Accesos ?? []).GroupBy(a => a.FincaId).Select(g => g.First()).ToList();
		var fincaIds = accesos.Select(a => a.FincaId).ToList();

		if (fincaIds.Count == 0)
		{
			result = Result<PersonaDto>.Failure("Debe asignar al menos una finca.");
			return result;
		}

		if (!_currentUser.TieneAccesoGlobal && fincaIds.Any(id => !_currentUser.FincaIds.Contains(id)))
		{
			result = Result<PersonaDto>.Failure("No tiene acceso a una o más de las fincas indicadas.");
			return result;
		}

		var fincasDelCliente = await _context.Fincas
			.Where(f => fincaIds.Contains(f.Id) && f.ClienteId == request.ClienteId)
			.Select(f => f.Id)
			.ToListAsync(cancellationToken);

		if (fincasDelCliente.Count != fincaIds.Count)
		{
			result = Result<PersonaDto>.Failure("Una o más fincas indicadas no existen o no pertenecen a este cliente.");
			return result;
		}

		var usuarioEnUso = await _context.Personas
			.AnyAsync(p => p.NombreUsuario == request.NombreUsuario, cancellationToken);

		if (usuarioEnUso)
		{
			result = Result<PersonaDto>.Failure($"El nombre de usuario '{request.NombreUsuario}' ya está en uso.");
			return result;
		}

		var plan = await _context.Clientes
			.Where(c => c.Id == request.ClienteId)
			.Select(c => c.Plan)
			.FirstOrDefaultAsync(cancellationToken);

		if (plan is not null && plan.MaxUsuarios != -1)
		{
			var numUsuariosActuales = await _context.Personas
				.CountAsync(p => p.ClienteId == request.ClienteId && p.NombreUsuario != null, cancellationToken);
			if (numUsuariosActuales >= plan.MaxUsuarios)
			{
				result = Result<PersonaDto>.Failure("Se alcanzó el límite de usuarios del plan contratado.");
				return result;
			}
		}

		var persona = new Persona
		{
			ClienteId = request.ClienteId,
			Nombre = request.Nombre,
			Documento = request.Documento,
			Telefono = request.Telefono,
			Email = request.Email,
			TipoPersona = request.TipoPersona,
			NombreUsuario = request.NombreUsuario,
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena)
		};

		foreach (var acceso in accesos)
		{
			persona.AsignacionesFinca.Add(new PersonaFinca
			{
				FincaId = acceso.FincaId,
				FechaAsignacion = DateTime.UtcNow,
				SoloLectura = acceso.SoloLectura,
				PuedeEliminar = !acceso.SoloLectura && acceso.PuedeEliminar
			});
		}

		_context.Personas.Add(persona);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Usuario creado",
			$"Se creó el usuario '{persona.NombreUsuario}' ({persona.Nombre}, {persona.TipoPersona}) con acceso a {fincaIds.Count} finca(s).",
			request.ClienteId,
			cancellationToken);

		var dto = new PersonaDto
		{
			Id = persona.Id,
			Nombre = persona.Nombre,
			Documento = persona.Documento,
			Telefono = persona.Telefono,
			Email = persona.Email,
			TipoPersona = persona.TipoPersona,
			ValorDia = persona.ValorDia
		};

		result = Result<PersonaDto>.Success(dto);
		return result;
	}
}
