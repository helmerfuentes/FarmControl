using FarmControlAPI.Application.Clientes.Queries;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Commands;

public record CreateClienteCommand(string RazonSocial, string NIT, string Email, string? Telefono, DateTime? FechaVencimientoContrato, int? PlanId = null) : IRequest<Result<ClienteDto>>;

public class CreateClienteCommandHandler : IRequestHandler<CreateClienteCommand, Result<ClienteDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public CreateClienteCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<ClienteDto>> Handle(CreateClienteCommand request, CancellationToken cancellationToken)
	{
		Result<ClienteDto> result;

		var nitEnUso = await _context.Clientes.AnyAsync(c => c.NIT == request.NIT, cancellationToken);
		if (nitEnUso)
		{
			result = Result<ClienteDto>.Failure($"Ya existe un cliente con NIT {request.NIT}.");
			return result;
		}

		if (request.PlanId is not null)
		{
			var planExiste = await _context.Planes.AnyAsync(p => p.Id == request.PlanId, cancellationToken);
			if (!planExiste)
			{
				result = Result<ClienteDto>.Failure($"Plan con Id {request.PlanId} no encontrado.");
				return result;
			}
		}

		var cliente = new Cliente
		{
			RazonSocial = request.RazonSocial,
			NIT = request.NIT,
			Email = request.Email,
			Telefono = request.Telefono,
			Activo = true,
			FechaAlta = DateTime.UtcNow,
			FechaVencimientoContrato = request.FechaVencimientoContrato,
			PlanId = request.PlanId
		};

		_context.Clientes.Add(cliente);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync("Cliente creado", $"Se registró el cliente '{cliente.RazonSocial}' (NIT {cliente.NIT}).", cliente.Id, cancellationToken);

		result = Result<ClienteDto>.Success(new ClienteDto(
			cliente.Id, cliente.RazonSocial, cliente.NIT, cliente.Email, cliente.Telefono, cliente.Activo, cliente.FechaAlta, cliente.FechaVencimientoContrato,
			cliente.PlanId, null, null, null, 0, 0));
		return result;
	}
}
