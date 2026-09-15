using FarmControlAPI.Application.Asistencias.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Asistencias.Commands;

public class UpdateAsistenciaCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static Asistencia SeedAsistencia(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var persona = new Persona { ClienteId = cliente.Id, Nombre = "Jornalero 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		ctx.Personas.Add(persona);
		ctx.SaveChanges();
		var asistencia = new Asistencia { PersonaId = persona.Id, FincaId = finca.Id, Fecha = DateTime.UtcNow.Date, HoraEntrada = new TimeSpan(7, 0, 0) };
		ctx.Asistencias.Add(asistencia);
		ctx.SaveChanges();
		return asistencia;
	}

	[Fact]
	public async Task Handle_AsistenciaExistente_ActualizaHoraSalidaYObservacion()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var asistencia = SeedAsistencia(ctx);
		var handler = new UpdateAsistenciaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new UpdateAsistenciaCommand(asistencia.Id, asistencia.HoraEntrada, new TimeSpan(16, 0, 0), "jornada completa"),
			CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(new TimeSpan(16, 0, 0), result.Value!.HoraSalida);
		Assert.Equal("jornada completa", result.Value.Observacion);
	}

	[Fact]
	public async Task Handle_AsistenciaInexistente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var handler = new UpdateAsistenciaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new UpdateAsistenciaCommand(9999, null, null, null), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
