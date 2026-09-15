namespace FarmControlAPI.Domain.Entities;

public class RegistroManoObra
{
	public int Id { get; set; }
	public int ActividadId { get; set; }
	public int JornaleroId { get; set; }
	public int SocioId { get; set; }
	public decimal ValorHora { get; set; }
	public decimal NumHoras { get; set; }
	public TimeSpan HoraInicio { get; set; }
	public TimeSpan HoraSalida { get; set; }
	public int? LiquidacionNominaId { get; set; }

	public Actividad Actividad { get; set; } = null!;
	public Persona Jornalero { get; set; } = null!;
	public Persona Socio { get; set; } = null!;
	public LiquidacionNomina? LiquidacionNomina { get; set; }
}
