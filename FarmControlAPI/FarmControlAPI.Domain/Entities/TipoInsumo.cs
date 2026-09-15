namespace FarmControlAPI.Domain.Entities;

public class TipoInsumo
{
	public int Id { get; set; }
	public int ClienteId { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public string? Descripcion { get; set; }

	public Cliente Cliente { get; set; } = null!;
	public ICollection<Insumo> Insumos { get; set; } = [];
}
