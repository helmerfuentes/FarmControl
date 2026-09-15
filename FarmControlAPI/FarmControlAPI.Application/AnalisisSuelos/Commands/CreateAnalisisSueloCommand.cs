using FarmControlAPI.Application.AnalisisSuelos.Queries;
using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.AnalisisSuelos.Commands;

public record CreateAnalisisSueloCommand(
	int ParcelaId,
	DateTime Fecha,
	decimal? Ph,
	decimal? MateriaOrganica,
	decimal? Nitrogeno,
	decimal? Fosforo,
	decimal? Potasio,
	string? Observacion) : IRequest<Result<AnalisisSueloDto>>;

public class CreateAnalisisSueloCommandHandler : IRequestHandler<CreateAnalisisSueloCommand, Result<AnalisisSueloDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateAnalisisSueloCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<AnalisisSueloDto>> Handle(CreateAnalisisSueloCommand request, CancellationToken cancellationToken)
	{
		Result<AnalisisSueloDto> result;

		var parcela = await _context.Parcelas.FirstOrDefaultAsync(p => p.Id == request.ParcelaId, cancellationToken);
		if (parcela is null)
		{
			result = Result<AnalisisSueloDto>.Failure($"Parcela con Id {request.ParcelaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(parcela.FincaId))
		{
			result = Result<AnalisisSueloDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var analisis = new Domain.Entities.AnalisisSuelo
		{
			ParcelaId = request.ParcelaId,
			Fecha = request.Fecha,
			Ph = request.Ph,
			MateriaOrganica = request.MateriaOrganica,
			Nitrogeno = request.Nitrogeno,
			Fosforo = request.Fosforo,
			Potasio = request.Potasio,
			Observacion = request.Observacion
		};

		_context.AnalisisSuelo.Add(analisis);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Análisis de suelo registrado",
			$"Se registró un análisis de suelo del {analisis.Fecha:yyyy-MM-dd} en la parcela '{parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<AnalisisSueloDto>.Success(new AnalisisSueloDto(
			analisis.Id, analisis.ParcelaId, analisis.Fecha, analisis.Ph, analisis.MateriaOrganica,
			analisis.Nitrogeno, analisis.Fosforo, analisis.Potasio, analisis.Observacion));
		return result;
	}
}
