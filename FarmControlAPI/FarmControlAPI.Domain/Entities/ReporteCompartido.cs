namespace FarmControlAPI.Domain.Entities;

public class ReporteCompartido
{
	public int Id { get; set; }
	public string Token { get; set; } = string.Empty;
	public int FincaId { get; set; }
	public DateTime FechaCreacion { get; set; }
	public DateTime? FechaExpiracion { get; set; }
	public int? CreadoPorPersonaId { get; set; }

	public Finca Finca { get; set; } = null!;
}
