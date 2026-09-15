using FarmControlAPI.Domain.Enums;

namespace FarmControlAPI.Domain.Entities;

public class Comentario
{
	public int Id { get; set; }
	public TipoEntidadComentario TipoEntidad { get; set; }
	public int EntidadId { get; set; }
	public int AutorPersonaId { get; set; }
	public string Texto { get; set; } = string.Empty;
	public DateTime Fecha { get; set; }

	public Persona Autor { get; set; } = null!;
}
