using FarmControlAPI.Application.Clientes.Queries;
using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Commands;

public record UpdateContratoClienteCommand(int ClienteId, DateTime? FechaVencimientoContrato, bool Activo) : IRequest<Result<ClienteDto>>;

public class UpdateContratoClienteCommandHandler : IRequestHandler<UpdateContratoClienteCommand, Result<ClienteDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public UpdateContratoClienteCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<ClienteDto>> Handle(UpdateContratoClienteCommand request, CancellationToken cancellationToken)
	{
		Result<ClienteDto> result;

		var cliente = await _context.Clientes.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == request.ClienteId, cancellationToken);

		if (cliente is null)
		{
			result = Result<ClienteDto>.Failure($"Cliente con Id {request.ClienteId} no encontrado.");
			return result;
		}

		cliente.FechaVencimientoContrato = request.FechaVencimientoContrato;
		cliente.Activo = request.Activo;

		await _context.SaveChangesAsync(cancellationToken);

		var vencimientoTexto = cliente.FechaVencimientoContrato?.ToString("dd/MM/yyyy") ?? "sin definir";
		await _bitacora.RegistrarAsync(
			"Contrato actualizado",
			$"Vencimiento: {vencimientoTexto}. Cliente {(cliente.Activo ? "activo" : "inactivo — bloquea el inicio de sesión")}.",
			cliente.Id,
			cancellationToken);

		var numFincas = await _context.Fincas.CountAsync(f => f.ClienteId == cliente.Id, cancellationToken);
		var numUsuarios = await _context.Personas.CountAsync(p => p.ClienteId == cliente.Id && p.NombreUsuario != null, cancellationToken);

		result = Result<ClienteDto>.Success(new ClienteDto(
			cliente.Id, cliente.RazonSocial, cliente.NIT, cliente.Email, cliente.Telefono, cliente.Activo, cliente.FechaAlta, cliente.FechaVencimientoContrato,
			cliente.PlanId, cliente.Plan?.Nombre, cliente.Plan?.MaxFincas, cliente.Plan?.MaxUsuarios, numFincas, numUsuarios));
		return result;
	}
}
