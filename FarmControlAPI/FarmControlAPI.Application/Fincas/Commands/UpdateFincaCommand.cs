using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Fincas.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Fincas.Commands;

public record UpdateFincaCommand(
	int Id,
	string Nombre,
	string? Ubicacion,
	decimal AreaTotal,
	decimal CostoTerreno) : IRequest<Result<FincaDto>>;

public class UpdateFincaCommandHandler : IRequestHandler<UpdateFincaCommand, Result<FincaDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public UpdateFincaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<FincaDto>> Handle(UpdateFincaCommand request, CancellationToken cancellationToken)
	{
		Result<FincaDto> result;

		var finca = await _context.Fincas
			.Include(f => f.Parcelas)
			.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

		if (finca is null)
		{
			result = Result<FincaDto>.Failure($"Finca con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(finca.Id))
		{
			result = Result<FincaDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		finca.Nombre = request.Nombre;
		finca.Ubicacion = request.Ubicacion;
		finca.AreaTotal = request.AreaTotal;
		finca.CostoTerreno = request.CostoTerreno;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Finca actualizada",
			$"Se actualizaron los datos de la finca '{finca.Nombre}'.",
			finca.ClienteId,
			cancellationToken);

		var parcelas = finca.Parcelas
			.Select(p => new ParcelaResumenDto(p.Id, p.Nombre, p.Area))
			.ToList();

		result = Result<FincaDto>.Success(new FincaDto(finca.Id, finca.Nombre, finca.Ubicacion, finca.AreaTotal, finca.CostoTerreno, parcelas));
		return result;
	}
}
