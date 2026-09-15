using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Common;

public interface IFarmControlDbContext
{
	DbSet<Cliente> Clientes { get; }
	DbSet<PersonaFinca> PersonasFincas { get; }
	DbSet<Persona> Personas { get; }
	DbSet<TipoInsumo> TiposInsumo { get; }
	DbSet<Producto> Productos { get; }
	DbSet<ClasificacionProducto> ClasificacionesProducto { get; }
	DbSet<Finca> Fincas { get; }
	DbSet<Parcela> Parcelas { get; }
	DbSet<Insumo> Insumos { get; }
	DbSet<MovimientoInsumo> MovimientosInsumo { get; }
	DbSet<Actividad> Actividades { get; }
	DbSet<RegistroManoObra> RegistrosManoObra { get; }
	DbSet<Compra> Compras { get; }
	DbSet<Venta> Ventas { get; }
	DbSet<DetalleVenta> DetallesVenta { get; }
	DbSet<ProcesoCultivo> ProcesosCultivo { get; }
	DbSet<HistorialPrecioVenta> HistorialPreciosVenta { get; }
	DbSet<BitacoraEntry> BitacoraEntries { get; }
	DbSet<TareaRecurrente> TareasRecurrentes { get; }
	DbSet<PagoVenta> PagosVenta { get; }
	DbSet<ReporteCompartido> ReportesCompartidos { get; }
	DbSet<PagoCompra> PagosCompra { get; }
	DbSet<Asistencia> Asistencias { get; }
	DbSet<LiquidacionNomina> LiquidacionesNomina { get; }
	DbSet<AnalisisSuelo> AnalisisSuelo { get; }
	DbSet<Plan> Planes { get; }
	DbSet<Comentario> Comentarios { get; }

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
