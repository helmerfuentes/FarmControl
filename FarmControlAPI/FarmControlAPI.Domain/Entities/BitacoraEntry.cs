namespace FarmControlAPI.Domain.Entities;

public class BitacoraEntry
{
	public int Id { get; set; }
	public DateTime FechaHora { get; set; }
	public int? ClienteId { get; set; }
	public int? ActorPersonaId { get; set; }
	public string ActorNombre { get; set; } = string.Empty;
	public string Accion { get; set; } = string.Empty;
	public string Detalle { get; set; } = string.Empty;
}
