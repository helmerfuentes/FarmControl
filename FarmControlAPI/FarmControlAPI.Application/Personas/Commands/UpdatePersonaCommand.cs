using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Personas.DTOs;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Personas.Commands;

public record UpdatePersonaCommand(
	int Id,
	string Nombre,
	string Documento,
	string Telefono,
	string? Email,
	TipoPersona TipoPersona,
	decimal ValorDia,
	string? NombreUsuario,
	string? Password) : IRequest<Result<PersonaDto>>;

public class UpdatePersonaCommandHandler : IRequestHandler<UpdatePersonaCommand, Result<PersonaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public UpdatePersonaCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<PersonaDto>> Handle(UpdatePersonaCommand request, CancellationToken cancellationToken)
	{
		Result<PersonaDto> result;

		var persona = await _context.Personas
			.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

		if (persona is null)
		{
			result = Result<PersonaDto>.Failure($"Persona con Id {request.Id} no encontrada.");
			return result;
		}

		persona.Nombre = request.Nombre;
		persona.Documento = request.Documento;
		persona.Telefono = request.Telefono;
		persona.Email = request.Email;
		persona.TipoPersona = request.TipoPersona;
		persona.ValorDia = request.ValorDia;
		persona.NombreUsuario = request.NombreUsuario;

		if (!string.IsNullOrEmpty(request.Password))
		{
			persona.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
		}

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Persona actualizada",
			$"Se actualizaron los datos de '{persona.Nombre}' ({persona.TipoPersona}).",
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
