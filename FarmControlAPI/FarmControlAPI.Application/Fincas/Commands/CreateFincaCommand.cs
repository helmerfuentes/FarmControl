using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Fincas.Queries;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Fincas.Commands;

public record CreateFincaCommand(
	string Nombre,
	string? Ubicacion,
	decimal AreaTotal,
	decimal CostoTerreno) : IRequest<Result<FincaDto>>;

public class CreateFincaCommandHandler : IRequestHandler<CreateFincaCommand, Result<FincaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateFincaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<FincaDto>> Handle(CreateFincaCommand request, CancellationToken cancellationToken)
	{
		Result<FincaDto> result;

		if (_currentUser.ClienteId is not int clienteId)
		{
			result = Result<FincaDto>.Failure("No se pudo determinar el cliente del usuario actual.");
			return result;
		}

		var cliente = await _context.Clientes
			.Include(c => c.Plan)
			.FirstOrDefaultAsync(c => c.Id == clienteId, cancellationToken);
		if (cliente is null)
		{
			result = Result<FincaDto>.Failure("Cliente no encontrado.");
			return result;
		}

		if (cliente.Plan is not null && cliente.Plan.MaxFincas != -1)
		{
			var numFincasActuales = await _context.Fincas.CountAsync(f => f.ClienteId == clienteId, cancellationToken);
			if (numFincasActuales >= cliente.Plan.MaxFincas)
			{
				result = Result<FincaDto>.Failure("Se alcanzó el límite de fincas del plan contratado.");
				return result;
			}
		}

		var finca = new Finca
		{
			ClienteId = clienteId,
			Nombre = request.Nombre,
			Ubicacion = request.Ubicacion,
			AreaTotal = request.AreaTotal,
			CostoTerreno = request.CostoTerreno
		};

		if (_currentUser.PersonaId is int personaId)
		{
			finca.AsignacionesPersona.Add(new PersonaFinca
			{
				PersonaId = personaId,
				FechaAsignacion = DateTime.UtcNow,
				SoloLectura = false,
				PuedeEliminar = true
			});
		}

		_context.Fincas.Add(finca);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Finca creada",
			$"Se registró la finca '{finca.Nombre}'.",
			clienteId,
			cancellationToken);

		result = Result<FincaDto>.Success(new FincaDto(finca.Id, finca.Nombre, finca.Ubicacion, finca.AreaTotal, finca.CostoTerreno, []));
		return result;
	}
}
