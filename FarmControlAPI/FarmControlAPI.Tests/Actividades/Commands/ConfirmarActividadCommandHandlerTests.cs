using FarmControlAPI.Application.Actividades.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Actividades.Commands;

public class ConfirmarActividadCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static Actividad SeedActividad(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		ctx.SaveChanges();
		var actividad = new Actividad
		{
			ParcelaId = parcela.Id,
			TipoActividad = "Fumigación",
			FechaInicio = DateTime.UtcNow.AddHours(-8),
			FechaFin = DateTime.UtcNow,
			ValorDiaUsado = 50000,
			Descripcion = "Aplicación de fungicida",
			Confirmada = false
		};
		ctx.Actividades.Add(actividad);
		ctx.SaveChanges();
		return actividad;
	}

	[Fact]
	public async Task Handle_ConDatosValidos_ConfirmaLaActividad()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var actividad = SeedActividad(ctx);
		var handler = new ConfirmarActividadCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new ConfirmarActividadCommand(actividad.Id, "Juan Pérez"), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.True(result.Value!.Confirmada);
		Assert.Equal("Juan Pérez", result.Value.ConfirmadaPor);
		Assert.NotNull(result.Value.FechaConfirmacion);
	}

	[Fact]
	public async Task Handle_SinConfirmadaPor_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var actividad = SeedActividad(ctx);
		var handler = new ConfirmarActividadCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new ConfirmarActividadCommand(actividad.Id, "   "), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_ActividadInexistente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var handler = new ConfirmarActividadCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new ConfirmarActividadCommand(999, "Juan Pérez"), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}

	private static Actividad SeedActividadAsignadaAJornalero(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, out int jornaleroId, out int otroUsuarioId, out int fincaId)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var jornalero = new Persona { ClienteId = cliente.Id, Nombre = "Jornalero Asignado", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		var otroUsuario = new Persona { ClienteId = cliente.Id, Nombre = "Otro Usuario", Documento = "2", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		ctx.Personas.AddRange(jornalero, otroUsuario);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		ctx.SaveChanges();
		var actividad = new Actividad
		{
			ParcelaId = parcela.Id,
			TipoActividad = "Fumigación",
			FechaInicio = DateTime.UtcNow.AddHours(-8),
			FechaFin = DateTime.UtcNow,
			ValorDiaUsado = 50000,
			Descripcion = "Aplicación de fungicida",
			Confirmada = false,
			PersonaACargoId = jornalero.Id
		};
		ctx.Actividades.Add(actividad);
		ctx.SaveChanges();
		jornaleroId = jornalero.Id;
		otroUsuarioId = otroUsuario.Id;
		fincaId = finca.Id;
		return actividad;
	}

	[Fact]
	public async Task Handle_JornaleroAsignadoConfirmaSuPropiaActividad_TieneExito()
	{
		// Seguridad: el jornalero NO es admin (PuedeEscribirEnFinca=false) — solo debe poder confirmar porque es el
		// responsable asignado (PersonaACargoId), no por ningún otro permiso. Tiene acceso de lectura a su propia
		// finca (como cualquier usuario con credenciales otorgadas) para que el filtro de tenant no la oculte.
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		currentUser.Setup(m => m.PuedeEscribirEnFinca(It.IsAny<int>())).Returns(false);
		var (ctx, bitacora) = CreateContext(currentUser);
		var actividad = SeedActividadAsignadaAJornalero(ctx, out var jornaleroId, out _, out var fincaId);
		currentUser.SetupGet(m => m.PersonaId).Returns(jornaleroId);
		currentUser.SetupGet(m => m.FincaIds).Returns([fincaId]);
		var handler = new ConfirmarActividadCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new ConfirmarActividadCommand(actividad.Id, "Jornalero Asignado"), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.True(result.Value!.Confirmada);
	}

	[Fact]
	public async Task Handle_UsuarioNoAsignadoYSinPermisoDeEscritura_NoPuedeConfirmarActividadAjena()
	{
		// Prueba crítica de seguridad: la policy del endpoint se relajó de "Admin" a "cualquier autenticado";
		// este chequeo dentro del handler es la única barrera que impide que cualquier usuario logueado
		// confirme la actividad de otra persona. El usuario SÍ tiene acceso de lectura a la finca (para que
		// el filtro de tenant no oculte la actividad y la prueba ejercite realmente la rama de autorización
		// del handler, no solo el filtro de tenant) pero no es el responsable asignado ni tiene permiso de
		// escritura — debe fallar.
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		currentUser.Setup(m => m.PuedeEscribirEnFinca(It.IsAny<int>())).Returns(false);
		var (ctx, bitacora) = CreateContext(currentUser);
		var actividad = SeedActividadAsignadaAJornalero(ctx, out _, out var otroUsuarioId, out var fincaId);
		currentUser.SetupGet(m => m.PersonaId).Returns(otroUsuarioId);
		currentUser.SetupGet(m => m.FincaIds).Returns([fincaId]);
		var handler = new ConfirmarActividadCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new ConfirmarActividadCommand(actividad.Id, "Otro Usuario"), CancellationToken.None);

		Assert.False(result.IsSuccess);
		var sinCambios = ctx.Actividades.Find(actividad.Id)!;
		Assert.False(sinCambios.Confirmada);
	}
}
