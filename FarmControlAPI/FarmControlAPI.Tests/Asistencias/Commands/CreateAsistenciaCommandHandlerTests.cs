using FarmControlAPI.Application.Asistencias.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Asistencias.Commands;

public class CreateAsistenciaCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static (Finca finca, Persona persona) SeedFincaYPersona(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var persona = new Persona { ClienteId = cliente.Id, Nombre = "Jornalero 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		ctx.Personas.Add(persona);
		ctx.SaveChanges();
		return (finca, persona);
	}

	[Fact]
	public async Task Handle_DatosValidos_RegistraLaAsistencia()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var (finca, persona) = SeedFincaYPersona(ctx);
		var handler = new CreateAsistenciaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new CreateAsistenciaCommand(persona.Id, finca.Id, DateTime.UtcNow.Date, new TimeSpan(7, 0, 0), null, "entrada normal"),
			CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(persona.Nombre, result.Value!.PersonaNombre);
		Assert.Equal(finca.Nombre, result.Value.FincaNombre);
	}

	[Fact]
	public async Task Handle_FincaInexistente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var (_, persona) = SeedFincaYPersona(ctx);
		var handler = new CreateAsistenciaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new CreateAsistenciaCommand(persona.Id, 9999, DateTime.UtcNow.Date, null, null, null),
			CancellationToken.None);

		Assert.False(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_PersonaInexistente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var (finca, _) = SeedFincaYPersona(ctx);
		var handler = new CreateAsistenciaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new CreateAsistenciaCommand(9999, finca.Id, DateTime.UtcNow.Date, null, null, null),
			CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
