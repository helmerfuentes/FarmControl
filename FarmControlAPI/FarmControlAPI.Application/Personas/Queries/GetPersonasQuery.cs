using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Personas.DTOs;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Personas.Queries;

public record GetPersonasQuery(TipoPersona? TipoPersona) : IRequest<Result<List<PersonaDto>>>;

public class GetPersonasQueryHandler : IRequestHandler<GetPersonasQuery, Result<List<PersonaDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetPersonasQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<PersonaDto>>> Handle(GetPersonasQuery request, CancellationToken cancellationToken)
	{
		Result<List<PersonaDto>> result;

		var query = _context.Personas.AsQueryable();

		if (request.TipoPersona.HasValue)
		{
			query = query.Where(p => p.TipoPersona == request.TipoPersona.Value);
		}

		var personas = await query
			.OrderBy(p => p.Nombre)
			.Select(p => new PersonaDto
			{
				Id = p.Id,
				Nombre = p.Nombre,
				Documento = p.Documento,
				Telefono = p.Telefono,
				Email = p.Email,
				TipoPersona = p.TipoPersona,
				ValorDia = p.ValorDia
			})
			.ToListAsync(cancellationToken);

		result = Result<List<PersonaDto>>.Success(personas);
		return result;
	}
}
