namespace FarmControlAPI.Domain.Entities;

public class Actividad
{
	public int Id { get; set; }
	public int ParcelaId { get; set; }
	public string TipoActividad { get; set; } = string.Empty;
	public DateTime FechaInicio { get; set; }
	public DateTime? FechaFin { get; set; }
	public int? PersonaACargoId { get; set; }
	public decimal ValorDiaUsado { get; set; }
	public string? Descripcion { get; set; }
	public bool Confirmada { get; set; }
	public DateTime? FechaConfirmacion { get; set; }
	public string? ConfirmadaPor { get; set; }

	public int? ProcesoCultivoId { get; set; }

	public Parcela Parcela { get; set; } = null!;
	public Persona? PersonaACargo { get; set; }
	public ProcesoCultivo? ProcesoCultivo { get; set; }
	public ICollection<RegistroManoObra> RegistrosManoObra { get; set; } = [];
}
