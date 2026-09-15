using FarmControlAPI.Application.Comentarios.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Xunit;

namespace FarmControlAPI.Tests.Comentarios.Queries;

public class GetComentariosQueryHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Actividad actividad) SeedConComentarios(
		FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var autor = new Persona { ClienteId = cliente.Id, Nombre = "Autor 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Admin };
		ctx.Personas.Add(autor);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		ctx.SaveChanges();
		var actividad = new Actividad { ParcelaId = parcela.Id, TipoActividad = "Fumigación", FechaInicio = DateTime.UtcNow, ValorDiaUsado = 1, Descripcion = "x" };
		ctx.Actividades.Add(actividad);
		ctx.SaveChanges();

		ctx.Comentarios.AddRange(
			new Comentario { TipoEntidad = TipoEntidadComentario.Actividad, EntidadId = actividad.Id, AutorPersonaId = autor.Id, Texto = "Primero", Fecha = new DateTime(2026, 1, 1) },
			new Comentario { TipoEntidad = TipoEntidadComentario.Actividad, EntidadId = actividad.Id, AutorPersonaId = autor.Id, Texto = "Segundo", Fecha = new DateTime(2026, 1, 2) });
		ctx.SaveChanges();

		return (ctx, actividad);
	}

	[Fact]
	public async Task Handle_EntidadAccesible_RetornaComentariosOrdenadosPorFecha()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var ctxBase = TestDbContextFactory.Create(currentUser.Object);
		var (ctx, actividad) = SeedConComentarios(ctxBase);
		var handler = new GetComentariosQueryHandler(ctx);

		var result = await handler.Handle(new GetComentariosQuery(TipoEntidadComentario.Actividad, actividad.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(2, result.Value!.Count);
		Assert.Equal("Primero", result.Value[0].Texto);
		Assert.Equal("Segundo", result.Value[1].Texto);
		Assert.Equal("Autor 1", result.Value[0].AutorNombre);
	}

	[Fact]
	public async Task Handle_EntidadFueraDelAlcanceDelTenant_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: [9999]);
		var ctxBase = TestDbContextFactory.Create(currentUser.Object);
		var (ctx, actividad) = SeedConComentarios(ctxBase);
		var handler = new GetComentariosQueryHandler(ctx);

		var result = await handler.Handle(new GetComentariosQuery(TipoEntidadComentario.Actividad, actividad.Id), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
