namespace FarmControlAPI.Domain.Entities;

public class Insumo
{
	public int Id { get; set; }
	public int TipoInsumoId { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public string? Marca { get; set; }
	public string? Descripcion { get; set; }
	public decimal PrecioUnitario { get; set; }
	public string UnidadMedida { get; set; } = string.Empty;
	public decimal? StockMinimo { get; set; }
	public DateTime? FechaVencimiento { get; set; }

	public TipoInsumo TipoInsumo { get; set; } = null!;
	public ICollection<MovimientoInsumo> Movimientos { get; set; } = [];
}
