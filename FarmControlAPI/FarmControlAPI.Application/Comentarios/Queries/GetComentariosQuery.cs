using FarmControlAPI.Application.Comentarios.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Comentarios.Queries;

public record GetComentariosQuery(TipoEntidadComentario TipoEntidad, int EntidadId) : IRequest<Result<List<ComentarioDto>>>;

public class GetComentariosQueryHandler : IRequestHandler<GetComentariosQuery, Result<List<ComentarioDto>>>
{
	private readonly IFarmControlDbContext _context;

	public GetComentariosQueryHandler(IFarmControlDbContext context)
	{
		_context = context;
	}

	public async Task<Result<List<ComentarioDto>>> Handle(GetComentariosQuery request, CancellationToken cancellationToken)
	{
		Result<List<ComentarioDto>> result;

		var tieneAcceso = await ComentariosAccesoHelper.TieneAccesoAEntidadAsync(_context, request.TipoEntidad, request.EntidadId, cancellationToken);
		if (!tieneAcceso)
		{
			result = Result<List<ComentarioDto>>.Failure("No encontrado o sin acceso.");
			return result;
		}

		var comentarios = await _context.Comentarios
			.Include(c => c.Autor)
			.Where(c => c.TipoEntidad == request.TipoEntidad && c.EntidadId == request.EntidadId)
			.OrderBy(c => c.Fecha)
			.Select(c => new ComentarioDto(c.Id, c.TipoEntidad, c.EntidadId, c.AutorPersonaId, c.Autor.Nombre, c.Texto, c.Fecha))
			.ToListAsync(cancellationToken);

		result = Result<List<ComentarioDto>>.Success(comentarios);
		return result;
	}
}
