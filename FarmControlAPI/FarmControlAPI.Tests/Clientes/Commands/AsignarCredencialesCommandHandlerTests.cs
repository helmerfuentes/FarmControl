using FarmControlAPI.Application.Clientes.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Clientes.Commands;

public class AsignarCredencialesCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	[Fact]
	public async Task Handle_PersonaDelMismoCliente_AsignaCredenciales()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: true);
		var (ctx, bitacora) = CreateContext(currentUser);
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var jornalero = new Persona { ClienteId = cliente.Id, Nombre = "Jornalero 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		ctx.Personas.Add(jornalero);
		ctx.SaveChanges();
		var handler = new AsignarCredencialesCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new AsignarCredencialesCommand(
			jornalero.Id, "jornalero1", "clave123", [new AccesoFincaInput(finca.Id, true, false)]), CancellationToken.None);

		Assert.True(result.IsSuccess);
		var actualizado = ctx.Personas.Find(jornalero.Id)!;
		Assert.Equal("jornalero1", actualizado.NombreUsuario);
		Assert.NotNull(actualizado.PasswordHash);
	}

	[Fact]
	public async Task Handle_PersonaDeOtroCliente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: [], clienteId: 1);
		var (ctx, bitacora) = CreateContext(currentUser);
		var clienteA = new Cliente { RazonSocial = "Cliente A", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		var clienteB = new Cliente { RazonSocial = "Cliente B", NIT = "2", Email = "b@b.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.AddRange(clienteA, clienteB);
		ctx.SaveChanges();
		var jornaleroDeOtroCliente = new Persona { ClienteId = clienteB.Id, Nombre = "Jornalero Ajeno", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		ctx.Personas.Add(jornaleroDeOtroCliente);
		ctx.SaveChanges();
		currentUser.SetupGet(m => m.ClienteId).Returns(clienteA.Id);
		var handler = new AsignarCredencialesCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new AsignarCredencialesCommand(
			jornaleroDeOtroCliente.Id, "intento", "clave123", null), CancellationToken.None);

		Assert.False(result.IsSuccess);
		var sinCambios = ctx.Personas.Find(jornaleroDeOtroCliente.Id)!;
		Assert.Null(sinCambios.NombreUsuario);
	}

	[Fact]
	public async Task Handle_ContrasenaCorta_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var jornalero = new Persona { ClienteId = cliente.Id, Nombre = "Jornalero 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		ctx.Personas.Add(jornalero);
		ctx.SaveChanges();
		var handler = new AsignarCredencialesCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new AsignarCredencialesCommand(jornalero.Id, "u1", "123", null), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
