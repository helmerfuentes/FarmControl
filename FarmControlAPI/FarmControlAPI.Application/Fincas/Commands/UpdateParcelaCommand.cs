using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Fincas.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Fincas.Commands;

public record UpdateParcelaCommand(int Id, int FincaId, string Nombre, decimal Area) : IRequest<Result<ParcelaResumenDto>>;

public class UpdateParcelaCommandHandler : IRequestHandler<UpdateParcelaCommand, Result<ParcelaResumenDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public UpdateParcelaCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ParcelaResumenDto>> Handle(UpdateParcelaCommand request, CancellationToken cancellationToken)
	{
		Result<ParcelaResumenDto> result;

		var parcela = await _context.Parcelas
			.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

		if (parcela is null)
		{
			result = Result<ParcelaResumenDto>.Failure($"Parcela con Id {request.Id} no encontrada.");
			return result;
		}

		if (!_currentUser.PuedeEscribirEnFinca(parcela.FincaId) || !_currentUser.PuedeEscribirEnFinca(request.FincaId))
		{
			result = Result<ParcelaResumenDto>.Failure("Esta finca está en modo solo lectura.");
			return result;
		}

		parcela.FincaId = request.FincaId;
		parcela.Nombre = request.Nombre;
		parcela.Area = request.Area;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Parcela actualizada",
			$"Se actualizó la parcela '{parcela.Nombre}'.",
			cancellationToken: cancellationToken);

		result = Result<ParcelaResumenDto>.Success(new ParcelaResumenDto(parcela.Id, parcela.Nombre, parcela.Area));
		return result;
	}
}
