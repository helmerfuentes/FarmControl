namespace FarmControlAPI.Domain.Entities;

public class Venta
{
	public int Id { get; set; }
	public int ParcelaId { get; set; }
	public int CompradorId { get; set; }
	public DateTime Fecha { get; set; }
	public decimal ValorTransporte { get; set; }
	public decimal Total { get; set; }

	public int? ProcesoCultivoId { get; set; }

	public Parcela Parcela { get; set; } = null!;
	public Persona Comprador { get; set; } = null!;
	public ProcesoCultivo? ProcesoCultivo { get; set; }
	public ICollection<DetalleVenta> Detalles { get; set; } = [];
	public ICollection<PagoVenta> Pagos { get; set; } = [];
}
