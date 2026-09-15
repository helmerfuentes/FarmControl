using FarmControlAPI.Domain.Enums;

namespace FarmControlAPI.Domain.Entities;

public class Compra
{
	public int Id { get; set; }
	public int FincaId { get; set; }
	public int SocioId { get; set; }
	public int? ParcelaId { get; set; }
	public int? ProcesoCultivoId { get; set; }
	public string Descripcion { get; set; } = string.Empty;
	public decimal Valor { get; set; }
	public DateTime Fecha { get; set; }
	public TipoCompra TipoCompra { get; set; }
	public string? AdjuntoUrl { get; set; }
	public string? Proveedor { get; set; }

	public Finca Finca { get; set; } = null!;
	public Persona Socio { get; set; } = null!;
	public Parcela? Parcela { get; set; }
	public ProcesoCultivo? ProcesoCultivo { get; set; }
	public ICollection<PagoCompra> Pagos { get; set; } = [];
}
