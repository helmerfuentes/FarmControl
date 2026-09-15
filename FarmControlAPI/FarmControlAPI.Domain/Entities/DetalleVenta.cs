using FarmControlAPI.Domain.Enums;

namespace FarmControlAPI.Domain.Entities;

public class DetalleVenta
{
	public int Id { get; set; }
	public int VentaId { get; set; }
	public string Clasificacion { get; set; } = string.Empty;
	public decimal Cantidad { get; set; }
	public UnidadMedidaVenta UnidadMedida { get; set; }
	public decimal PrecioUnitario { get; set; }
	public decimal Subtotal { get; set; }

	public Venta Venta { get; set; } = null!;
}
