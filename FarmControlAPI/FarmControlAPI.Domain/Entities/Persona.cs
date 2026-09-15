using FarmControlAPI.Domain.Enums;

namespace FarmControlAPI.Domain.Entities;

public class Persona
{
	public int Id { get; set; }
	public int ClienteId { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public string Documento { get; set; } = string.Empty;
	public string Telefono { get; set; } = string.Empty;
	public string? Email { get; set; }
	public TipoPersona TipoPersona { get; set; }
	public decimal ValorDia { get; set; }
	public string? NombreUsuario { get; set; }
	public string? PasswordHash { get; set; }
	public bool Activo { get; set; } = true;
	public string? PreferenciasDashboardJson { get; set; }

	public Cliente Cliente { get; set; } = null!;
	public ICollection<Actividad> ActividadesACargo { get; set; } = [];
	public ICollection<RegistroManoObra> RegistrosComoJornalero { get; set; } = [];
	public ICollection<RegistroManoObra> RegistrosComoPagador { get; set; } = [];
	public ICollection<Compra> Compras { get; set; } = [];
	public ICollection<Venta> Ventas { get; set; } = [];
	public ICollection<PersonaFinca> AsignacionesFinca { get; set; } = [];
	public ICollection<Asistencia> Asistencias { get; set; } = [];
	public ICollection<LiquidacionNomina> Liquidaciones { get; set; } = [];
}
