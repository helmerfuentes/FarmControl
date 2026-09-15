using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Auth;

public record PreferenciasDashboardDto(string? PreferenciasJson);

public record GetPreferenciasDashboardQuery : IRequest<Result<PreferenciasDashboardDto>>;

public class GetPreferenciasDashboardQueryHandler : IRequestHandler<GetPreferenciasDashboardQuery, Result<PreferenciasDashboardDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;

	public GetPreferenciasDashboardQueryHandler(IFarmControlDbContext context, ICurrentUserContext currentUser)
	{
		_context = context;
		_currentUser = currentUser;
	}

	public async Task<Result<PreferenciasDashboardDto>> Handle(GetPreferenciasDashboardQuery request, CancellationToken cancellationToken)
	{
		Result<PreferenciasDashboardDto> result;

		if (_currentUser.PersonaId is null)
		{
			result = Result<PreferenciasDashboardDto>.Success(new PreferenciasDashboardDto(null));
			return result;
		}

		var preferenciasJson = await _context.Personas
			.IgnoreQueryFilters()
			.Where(p => p.Id == _currentUser.PersonaId.Value)
			.Select(p => p.PreferenciasDashboardJson)
			.FirstOrDefaultAsync(cancellationToken);

		result = Result<PreferenciasDashboardDto>.Success(new PreferenciasDashboardDto(preferenciasJson));
		return result;
	}
}
