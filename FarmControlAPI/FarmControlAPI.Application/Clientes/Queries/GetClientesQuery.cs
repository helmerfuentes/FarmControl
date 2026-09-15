using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Queries;

public record ClienteDto(
	int Id, string RazonSocial, string NIT, string Email, string? Telefono, bool Activo, DateTime FechaAlta, DateTime? FechaVencimientoContrato,
	int? PlanId, string? PlanNombre, int? MaxFincas, int? MaxUsuarios, int NumFincas, int NumUsuarios);

public record GetClientesQuery : IRequest<Result<List<ClienteDto>>>;

public class GetClientesQueryHandler : IRequestHandler<GetClientesQuery, Result<List<ClienteDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetClientesQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<ClienteDto>>> Handle(GetClientesQuery request, CancellationToken cancellationToken)
	{
		Result<List<ClienteDto>> result;

		var clientes = await _context.Clientes
			.OrderBy(c => c.RazonSocial)
			.Select(c => new ClienteDto(
				c.Id, c.RazonSocial, c.NIT, c.Email, c.Telefono, c.Activo, c.FechaAlta, c.FechaVencimientoContrato,
				c.PlanId, c.Plan == null ? null : c.Plan.Nombre, c.Plan == null ? null : c.Plan.MaxFincas, c.Plan == null ? null : c.Plan.MaxUsuarios,
				c.Fincas.Count,
				_context.Personas.Count(p => p.ClienteId == c.Id && p.NombreUsuario != null)))
			.ToListAsync(cancellationToken);

		result = Result<List<ClienteDto>>.Success(clientes);
		return result;
	}
}
