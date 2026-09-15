using FarmControlAPI.Domain.Enums;

namespace FarmControlAPI.Domain.Entities;

public class HistorialPrecioVenta
{
	public int Id { get; set; }
	public int ProductoId { get; set; }
	public string Clasificacion { get; set; } = string.Empty;
	public decimal PrecioUnitario { get; set; }
	public UnidadMedidaVenta UnidadMedida { get; set; }
	public DateTime Fecha { get; set; }
	public int VentaId { get; set; }

	public Producto Producto { get; set; } = null!;
	public Venta Venta { get; set; } = null!;
}
