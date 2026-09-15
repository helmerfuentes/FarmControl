namespace FarmControlAPI.Domain.Entities;

public class Producto
{
	public int Id { get; set; }
	public int ClienteId { get; set; }
	public string Nombre { get; set; } = string.Empty;

	public Cliente Cliente { get; set; } = null!;
	public ICollection<ClasificacionProducto> Clasificaciones { get; set; } = [];
	public ICollection<Parcela> Parcelas { get; set; } = [];
}
