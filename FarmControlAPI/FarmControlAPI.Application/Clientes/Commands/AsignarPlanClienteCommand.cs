using FarmControlAPI.Application.Clientes.Queries;
using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Commands;

public record AsignarPlanClienteCommand(int ClienteId, int? PlanId) : IRequest<Result<ClienteDto>>;

public class AsignarPlanClienteCommandHandler : IRequestHandler<AsignarPlanClienteCommand, Result<ClienteDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public AsignarPlanClienteCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<ClienteDto>> Handle(AsignarPlanClienteCommand request, CancellationToken cancellationToken)
	{
		Result<ClienteDto> result;

		var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == request.ClienteId, cancellationToken);
		if (cliente is null)
		{
			result = Result<ClienteDto>.Failure($"Cliente con Id {request.ClienteId} no encontrado.");
			return result;
		}

		string? planNombre = null;
		int? maxFincas = null;
		int? maxUsuarios = null;

		if (request.PlanId is not null)
		{
			var plan = await _context.Planes.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
			if (plan is null)
			{
				result = Result<ClienteDto>.Failure($"Plan con Id {request.PlanId} no encontrado.");
				return result;
			}

			planNombre = plan.Nombre;
			maxFincas = plan.MaxFincas;
			maxUsuarios = plan.MaxUsuarios;
		}

		cliente.PlanId = request.PlanId;
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Plan del cliente actualizado",
			request.PlanId is null ? $"Se quitó el plan del cliente '{cliente.RazonSocial}'." : $"Se asignó el plan '{planNombre}' al cliente '{cliente.RazonSocial}'.",
			cliente.Id,
			cancellationToken);

		var numFincas = await _context.Fincas.CountAsync(f => f.ClienteId == cliente.Id, cancellationToken);
		var numUsuarios = await _context.Personas.CountAsync(p => p.ClienteId == cliente.Id && p.NombreUsuario != null, cancellationToken);

		result = Result<ClienteDto>.Success(new ClienteDto(
			cliente.Id, cliente.RazonSocial, cliente.NIT, cliente.Email, cliente.Telefono, cliente.Activo, cliente.FechaAlta, cliente.FechaVencimientoContrato,
			cliente.PlanId, planNombre, maxFincas, maxUsuarios, numFincas, numUsuarios));
		return result;
	}
}
