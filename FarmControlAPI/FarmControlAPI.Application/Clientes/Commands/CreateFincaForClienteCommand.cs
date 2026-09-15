using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Fincas.Queries;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Commands;

public record CreateFincaForClienteCommand(
	int ClienteId,
	string Nombre,
	string? Ubicacion,
	decimal AreaTotal,
	decimal CostoTerreno) : IRequest<Result<FincaDto>>;

public class CreateFincaForClienteCommandHandler : IRequestHandler<CreateFincaForClienteCommand, Result<FincaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public CreateFincaForClienteCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<FincaDto>> Handle(CreateFincaForClienteCommand request, CancellationToken cancellationToken)
	{
		Result<FincaDto> result;

		var cliente = await _context.Clientes
			.Include(c => c.Plan)
			.FirstOrDefaultAsync(c => c.Id == request.ClienteId, cancellationToken);
		if (cliente is null)
		{
			result = Result<FincaDto>.Failure($"Cliente con Id {request.ClienteId} no encontrado.");
			return result;
		}

		if (cliente.Plan is not null && cliente.Plan.MaxFincas != -1)
		{
			var numFincasActuales = await _context.Fincas.CountAsync(f => f.ClienteId == request.ClienteId, cancellationToken);
			if (numFincasActuales >= cliente.Plan.MaxFincas)
			{
				result = Result<FincaDto>.Failure("Se alcanzó el límite de fincas del plan contratado.");
				return result;
			}
		}

		var finca = new Finca
		{
			ClienteId = request.ClienteId,
			Nombre = request.Nombre,
			Ubicacion = request.Ubicacion,
			AreaTotal = request.AreaTotal,
			CostoTerreno = request.CostoTerreno
		};

		_context.Fincas.Add(finca);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Finca creada",
			$"Se registró la finca '{finca.Nombre}' para el cliente.",
			finca.ClienteId,
			cancellationToken);

		result = Result<FincaDto>.Success(new FincaDto(finca.Id, finca.Nombre, finca.Ubicacion, finca.AreaTotal, finca.CostoTerreno, []));
		return result;
	}
}
