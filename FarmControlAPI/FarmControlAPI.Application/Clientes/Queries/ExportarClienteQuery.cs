using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Queries;

public record ParcelaExportDto(int Id, string Nombre, decimal Area);
public record FincaExportDto(int Id, string Nombre, string? Ubicacion, decimal AreaTotal, decimal CostoTerreno, List<ParcelaExportDto> Parcelas);
public record PersonaExportDto(int Id, string Nombre, string Documento, string Telefono, string? Email, string TipoPersona, decimal ValorDia, string? NombreUsuario, bool Activo);
public record ClasificacionExportDto(int Id, string Nombre, string UnidadMedida, decimal PesoUnidadKg);
public record ProductoExportDto(int Id, string Nombre, List<ClasificacionExportDto> Clasificaciones);
public record TipoInsumoExportDto(int Id, string Nombre, string? Descripcion);
public record InsumoExportDto(int Id, int TipoInsumoId, string Nombre, string? Marca, decimal PrecioUnitario, string UnidadMedida, decimal? StockMinimo, DateTime? FechaVencimiento);
public record ActividadExportDto(int Id, int ParcelaId, string TipoActividad, DateTime FechaInicio, DateTime? FechaFin, string? Descripcion, bool Confirmada);
public record PagoCompraExportDto(int Id, decimal Monto, DateTime Fecha, string? Observacion);
public record CompraExportDto(int Id, int FincaId, int? ParcelaId, string Descripcion, decimal Valor, DateTime Fecha, string TipoCompra, string? Proveedor, List<PagoCompraExportDto> Pagos);
public record DetalleVentaExportDto(int Id, string Clasificacion, decimal Cantidad, string UnidadMedida, decimal PrecioUnitario, decimal Subtotal);
public record PagoVentaExportDto(int Id, decimal Monto, DateTime Fecha, string? Observacion);
public record VentaExportDto(int Id, int ParcelaId, int CompradorId, DateTime Fecha, decimal ValorTransporte, decimal Total, List<DetalleVentaExportDto> Detalles, List<PagoVentaExportDto> Pagos);
public record ProcesoCultivoExportDto(int Id, int ParcelaId, int ProductoId, decimal CostoInicial, DateTime FechaInicio, DateTime? FechaEstimadaCosecha, DateTime? FechaCierre, string Estado);
public record MovimientoInsumoExportDto(int Id, int InsumoId, int ParcelaId, decimal Cantidad, string TipoMovimiento, DateTime Fecha, string? Observacion);
public record RegistroManoObraExportDto(int Id, int ActividadId, int JornaleroId, decimal ValorHora, decimal NumHoras);
public record TareaRecurrenteExportDto(int Id, int ParcelaId, string Descripcion, int FrecuenciaDias, DateTime ProximaFecha, bool Activa);

public record ClienteExportDto(
	int Id, string RazonSocial, string NIT, string Email, string? Telefono, bool Activo, DateTime FechaAlta,
	List<FincaExportDto> Fincas,
	List<PersonaExportDto> Personas,
	List<ProductoExportDto> Productos,
	List<TipoInsumoExportDto> TiposInsumo,
	List<InsumoExportDto> Insumos,
	List<ActividadExportDto> Actividades,
	List<CompraExportDto> Compras,
	List<VentaExportDto> Ventas,
	List<ProcesoCultivoExportDto> ProcesosCultivo,
	List<MovimientoInsumoExportDto> MovimientosInsumo,
	List<RegistroManoObraExportDto> RegistrosManoObra,
	List<TareaRecurrenteExportDto> TareasRecurrentes);

public record ExportarClienteQuery(int ClienteId) : IRequest<Result<ClienteExportDto>>;

/// <summary>
/// Exportación/portabilidad de datos para SuperAdmin: usa IgnoreQueryFilters() porque el
/// llamante (SuperAdmin) no tiene FincaIds propios del cliente exportado — el aislamiento
/// aquí lo da filtrar explícitamente por ClienteId (directo o transitivo vía FincaId), no
/// el query filter de tenant normal. Mismo patrón que GetSaludSistemaQueryHandler.
/// Nunca incluye PasswordHash en la salida.
/// </summary>
public class ExportarClienteQueryHandler : IRequestHandler<ExportarClienteQuery, Result<ClienteExportDto>>
{
	private readonly IFarmControlDbContext _context;

	public ExportarClienteQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<ClienteExportDto>> Handle(ExportarClienteQuery request, CancellationToken cancellationToken)
	{
		Result<ClienteExportDto> result;

		var cliente = await _context.Clientes
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(c => c.Id == request.ClienteId, cancellationToken);

		if (cliente is null)
		{
			result = Result<ClienteExportDto>.Failure("Cliente no encontrado.");
			return result;
		}

		var fincas = await _context.Fincas
			.IgnoreQueryFilters()
			.Where(f => f.ClienteId == request.ClienteId)
			.Include(f => f.Parcelas)
			.ToListAsync(cancellationToken);

		var fincaIds = fincas.Select(f => f.Id).ToList();
		var parcelaIds = fincas.SelectMany(f => f.Parcelas).Select(p => p.Id).ToList();

		var fincasDto = fincas.Select(f => new FincaExportDto(
			f.Id, f.Nombre, f.Ubicacion, f.AreaTotal, f.CostoTerreno,
			f.Parcelas.Select(p => new ParcelaExportDto(p.Id, p.Nombre, p.Area)).ToList())).ToList();

		var personasDto = await _context.Personas
			.IgnoreQueryFilters()
			.Where(p => p.ClienteId == request.ClienteId)
			.Select(p => new PersonaExportDto(p.Id, p.Nombre, p.Documento, p.Telefono, p.Email, p.TipoPersona.ToString(), p.ValorDia, p.NombreUsuario, p.Activo))
			.ToListAsync(cancellationToken);

		var productosDto = await _context.Productos
			.IgnoreQueryFilters()
			.Where(p => p.ClienteId == request.ClienteId)
			.Include(p => p.Clasificaciones)
			.Select(p => new ProductoExportDto(p.Id, p.Nombre,
				p.Clasificaciones.Select(c => new ClasificacionExportDto(c.Id, c.Nombre, c.UnidadMedida.ToString(), c.PesoUnidadKg)).ToList()))
			.ToListAsync(cancellationToken);

		var tiposInsumoDto = await _context.TiposInsumo
			.IgnoreQueryFilters()
			.Where(t => t.ClienteId == request.ClienteId)
			.Select(t => new TipoInsumoExportDto(t.Id, t.Nombre, t.Descripcion))
			.ToListAsync(cancellationToken);

		var tipoInsumoIds = tiposInsumoDto.Select(t => t.Id).ToList();

		var insumosDto = await _context.Insumos
			.IgnoreQueryFilters()
			.Where(i => tipoInsumoIds.Contains(i.TipoInsumoId))
			.Select(i => new InsumoExportDto(i.Id, i.TipoInsumoId, i.Nombre, i.Marca, i.PrecioUnitario, i.UnidadMedida, i.StockMinimo, i.FechaVencimiento))
			.ToListAsync(cancellationToken);

		var actividadesDto = await _context.Actividades
			.IgnoreQueryFilters()
			.Where(a => parcelaIds.Contains(a.ParcelaId))
			.Select(a => new ActividadExportDto(a.Id, a.ParcelaId, a.TipoActividad, a.FechaInicio, a.FechaFin, a.Descripcion, a.Confirmada))
			.ToListAsync(cancellationToken);

		var comprasEntidades = await _context.Compras
			.IgnoreQueryFilters()
			.Where(c => fincaIds.Contains(c.FincaId))
			.Include(c => c.Pagos)
			.ToListAsync(cancellationToken);

		var comprasDto = comprasEntidades.Select(c => new CompraExportDto(
			c.Id, c.FincaId, c.ParcelaId, c.Descripcion, c.Valor, c.Fecha, c.TipoCompra.ToString(), c.Proveedor,
			(c.Pagos ?? []).Select(pg => new PagoCompraExportDto(pg.Id, pg.Monto, pg.Fecha, pg.Observacion)).ToList())).ToList();

		var ventasEntidades = await _context.Ventas
			.IgnoreQueryFilters()
			.Where(v => parcelaIds.Contains(v.ParcelaId))
			.Include(v => v.Detalles)
			.Include(v => v.Pagos)
			.ToListAsync(cancellationToken);

		var ventasDto = ventasEntidades.Select(v => new VentaExportDto(
			v.Id, v.ParcelaId, v.CompradorId, v.Fecha, v.ValorTransporte, v.Total,
			v.Detalles.Select(d => new DetalleVentaExportDto(d.Id, d.Clasificacion, d.Cantidad, d.UnidadMedida.ToString(), d.PrecioUnitario, d.Subtotal)).ToList(),
			(v.Pagos ?? []).Select(pg => new PagoVentaExportDto(pg.Id, pg.Monto, pg.Fecha, pg.Observacion)).ToList())).ToList();

		var procesosDto = await _context.ProcesosCultivo
			.IgnoreQueryFilters()
			.Where(pc => parcelaIds.Contains(pc.ParcelaId))
			.Select(pc => new ProcesoCultivoExportDto(pc.Id, pc.ParcelaId, pc.ProductoId, pc.CostoInicial, pc.FechaInicio, pc.FechaEstimadaCosecha, pc.FechaCierre, pc.Estado.ToString()))
			.ToListAsync(cancellationToken);

		var movimientosDto = await _context.MovimientosInsumo
			.IgnoreQueryFilters()
			.Where(m => parcelaIds.Contains(m.ParcelaId))
			.Select(m => new MovimientoInsumoExportDto(m.Id, m.InsumoId, m.ParcelaId, m.Cantidad, m.TipoMovimiento.ToString(), m.Fecha, m.Observacion))
			.ToListAsync(cancellationToken);

		var registrosDto = await _context.RegistrosManoObra
			.IgnoreQueryFilters()
			.Where(r => r.Actividad.Parcela != null && parcelaIds.Contains(r.Actividad.ParcelaId))
			.Select(r => new RegistroManoObraExportDto(r.Id, r.ActividadId, r.JornaleroId, r.ValorHora, r.NumHoras))
			.ToListAsync(cancellationToken);

		var tareasDto = await _context.TareasRecurrentes
			.IgnoreQueryFilters()
			.Where(t => parcelaIds.Contains(t.ParcelaId))
			.Select(t => new TareaRecurrenteExportDto(t.Id, t.ParcelaId, t.Descripcion, t.FrecuenciaDias, t.ProximaFecha, t.Activa))
			.ToListAsync(cancellationToken);

		var dto = new ClienteExportDto(
			cliente.Id, cliente.RazonSocial, cliente.NIT, cliente.Email, cliente.Telefono, cliente.Activo, cliente.FechaAlta,
			fincasDto, personasDto, productosDto, tiposInsumoDto, insumosDto, actividadesDto,
			comprasDto, ventasDto, procesosDto, movimientosDto, registrosDto, tareasDto);

		result = Result<ClienteExportDto>.Success(dto);
		return result;
	}
}
