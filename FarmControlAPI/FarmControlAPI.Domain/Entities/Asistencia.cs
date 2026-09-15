namespace FarmControlAPI.Domain.Entities;

public class Asistencia
{
	public int Id { get; set; }
	public int PersonaId { get; set; }
	public int FincaId { get; set; }
	public DateTime Fecha { get; set; }
	public TimeSpan? HoraEntrada { get; set; }
	public TimeSpan? HoraSalida { get; set; }
	public string? Observacion { get; set; }

	public Persona Persona { get; set; } = null!;
	public Finca Finca { get; set; } = null!;
}
