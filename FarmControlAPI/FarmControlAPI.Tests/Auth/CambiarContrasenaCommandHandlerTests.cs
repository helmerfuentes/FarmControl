using FarmControlAPI.Application.Auth;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Auth;

public class CambiarContrasenaCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora, Persona persona) CreateContext(string contrasenaActual = "clave123")
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: 1);
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var persona = new Persona
		{
			Id = 1,
			ClienteId = cliente.Id,
			Nombre = "Usuario 1",
			Documento = "1",
			Telefono = "1",
			TipoPersona = TipoPersona.Admin,
			NombreUsuario = "usuario1",
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(contrasenaActual)
		};
		ctx.Personas.Add(persona);
		ctx.SaveChanges();

		return (ctx, bitacora, persona);
	}

	[Fact]
	public async Task Handle_ConContrasenaActualCorrecta_ActualizaElHash()
	{
		var (ctx, bitacora, persona) = CreateContext("clave123");
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: persona.Id);
		var handler = new CambiarContrasenaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CambiarContrasenaCommand("clave123", "nuevaClave456"), CancellationToken.None);

		Assert.True(result.IsSuccess);
		var actualizada = await ctx.Personas.FindAsync(persona.Id);
		Assert.True(BCrypt.Net.BCrypt.Verify("nuevaClave456", actualizada!.PasswordHash));
	}

	[Fact]
	public async Task Handle_ConContrasenaActualIncorrecta_RetornaFallo()
	{
		var (ctx, bitacora, persona) = CreateContext("clave123");
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: persona.Id);
		var handler = new CambiarContrasenaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CambiarContrasenaCommand("incorrecta", "nuevaClave456"), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_ConNuevaContrasenaMuyCorta_RetornaFallo()
	{
		var (ctx, bitacora, persona) = CreateContext("clave123");
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: persona.Id);
		var handler = new CambiarContrasenaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CambiarContrasenaCommand("clave123", "abc"), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
