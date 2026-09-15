using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Infrastructure.Persistence;

public class FarmControlDbContext : DbContext, IFarmControlDbContext
{
	private readonly ICurrentUserContext _currentUser;

	public FarmControlDbContext(DbContextOptions<FarmControlDbContext> options, ICurrentUserContext currentUser) : base(options)
	{
		_currentUser = currentUser;
	}

	public DbSet<Cliente> Clientes => Set<Cliente>();
	public DbSet<PersonaFinca> PersonasFincas => Set<PersonaFinca>();
	public DbSet<Persona> Personas => Set<Persona>();
	public DbSet<TipoInsumo> TiposInsumo => Set<TipoInsumo>();
	public DbSet<Producto> Productos => Set<Producto>();
	public DbSet<ClasificacionProducto> ClasificacionesProducto => Set<ClasificacionProducto>();
	public DbSet<Finca> Fincas => Set<Finca>();
	public DbSet<Parcela> Parcelas => Set<Parcela>();
	public DbSet<Insumo> Insumos => Set<Insumo>();
	public DbSet<MovimientoInsumo> MovimientosInsumo => Set<MovimientoInsumo>();
	public DbSet<Actividad> Actividades => Set<Actividad>();
	public DbSet<RegistroManoObra> RegistrosManoObra => Set<RegistroManoObra>();
	public DbSet<Compra> Compras => Set<Compra>();
	public DbSet<Venta> Ventas => Set<Venta>();
	public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
	public DbSet<ProcesoCultivo> ProcesosCultivo => Set<ProcesoCultivo>();
	public DbSet<HistorialPrecioVenta> HistorialPreciosVenta => Set<HistorialPrecioVenta>();
	public DbSet<BitacoraEntry> BitacoraEntries => Set<BitacoraEntry>();
	public DbSet<TareaRecurrente> TareasRecurrentes => Set<TareaRecurrente>();
	public DbSet<PagoVenta> PagosVenta => Set<PagoVenta>();
	public DbSet<ReporteCompartido> ReportesCompartidos => Set<ReporteCompartido>();
	public DbSet<PagoCompra> PagosCompra => Set<PagoCompra>();
	public DbSet<Asistencia> Asistencias => Set<Asistencia>();
	public DbSet<LiquidacionNomina> LiquidacionesNomina => Set<LiquidacionNomina>();
	public DbSet<AnalisisSuelo> AnalisisSuelo => Set<AnalisisSuelo>();
	public DbSet<Plan> Planes => Set<Plan>();
	public DbSet<Comentario> Comentarios => Set<Comentario>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(FarmControlDbContext).Assembly);
		AplicarFiltrosDeTenant(modelBuilder);

		modelBuilder.Entity<Comentario>()
			.HasOne(c => c.Autor)
			.WithMany()
			.HasForeignKey(c => c.AutorPersonaId);
	}

	private void AplicarFiltrosDeTenant(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Finca>().HasQueryFilter(f =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(f.Id));

		modelBuilder.Entity<Parcela>().HasQueryFilter(p =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(p.FincaId));

		modelBuilder.Entity<Compra>().HasQueryFilter(c =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(c.FincaId));

		modelBuilder.Entity<Actividad>().HasQueryFilter(a =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(a.Parcela.FincaId));

		modelBuilder.Entity<Venta>().HasQueryFilter(v =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(v.Parcela.FincaId));

		modelBuilder.Entity<MovimientoInsumo>().HasQueryFilter(m =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(m.Parcela.FincaId));

		modelBuilder.Entity<ProcesoCultivo>().HasQueryFilter(pc =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(pc.Parcela.FincaId));

		modelBuilder.Entity<RegistroManoObra>().HasQueryFilter(r =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(r.Actividad.Parcela.FincaId));

		modelBuilder.Entity<DetalleVenta>().HasQueryFilter(d =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(d.Venta.Parcela.FincaId));

		modelBuilder.Entity<HistorialPrecioVenta>().HasQueryFilter(h =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(h.Venta.Parcela.FincaId));

		modelBuilder.Entity<Producto>().HasQueryFilter(p =>
			_currentUser.TieneAccesoGlobal || p.ClienteId == _currentUser.ClienteId);

		modelBuilder.Entity<TipoInsumo>().HasQueryFilter(t =>
			_currentUser.TieneAccesoGlobal || t.ClienteId == _currentUser.ClienteId);

		modelBuilder.Entity<Insumo>().HasQueryFilter(i =>
			_currentUser.TieneAccesoGlobal || i.TipoInsumo.ClienteId == _currentUser.ClienteId);

		modelBuilder.Entity<ClasificacionProducto>().HasQueryFilter(cp =>
			_currentUser.TieneAccesoGlobal || cp.Producto.ClienteId == _currentUser.ClienteId);

		modelBuilder.Entity<Persona>().HasQueryFilter(per =>
			_currentUser.TieneAccesoGlobal || per.ClienteId == _currentUser.ClienteId);

		modelBuilder.Entity<BitacoraEntry>().HasQueryFilter(b =>
			_currentUser.TieneAccesoGlobal || b.ClienteId == _currentUser.ClienteId);

		modelBuilder.Entity<TareaRecurrente>().HasQueryFilter(t =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(t.Parcela.FincaId));

		modelBuilder.Entity<PagoVenta>().HasQueryFilter(p =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(p.Venta.Parcela.FincaId));

		modelBuilder.Entity<PagoCompra>().HasQueryFilter(p =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(p.Compra.FincaId));

		modelBuilder.Entity<Asistencia>().HasQueryFilter(a =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(a.FincaId));

		modelBuilder.Entity<LiquidacionNomina>().HasQueryFilter(l =>
			_currentUser.TieneAccesoGlobal || l.Jornalero.ClienteId == _currentUser.ClienteId);

		modelBuilder.Entity<AnalisisSuelo>().HasQueryFilter(a =>
			_currentUser.TieneAccesoGlobal || _currentUser.FincaIds.Contains(a.Parcela.FincaId));
	}
}
