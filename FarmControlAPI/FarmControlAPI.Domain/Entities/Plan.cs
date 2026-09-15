namespace FarmControlAPI.Domain.Entities;

public class Plan
{
	public int Id { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public string? Descripcion { get; set; }
	public int MaxFincas { get; set; } = -1;
	public int MaxUsuarios { get; set; } = -1;

	public ICollection<Cliente> Clientes { get; set; } = [];
}
