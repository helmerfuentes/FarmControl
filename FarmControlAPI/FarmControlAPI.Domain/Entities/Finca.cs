namespace FarmControlAPI.Domain.Entities;

public class Finca
{
	public int Id { get; set; }
	public int ClienteId { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public string? Ubicacion { get; set; }
	public decimal AreaTotal { get; set; }
	public decimal CostoTerreno { get; set; }

	public Cliente Cliente { get; set; } = null!;
	public ICollection<Parcela> Parcelas { get; set; } = [];
	public ICollection<Compra> Compras { get; set; } = [];
	public ICollection<PersonaFinca> AsignacionesPersona { get; set; } = [];
}
