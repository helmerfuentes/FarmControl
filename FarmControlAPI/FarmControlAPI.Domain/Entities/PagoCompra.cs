namespace FarmControlAPI.Domain.Entities;

public class PagoCompra
{
	public int Id { get; set; }
	public int CompraId { get; set; }
	public decimal Monto { get; set; }
	public DateTime Fecha { get; set; }
	public string? Observacion { get; set; }

	public Compra Compra { get; set; } = null!;
}
