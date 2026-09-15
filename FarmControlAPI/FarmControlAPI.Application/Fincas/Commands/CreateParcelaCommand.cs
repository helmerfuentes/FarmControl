using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Fincas.Queries;
using FarmControlAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Fincas.Commands;

public record CreateParcelaCommand(int FincaId, string Nombre, decimal Area) : IRequest<Result<ParcelaResumenDto>>;

public class CreateParcelaCommandHandler : IRequestHandler<CreateParcelaCommand, Result<ParcelaResumenDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateParcelaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ParcelaResumenDto>> Handle(CreateParcelaCommand request, CancellationToken cancellationToken)
	{
		Result<ParcelaResumenDto> result;

		var fincaExiste = await _context.Fincas.AnyAsync(f => f.Id == request.FincaId, cancellationToken);
		if (!fincaExiste)
		{
			result = Result<ParcelaResumenDto>.Failure($"Finca con Id {request.FincaId} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(request.FincaId))
		{
			result = Result<ParcelaResumenDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		var parcela = new Parcela
		{
			FincaId = request.FincaId,
			Nombre = request.Nombre,
			Area = request.Area
		};

		_context.Parcelas.Add(parcela);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Parcela creada",
			$"Se registró la parcela '{parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<ParcelaResumenDto>.Success(new ParcelaResumenDto(parcela.Id, parcela.Nombre, parcela.Area));
		return result;
	}
}
