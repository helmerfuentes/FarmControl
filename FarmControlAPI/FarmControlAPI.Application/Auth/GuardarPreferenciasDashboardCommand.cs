using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Auth;

public record GuardarPreferenciasDashboardCommand(string PreferenciasJson) : IRequest<Result<bool>>;

public class GuardarPreferenciasDashboardCommandHandler : IRequestHandler<GuardarPreferenciasDashboardCommand, Result<bool>>
{
	private const int _LONGITUD_MAXIMA = 2000;

	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;

	public GuardarPreferenciasDashboardCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser)
	{
		_context = context;
		_currentUser = currentUser;
	}

	public async Task<Result<bool>> Handle(GuardarPreferenciasDashboardCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		if (_currentUser.PersonaId is null)
		{
			result = Result<bool>.Failure("Esta cuenta no puede guardar preferencias.");
			return result;
		}

		if (request.PreferenciasJson.Length > _LONGITUD_MAXIMA)
		{
			result = Result<bool>.Failure("Las preferencias enviadas son demasiado grandes.");
			return result;
		}

		var persona = await _context.Personas
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(p => p.Id == _currentUser.PersonaId.Value, cancellationToken);

		if (persona is null)
		{
			result = Result<bool>.Failure("No se pudo verificar la cuenta.");
			return result;
		}

		persona.PreferenciasDashboardJson = request.PreferenciasJson;
		await _context.SaveChangesAsync(cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
