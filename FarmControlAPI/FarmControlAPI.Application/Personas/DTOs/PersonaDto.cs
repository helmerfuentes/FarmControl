using FarmControlAPI.Domain.Enums;

namespace FarmControlAPI.Application.Personas.DTOs;

public class PersonaDto
{
	public int Id { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public string Documento { get; set; } = string.Empty;
	public string Telefono { get; set; } = string.Empty;
	public string? Email { get; set; }
	public TipoPersona TipoPersona { get; set; }
	public decimal ValorDia { get; set; }
}
