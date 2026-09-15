namespace FarmControlAPI.Domain.Entities;

public class PagoVenta
{
	public int Id { get; set; }
	public int VentaId { get; set; }
	public decimal Monto { get; set; }
	public DateTime Fecha { get; set; }
	public string? Observacion { get; set; }

	public Venta Venta { get; set; } = null!;
}
