namespace FarmControlAPI.Domain.Entities;

public class AnalisisSuelo
{
	public int Id { get; set; }
	public int ParcelaId { get; set; }
	public DateTime Fecha { get; set; }
	public decimal? Ph { get; set; }
	public decimal? MateriaOrganica { get; set; }
	public decimal? Nitrogeno { get; set; }
	public decimal? Fosforo { get; set; }
	public decimal? Potasio { get; set; }
	public string? Observacion { get; set; }

	public Parcela Parcela { get; set; } = null!;
}
