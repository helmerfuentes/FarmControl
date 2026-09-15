using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Queries;

public record FincaConAccesoDto(int Id, string Nombre, string? Ubicacion, int CantidadUsuarios);

public record ClienteDetalleDto(
	int Id,
	string RazonSocial,
	string NIT,
	string Email,
	string? Telefono,
	bool Activo,
	DateTime FechaAlta,
	DateTime? FechaVencimientoContrato,
	List<FincaConAccesoDto> Fincas,
	int? PlanId,
	string? PlanNombre,
	int? MaxFincas,
	int? MaxUsuarios,
	int NumUsuarios);

public record GetClienteDetalleQuery(int Id) : IRequest<Result<ClienteDetalleDto>>;

public class GetClienteDetalleQueryHandler : IRequestHandler<GetClienteDetalleQuery, Result<ClienteDetalleDto>>
{
	private readonly IFarmControlDbContext _context;

	public GetClienteDetalleQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<ClienteDetalleDto>> Handle(GetClienteDetalleQuery request, CancellationToken cancellationToken)
	{
		Result<ClienteDetalleDto> result;

		var cliente = await _context.Clientes
			.Include(c => c.Plan)
			.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

		if (cliente is null)
		{
			result = Result<ClienteDetalleDto>.Failure($"Cliente con Id {request.Id} no encontrado.");
			return result;
		}

		var fincas = await _context.Fincas
			.Where(f => f.ClienteId == request.Id)
			.OrderBy(f => f.Nombre)
			.Select(f => new FincaConAccesoDto(f.Id, f.Nombre, f.Ubicacion, f.AsignacionesPersona.Count()))
			.ToListAsync(cancellationToken);

		var numUsuarios = await _context.Personas.CountAsync(p => p.ClienteId == request.Id && p.NombreUsuario != null, cancellationToken);

		result = Result<ClienteDetalleDto>.Success(new ClienteDetalleDto(
			cliente.Id, cliente.RazonSocial, cliente.NIT, cliente.Email, cliente.Telefono, cliente.Activo, cliente.FechaAlta, cliente.FechaVencimientoContrato, fincas,
			cliente.PlanId, cliente.Plan?.Nombre, cliente.Plan?.MaxFincas, cliente.Plan?.MaxUsuarios, numUsuarios));
		return result;
	}
}
