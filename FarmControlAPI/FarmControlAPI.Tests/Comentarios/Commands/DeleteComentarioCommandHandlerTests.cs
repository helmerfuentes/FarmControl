using FarmControlAPI.Application.Comentarios.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Comentarios.Commands;

public class DeleteComentarioCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static (Persona autor, Persona otro, Actividad actividad, Comentario comentario) SeedComentario(
		FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var autor = new Persona { ClienteId = cliente.Id, Nombre = "Autor 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		var otro = new Persona { ClienteId = cliente.Id, Nombre = "Otro 1", Documento = "2", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		ctx.Personas.AddRange(autor, otro);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		ctx.SaveChanges();
		var actividad = new Actividad { ParcelaId = parcela.Id, TipoActividad = "Fumigación", FechaInicio = DateTime.UtcNow, ValorDiaUsado = 1, Descripcion = "x" };
		ctx.Actividades.Add(actividad);
		ctx.SaveChanges();
		var comentario = new Comentario { TipoEntidad = TipoEntidadComentario.Actividad, EntidadId = actividad.Id, AutorPersonaId = autor.Id, Texto = "Nota", Fecha = DateTime.UtcNow };
		ctx.Comentarios.Add(comentario);
		ctx.SaveChanges();
		return (autor, otro, actividad, comentario);
	}

	[Fact]
	public async Task Handle_Autor_EliminaSuPropioComentario()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		var (ctx, bitacora) = CreateContext(currentUser);
		var (autor, _, _, comentario) = SeedComentario(ctx);
		currentUser.SetupGet(m => m.PersonaId).Returns(autor.Id);
		currentUser.Setup(m => m.PuedeEliminarEnFinca(It.IsAny<int>())).Returns(false);
		var handler = new DeleteComentarioCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new DeleteComentarioCommand(comentario.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Empty(ctx.Comentarios);
	}

	[Fact]
	public async Task Handle_AdminConAccesoGlobal_EliminaComentarioAjeno()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: true);
		var (ctx, bitacora) = CreateContext(currentUser);
		var (_, otro, _, comentario) = SeedComentario(ctx);
		currentUser.SetupGet(m => m.PersonaId).Returns(otro.Id);
		var handler = new DeleteComentarioCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new DeleteComentarioCommand(comentario.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_UsuarioSinPermisoNiAutoria_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		var (ctx, bitacora) = CreateContext(currentUser);
		var (_, otro, _, comentario) = SeedComentario(ctx);
		currentUser.SetupGet(m => m.PersonaId).Returns(otro.Id);
		currentUser.Setup(m => m.PuedeEliminarEnFinca(It.IsAny<int>())).Returns(false);
		var handler = new DeleteComentarioCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new DeleteComentarioCommand(comentario.Id), CancellationToken.None);

		Assert.False(result.IsSuccess);
		Assert.Single(ctx.Comentarios);
	}
}
