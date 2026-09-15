using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Nomina.Commands;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Nomina.Commands;

public class CrearLiquidacionNominaCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static (Persona jornalero, List<RegistroManoObra> registros) SeedRegistros(
		FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, DateTime fechaActividad)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		var jornalero = new Persona { ClienteId = cliente.Id, Nombre = "Jornalero 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		var socio = new Persona { ClienteId = cliente.Id, Nombre = "Socio 1", Documento = "2", Telefono = "2", TipoPersona = TipoPersona.Socio };
		ctx.Personas.AddRange(jornalero, socio);
		ctx.SaveChanges();
		var actividad = new Actividad { ParcelaId = parcela.Id, TipoActividad = "Fumigación", FechaInicio = fechaActividad };
		ctx.Actividades.Add(actividad);
		ctx.SaveChanges();

		var registros = new List<RegistroManoObra>
		{
			new() { ActividadId = actividad.Id, JornaleroId = jornalero.Id, SocioId = socio.Id, ValorHora = 10, NumHoras = 8, HoraInicio = new TimeSpan(7, 0, 0), HoraSalida = new TimeSpan(15, 0, 0) },
			new() { ActividadId = actividad.Id, JornaleroId = jornalero.Id, SocioId = socio.Id, ValorHora = 10, NumHoras = 4, HoraInicio = new TimeSpan(7, 0, 0), HoraSalida = new TimeSpan(11, 0, 0) },
		};
		ctx.RegistrosManoObra.AddRange(registros);
		ctx.SaveChanges();

		return (jornalero, registros);
	}

	[Fact]
	public async Task Handle_ConRegistrosPendientesEnRango_CreaLaLiquidacionYMarcaLosRegistros()
	{
		var (ctx, bitacora) = CreateContext();
		var fecha = new DateTime(2026, 3, 10);
		var (jornalero, registros) = SeedRegistros(ctx, fecha);
		var handler = new CrearLiquidacionNominaCommandHandler(ctx, bitacora.Object);

		var result = await handler.Handle(
			new CrearLiquidacionNominaCommand(jornalero.Id, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), "quincena"),
			CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(12, result.Value!.TotalHoras);
		Assert.Equal(120, result.Value.TotalPagar);
		foreach (var registro in registros)
		{
			var actualizado = ctx.RegistrosManoObra.Find(registro.Id)!;
			Assert.Equal(result.Value.Id, actualizado.LiquidacionNominaId);
		}
	}

	[Fact]
	public async Task Handle_SinRegistrosPendientesEnRango_RetornaFallo()
	{
		var (ctx, bitacora) = CreateContext();
		var fecha = new DateTime(2026, 3, 10);
		var (jornalero, _) = SeedRegistros(ctx, fecha);
		var handler = new CrearLiquidacionNominaCommandHandler(ctx, bitacora.Object);

		var result = await handler.Handle(
			new CrearLiquidacionNominaCommand(jornalero.Id, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), null),
			CancellationToken.None);

		Assert.False(result.IsSuccess);
		Assert.Contains("No hay registros", result.Error);
	}

	[Fact]
	public async Task Handle_SegundaLiquidacionEnElMismoRango_NoEncuentraRegistrosPendientes()
	{
		var (ctx, bitacora) = CreateContext();
		var fecha = new DateTime(2026, 3, 10);
		var (jornalero, _) = SeedRegistros(ctx, fecha);
		var handler = new CrearLiquidacionNominaCommandHandler(ctx, bitacora.Object);
		var rango = (Inicio: new DateTime(2026, 3, 1), Fin: new DateTime(2026, 3, 31));

		var primera = await handler.Handle(new CrearLiquidacionNominaCommand(jornalero.Id, rango.Inicio, rango.Fin, null), CancellationToken.None);
		var segunda = await handler.Handle(new CrearLiquidacionNominaCommand(jornalero.Id, rango.Inicio, rango.Fin, null), CancellationToken.None);

		Assert.True(primera.IsSuccess);
		Assert.False(segunda.IsSuccess);
	}
}
