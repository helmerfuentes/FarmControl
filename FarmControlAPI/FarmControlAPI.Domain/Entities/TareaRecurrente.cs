namespace FarmControlAPI.Domain.Entities;

public class TareaRecurrente
{
	public int Id { get; set; }
	public int ParcelaId { get; set; }
	public string Descripcion { get; set; } = string.Empty;
	public int FrecuenciaDias { get; set; }
	public DateTime ProximaFecha { get; set; }
	public DateTime? UltimaEjecucion { get; set; }
	public bool Activa { get; set; } = true;

	public Parcela Parcela { get; set; } = null!;
}
