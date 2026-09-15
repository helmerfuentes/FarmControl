using FarmControlAPI.Application.Actividades.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Xunit;

namespace FarmControlAPI.Tests.Actividades.Queries;

public class GetMisActividadesQueryHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Persona jornaleroA, Persona jornaleroB, Actividad actividadDeA, Actividad actividadDeB) SeedEscenario()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: 0);
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var jornaleroA = new Persona { ClienteId = cliente.Id, Nombre = "Jornalero A", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		var jornaleroB = new Persona { ClienteId = cliente.Id, Nombre = "Jornalero B", Documento = "2", Telefono = "1", TipoPersona = TipoPersona.Jornalero };
		ctx.Personas.AddRange(jornaleroA, jornaleroB);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		ctx.SaveChanges();

		var hoy = DateTime.UtcNow.Date;
		var actividadDeA = new Actividad { ParcelaId = parcela.Id, TipoActividad = "Riego", FechaInicio = hoy.AddHours(8), ValorDiaUsado = 1, Descripcion = "x", PersonaACargoId = jornaleroA.Id };
		var actividadDeB = new Actividad { ParcelaId = parcela.Id, TipoActividad = "Cosecha", FechaInicio = hoy.AddHours(9), ValorDiaUsado = 1, Descripcion = "y", PersonaACargoId = jornaleroB.Id };
		var actividadDeAOtroDia = new Actividad { ParcelaId = parcela.Id, TipoActividad = "Poda", FechaInicio = hoy.AddDays(-5), ValorDiaUsado = 1, Descripcion = "z", PersonaACargoId = jornaleroA.Id };
		ctx.Actividades.AddRange(actividadDeA, actividadDeB, actividadDeAOtroDia);
		ctx.SaveChanges();

		return (ctx, jornaleroA, jornaleroB, actividadDeA, actividadDeB);
	}

	[Fact]
	public async Task Handle_ParaHoy_RetornaSoloLasActividadesDelPropioJornalero()
	{
		var (ctx, jornaleroA, _, actividadDeA, _) = SeedEscenario();
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: jornaleroA.Id);
		var handler = new GetMisActividadesQueryHandler(ctx, currentUser.Object);

		var result = await handler.Handle(new GetMisActividadesQuery(DateTime.UtcNow.Date), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Single(result.Value!);
		Assert.Equal(actividadDeA.Id, result.Value![0].Id);
	}

	[Fact]
	public async Task Handle_SinPersonaIdEnLaSesion_RetornaListaVacia()
	{
		var (ctx, _, _, _, _) = SeedEscenario();
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: null);
		var handler = new GetMisActividadesQueryHandler(ctx, currentUser.Object);

		var result = await handler.Handle(new GetMisActividadesQuery(null), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Empty(result.Value!);
	}
}
