namespace FarmControlAPI.Domain.Entities;

public class Parcela
{
	public int Id { get; set; }
	public int FincaId { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public decimal Area { get; set; }

	public Finca Finca { get; set; } = null!;
	public ICollection<Actividad> Actividades { get; set; } = [];
	public ICollection<MovimientoInsumo> MovimientosInsumo { get; set; } = [];
	public ICollection<Venta> Ventas { get; set; } = [];
	public ICollection<ProcesoCultivo> ProcesosCultivo { get; set; } = [];
	public ICollection<TareaRecurrente> TareasRecurrentes { get; set; } = [];
	public ICollection<AnalisisSuelo> AnalisisSuelo { get; set; } = [];
}
