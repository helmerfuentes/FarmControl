using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.TiposInsumo.Queries;
using FarmControlAPI.Domain.Entities;
using MediatR;

namespace FarmControlAPI.Application.TiposInsumo.Commands;

public record CreateTipoInsumoCommand(string Nombre, string? Descripcion) : IRequest<Result<TipoInsumoDto>>;

public class CreateTipoInsumoCommandHandler : IRequestHandler<CreateTipoInsumoCommand, Result<TipoInsumoDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateTipoInsumoCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<TipoInsumoDto>> Handle(CreateTipoInsumoCommand request, CancellationToken cancellationToken)
	{
		Result<TipoInsumoDto> result;

		if (_currentUser.ClienteId is null)
		{
			result = Result<TipoInsumoDto>.Failure("El usuario actual no tiene un cliente asociado.");
			return result;
		}

		var tipo = new TipoInsumo { ClienteId = _currentUser.ClienteId.Value, Nombre = request.Nombre, Descripcion = request.Descripcion };
		_context.TiposInsumo.Add(tipo);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Tipo de insumo creado",
			$"Se registró el tipo de insumo '{tipo.Nombre}' en el catálogo.",
			tipo.ClienteId,
			cancellationToken);

		result = Result<TipoInsumoDto>.Success(new TipoInsumoDto(tipo.Id, tipo.Nombre, tipo.Descripcion));
		return result;
	}
}
