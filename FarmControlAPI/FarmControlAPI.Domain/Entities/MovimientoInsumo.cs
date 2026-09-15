using FarmControlAPI.Domain.Enums;

namespace FarmControlAPI.Domain.Entities;

public class MovimientoInsumo
{
	public int Id { get; set; }
	public int InsumoId { get; set; }
	public int ParcelaId { get; set; }
	public decimal Cantidad { get; set; }
	public TipoMovimientoInsumo TipoMovimiento { get; set; }
	public DateTime Fecha { get; set; }
	public string? Observacion { get; set; }
	public decimal? PrecioUnitario { get; set; }

	public Insumo Insumo { get; set; } = null!;
	public Parcela Parcela { get; set; } = null!;
}
