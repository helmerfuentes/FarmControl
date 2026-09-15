namespace FarmControlAPI.Domain.Entities;

public class Cliente
{
	public int Id { get; set; }
	public string RazonSocial { get; set; } = string.Empty;
	public string NIT { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? Telefono { get; set; }
	public bool Activo { get; set; } = true;
	public DateTime FechaAlta { get; set; }
	public DateTime? FechaVencimientoContrato { get; set; }
	public int? PlanId { get; set; }

	public Plan? Plan { get; set; }
	public ICollection<Finca> Fincas { get; set; } = [];
	public ICollection<Producto> Productos { get; set; } = [];
	public ICollection<TipoInsumo> TiposInsumo { get; set; } = [];
}
