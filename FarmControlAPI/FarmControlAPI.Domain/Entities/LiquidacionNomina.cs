namespace FarmControlAPI.Domain.Entities;

public class LiquidacionNomina
{
	public int Id { get; set; }
	public int JornaleroId { get; set; }
	public DateTime FechaInicio { get; set; }
	public DateTime FechaFin { get; set; }
	public decimal TotalHoras { get; set; }
	public decimal TotalPagar { get; set; }
	public DateTime FechaLiquidacion { get; set; }
	public string? Observacion { get; set; }

	public Persona Jornalero { get; set; } = null!;
	public ICollection<RegistroManoObra> Registros { get; set; } = [];
}
