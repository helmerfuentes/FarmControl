using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Sistema.Queries;

public record ClienteResumenDto(int ClienteId, string RazonSocial, bool Activo, DateTime FechaAlta, int NumFincas, int NumPersonas);

public record BitacoraResumenDto(DateTime FechaHora, string? ClienteRazonSocial, string ActorNombre, string Accion, string Detalle);

public record SaludSistemaDto(
	int TotalClientes,
	int ClientesActivos,
	int TotalFincas,
	int TotalPersonas,
	int TotalParcelas,
	long TamanoBaseDatosBytes,
	List<ClienteResumenDto> Clientes,
	List<BitacoraResumenDto> BitacoraReciente);

public record GetSaludSistemaQuery : IRequest<Result<SaludSistemaDto>>;

/// <summary>
/// Vista cross-tenant exclusiva de SuperAdmin: usa IgnoreQueryFilters() en todas las
/// consultas porque el propósito mismo del endpoint es ver a través del aislamiento
/// por tenant (conteos globales y las últimas acciones de bitácora de TODOS los clientes).
/// Mismo patrón/justificación que GetReporteCompartidoQueryHandler.
/// </summary>
public class GetSaludSistemaQueryHandler : IRequestHandler<GetSaludSistemaQuery, Result<SaludSistemaDto>>
{
	private const int _MAX_ENTRADAS_BITACORA = 20;

	private readonly IFarmControlDbContext _context;
	private readonly IBackupService _backupService;

	public GetSaludSistemaQueryHandler(IFarmControlDbContext context, IBackupService backupService)
	{
		_context = context;
		_backupService = backupService;
	}

	public async Task<Result<SaludSistemaDto>> Handle(GetSaludSistemaQuery request, CancellationToken cancellationToken)
	{
		var clientes = await _context.Clientes
			.IgnoreQueryFilters()
			.OrderBy(c => c.RazonSocial)
			.ToListAsync(cancellationToken);

		var fincas = await _context.Fincas
			.IgnoreQueryFilters()
			.Select(f => new { f.Id, f.ClienteId })
			.ToListAsync(cancellationToken);

		var personas = await _context.Personas
			.IgnoreQueryFilters()
			.Select(p => new { p.Id, p.ClienteId })
			.ToListAsync(cancellationToken);

		var totalParcelas = await _context.Parcelas.IgnoreQueryFilters().CountAsync(cancellationToken);

		var clienteResumenes = clientes.Select(c => new ClienteResumenDto(
			c.Id, c.RazonSocial, c.Activo, c.FechaAlta,
			fincas.Count(f => f.ClienteId == c.Id),
			personas.Count(p => p.ClienteId == c.Id))).ToList();

		var bitacoraReciente = await _context.BitacoraEntries
			.IgnoreQueryFilters()
			.OrderByDescending(b => b.FechaHora)
			.Take(_MAX_ENTRADAS_BITACORA)
			.Select(b => new { b.FechaHora, b.ClienteId, b.ActorNombre, b.Accion, b.Detalle })
			.ToListAsync(cancellationToken);

		var bitacoraDto = bitacoraReciente.Select(b => new BitacoraResumenDto(
			b.FechaHora,
			clientes.FirstOrDefault(c => c.Id == b.ClienteId)?.RazonSocial,
			b.ActorNombre, b.Accion, b.Detalle)).ToList();

		var dto = new SaludSistemaDto(
			clientes.Count,
			clientes.Count(c => c.Activo),
			fincas.Count,
			personas.Count,
			totalParcelas,
			_backupService.ObtenerTamanoBaseDatosBytes(),
			clienteResumenes,
			bitacoraDto);

		return Result<SaludSistemaDto>.Success(dto);
	}
}
