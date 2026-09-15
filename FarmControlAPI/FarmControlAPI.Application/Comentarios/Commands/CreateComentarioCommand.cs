using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Comentarios.Commands;

public record ComentarioDto(int Id, TipoEntidadComentario TipoEntidad, int EntidadId, int AutorPersonaId, string AutorNombre, string Texto, DateTime Fecha);

public record CreateComentarioCommand(TipoEntidadComentario TipoEntidad, int EntidadId, string Texto) : IRequest<Result<ComentarioDto>>;

public class CreateComentarioCommandHandler : IRequestHandler<CreateComentarioCommand, Result<ComentarioDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly ICurrentUserContext _currentUser;
	private readonly IBitacoraService _bitacora;

	public CreateComentarioCommandHandler(IFarmControlDbContext context, ICurrentUserContext currentUser, IBitacoraService bitacora)
	{
		_context = context;
		_currentUser = currentUser;
		_bitacora = bitacora;
	}

	public async Task<Result<ComentarioDto>> Handle(CreateComentarioCommand request, CancellationToken cancellationToken)
	{
		Result<ComentarioDto> result;

		if (string.IsNullOrWhiteSpace(request.Texto))
		{
			result = Result<ComentarioDto>.Failure("El comentario no puede estar vacío.");
			return result;
		}

		if (_currentUser.PersonaId is null)
		{
			result = Result<ComentarioDto>.Failure("No se pudo identificar al usuario actual.");
			return result;
		}

		var tieneAcceso = await ComentariosAccesoHelper.TieneAccesoAEntidadAsync(_context, request.TipoEntidad, request.EntidadId, cancellationToken);
		if (!tieneAcceso)
		{
			result = Result<ComentarioDto>.Failure("No encontrado o sin acceso.");
			return result;
		}

		var autor = await _context.Personas.FirstOrDefaultAsync(p => p.Id == _currentUser.PersonaId.Value, cancellationToken);
		if (autor is null)
		{
			result = Result<ComentarioDto>.Failure("No se pudo identificar al usuario actual.");
			return result;
		}

		var comentario = new Comentario
		{
			TipoEntidad = request.TipoEntidad,
			EntidadId = request.EntidadId,
			AutorPersonaId = _currentUser.PersonaId.Value,
			Texto = request.Texto,
			Fecha = DateTime.UtcNow
		};

		_context.Comentarios.Add(comentario);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync(
			"Comentario agregado",
			$"'{autor.Nombre}' agregó un comentario en {request.TipoEntidad} #{request.EntidadId}.",
			cancellationToken: cancellationToken);

		result = Result<ComentarioDto>.Success(new ComentarioDto(
			comentario.Id, comentario.TipoEntidad, comentario.EntidadId,
			comentario.AutorPersonaId, autor.Nombre, comentario.Texto, comentario.Fecha));
		return result;
	}
}

internal static class ComentariosAccesoHelper
{
	internal static async Task<bool> TieneAccesoAEntidadAsync(IFarmControlDbContext context, TipoEntidadComentario tipoEntidad, int entidadId, CancellationToken cancellationToken)
	{
		bool existe;

		switch (tipoEntidad)
		{
			case TipoEntidadComentario.Actividad:
				existe = await context.Actividades.AnyAsync(a => a.Id == entidadId, cancellationToken);
				break;
			case TipoEntidadComentario.Compra:
				existe = await context.Compras.AnyAsync(c => c.Id == entidadId, cancellationToken);
				break;
			case TipoEntidadComentario.Venta:
				existe = await context.Ventas.AnyAsync(v => v.Id == entidadId, cancellationToken);
				break;
			default:
				existe = false;
				break;
		}

		return existe;
	}

	internal static async Task<bool> PuedeEliminarEnEntidadAsync(IFarmControlDbContext context, ICurrentUserContext currentUser, TipoEntidadComentario tipoEntidad, int entidadId, CancellationToken cancellationToken)
	{
		bool puede;

		switch (tipoEntidad)
		{
			case TipoEntidadComentario.Actividad:
				var actividad = await context.Actividades.Include(a => a.Parcela).FirstOrDefaultAsync(a => a.Id == entidadId, cancellationToken);
				puede = actividad is not null && currentUser.PuedeEliminarEnFinca(actividad.Parcela.FincaId);
				break;
			case TipoEntidadComentario.Compra:
				var compra = await context.Compras.FirstOrDefaultAsync(c => c.Id == entidadId, cancellationToken);
				puede = compra is not null && currentUser.PuedeEliminarEnFinca(compra.FincaId);
				break;
			case TipoEntidadComentario.Venta:
				var venta = await context.Ventas.Include(v => v.Parcela).FirstOrDefaultAsync(v => v.Id == entidadId, cancellationToken);
				puede = venta is not null && currentUser.PuedeEliminarEnFinca(venta.Parcela.FincaId);
				break;
			default:
				puede = false;
				break;
		}

		return puede;
	}
}
