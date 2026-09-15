namespace FarmControlAPI.Domain.Entities;

public class PersonaFinca
{
	public int Id { get; set; }
	public int PersonaId { get; set; }
	public int FincaId { get; set; }
	public DateTime FechaAsignacion { get; set; }
	public bool SoloLectura { get; set; }
	public bool PuedeEliminar { get; set; } = true;

	public Persona Persona { get; set; } = null!;
	public Finca Finca { get; set; } = null!;
}
