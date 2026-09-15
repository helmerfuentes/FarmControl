using FarmControlAPI.Application.Comentarios.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Comentarios.Commands;

public class CreateComentarioCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static (Persona autor, Finca finca, Parcela parcela, Actividad actividad, Compra compra, Venta venta) SeedEscenario(
		FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var autor = new Persona { ClienteId = cliente.Id, Nombre = "Autor 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Admin };
		var comprador = new Persona { ClienteId = cliente.Id, Nombre = "Comprador 1", Documento = "2", Telefono = "1", TipoPersona = TipoPersona.Comprador };
		var socio = new Persona { ClienteId = cliente.Id, Nombre = "Socio 1", Documento = "3", Telefono = "1", TipoPersona = TipoPersona.Socio };
		ctx.Personas.AddRange(autor, comprador, socio);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		ctx.SaveChanges();

		var actividad = new Actividad { ParcelaId = parcela.Id, TipoActividad = "Fumigación", FechaInicio = DateTime.UtcNow, ValorDiaUsado = 1, Descripcion = "x" };
		var compra = new Compra { FincaId = finca.Id, SocioId = socio.Id, Descripcion = "Insumo", Valor = 100, Fecha = DateTime.UtcNow, TipoCompra = TipoCompra.Insumo, AdjuntoUrl = "" };
		var venta = new Venta { ParcelaId = parcela.Id, CompradorId = comprador.Id, Fecha = DateTime.UtcNow, Total = 100 };
		ctx.Actividades.Add(actividad);
		ctx.Compras.Add(compra);
		ctx.Ventas.Add(venta);
		ctx.SaveChanges();

		return (autor, finca, parcela, actividad, compra, venta);
	}

	[Theory]
	[InlineData(TipoEntidadComentario.Actividad)]
	[InlineData(TipoEntidadComentario.Compra)]
	[InlineData(TipoEntidadComentario.Venta)]
	public async Task Handle_EntidadAccesible_CreaElComentario(TipoEntidadComentario tipo)
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: 100);
		var (ctx, bitacora) = CreateContext(currentUser);
		var (autor, _, _, actividad, compra, venta) = SeedEscenario(ctx);
		// El mock de currentUser usa personaId=100 por defecto en su firma; forzamos que apunte al autor sembrado.
		currentUser.SetupGet(m => m.PersonaId).Returns(autor.Id);
		var handler = new CreateComentarioCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var entidadId = tipo switch
		{
			TipoEntidadComentario.Actividad => actividad.Id,
			TipoEntidadComentario.Compra => compra.Id,
			TipoEntidadComentario.Venta => venta.Id,
			_ => throw new ArgumentOutOfRangeException(nameof(tipo)),
		};

		var result = await handler.Handle(new CreateComentarioCommand(tipo, entidadId, "Nota de seguimiento"), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(autor.Nombre, result.Value!.AutorNombre);
		Assert.Equal("Nota de seguimiento", result.Value.Texto);
	}

	[Fact]
	public async Task Handle_ActividadFueraDelAlcanceDelTenant_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: [9999], personaId: 100);
		var (ctx, bitacora) = CreateContext(currentUser);
		var (autor, _, _, actividad, _, _) = SeedEscenario(ctx);
		currentUser.SetupGet(m => m.PersonaId).Returns(autor.Id);
		var handler = new CreateComentarioCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CreateComentarioCommand(TipoEntidadComentario.Actividad, actividad.Id, "No debería poder"), CancellationToken.None);

		Assert.False(result.IsSuccess);
		Assert.Empty(ctx.Comentarios);
	}

	[Fact]
	public async Task Handle_TextoVacio_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: 100);
		var (ctx, bitacora) = CreateContext(currentUser);
		var (autor, _, _, actividad, _, _) = SeedEscenario(ctx);
		currentUser.SetupGet(m => m.PersonaId).Returns(autor.Id);
		var handler = new CreateComentarioCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CreateComentarioCommand(TipoEntidadComentario.Actividad, actividad.Id, "   "), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
