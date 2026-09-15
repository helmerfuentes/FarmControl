using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Personas.DTOs;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;

namespace FarmControlAPI.Application.Personas.Commands;

public record CreatePersonaCommand(
	string Nombre,
	string Documento,
	string Telefono,
	string? Email,
	TipoPersona TipoPersona,
	decimal ValorDia,
	string? NombreUsuario,
	string? Password) : IRequest<Result<PersonaDto>>;

public class CreatePersonaCommandHandler : IRequestHandler<CreatePersonaCommand, Result<PersonaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreatePersonaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<PersonaDto>> Handle(CreatePersonaCommand request, CancellationToken cancellationToken)
	{
		Result<PersonaDto> result;

		if (_currentUser.ClienteId is null)
		{
			result = Result<PersonaDto>.Failure("El usuario actual no tiene un cliente asociado.");
			return result;
		}

		var persona = new Persona
		{
			ClienteId = _currentUser.ClienteId.Value,
			Nombre = request.Nombre,
			Documento = request.Documento,
			Telefono = request.Telefono,
			Email = request.Email,
			TipoPersona = request.TipoPersona,
			ValorDia = request.ValorDia,
			NombreUsuario = request.NombreUsuario,
			PasswordHash = string.IsNullOrEmpty(request.Password) ? null : BCrypt.Net.BCrypt.HashPassword(request.Password)
		};

		_context.Personas.Add(persona);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Persona creada",
			$"Se registró a '{persona.Nombre}' ({persona.TipoPersona}) en el catálogo de personas.",
			persona.ClienteId,
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
