using FarmControlAPI.Domain.Enums;

namespace FarmControlAPI.Domain.Entities;

public class ClasificacionProducto
{
	public int Id { get; set; }
	public int ProductoId { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public UnidadMedidaVenta UnidadMedida { get; set; }
	public decimal PesoUnidadKg { get; set; }

	public Producto Producto { get; set; } = null!;
}
