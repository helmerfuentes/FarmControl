using FarmControlAPI.Domain.Enums;

namespace FarmControlAPI.Domain.Entities;

public class ProcesoCultivo
{
	public int Id { get; set; }
	public int ParcelaId { get; set; }
	public int ProductoId { get; set; }
	public int? SocioId { get; set; }
	public decimal CostoInicial { get; set; }
	public DateTime FechaInicio { get; set; }
	public DateTime? FechaEstimadaCosecha { get; set; }
	public DateTime? FechaCierre { get; set; }
	public EstadoProceso Estado { get; set; } = EstadoProceso.Activo;

	public Parcela Parcela { get; set; } = null!;
	public Producto Producto { get; set; } = null!;
	public Persona? Socio { get; set; }
}
