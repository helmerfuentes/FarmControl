using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Clientes.Queries;

public record AccesoFincaDto(int FincaId, string FincaNombre, bool SoloLectura, bool PuedeEliminar);

public record PersonaDelClienteDto(
	int Id,
	string Nombre,
	TipoPersona TipoPersona,
	string? NombreUsuario,
	bool Activo,
	List<AccesoFincaDto> Fincas);

public record GetPersonasDelClienteQuery(int ClienteId) : IRequest<Result<List<PersonaDelClienteDto>>>;

public class GetPersonasDelClienteQueryHandler : IRequestHandler<GetPersonasDelClienteQuery, Result<List<PersonaDelClienteDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetPersonasDelClienteQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<PersonaDelClienteDto>>> Handle(GetPersonasDelClienteQuery request, CancellationToken cancellationToken)
	{
		Result<List<PersonaDelClienteDto>> result;

		var personas = await _context.Personas
			.Where(p => p.ClienteId == request.ClienteId)
			.OrderBy(p => p.Nombre)
			.Select(p => new PersonaDelClienteDto(
				p.Id,
				p.Nombre,
				p.TipoPersona,
				p.NombreUsuario,
				p.Activo,
				p.AsignacionesFinca
					.Select(pf => new AccesoFincaDto(pf.FincaId, pf.Finca.Nombre, pf.SoloLectura, pf.PuedeEliminar))
					.ToList()))
			.ToListAsync(cancellationToken);

		result = Result<List<PersonaDelClienteDto>>.Success(personas);
		return result;
	}
}
