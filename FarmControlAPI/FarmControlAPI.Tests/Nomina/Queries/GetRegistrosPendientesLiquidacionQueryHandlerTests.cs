using FarmControlAPI.Application.Nomina.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Xunit;

namespace FarmControlAPI.Tests.Nomina.Queries;

public class GetRegistrosPendientesLiquidacionQueryHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Persona jornalero, Parcela parcela) SeedBase()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var ctx = TestDbContextFactory.Create(currentUser.Object);
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
		return (ctx, jornalero, parcela);
	}

	private static RegistroManoObra AddRegistro(
		FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Parcela parcela, Persona jornalero,
		DateTime fechaActividad, bool yaLiquidado = false)
	{
		var actividad = new Actividad { ParcelaId = parcela.Id, TipoActividad = "Riego", FechaInicio = fechaActividad };
		ctx.Actividades.Add(actividad);
		ctx.SaveChanges();

		var liquidacion = default(LiquidacionNomina);
		if (yaLiquidado)
		{
			liquidacion = new LiquidacionNomina { JornaleroId = jornalero.Id, FechaInicio = fechaActividad, FechaFin = fechaActividad, TotalHoras = 1, TotalPagar = 10, FechaLiquidacion = DateTime.UtcNow };
			ctx.LiquidacionesNomina.Add(liquidacion);
			ctx.SaveChanges();
		}

		var registro = new RegistroManoObra
		{
			ActividadId = actividad.Id,
			JornaleroId = jornalero.Id,
			SocioId = jornalero.Id,
			ValorHora = 10,
			NumHoras = 6,
			HoraInicio = new TimeSpan(7, 0, 0),
			HoraSalida = new TimeSpan(13, 0, 0),
			LiquidacionNominaId = liquidacion?.Id,
		};
		ctx.RegistrosManoObra.Add(registro);
		ctx.SaveChanges();
		return registro;
	}

	[Fact]
	public async Task Handle_ExcluyeRegistrosYaLiquidadosYFueraDeRango()
	{
		var (ctx, jornalero, parcela) = SeedBase();
		AddRegistro(ctx, parcela, jornalero, new DateTime(2026, 3, 5)); // pendiente, dentro de rango
		AddRegistro(ctx, parcela, jornalero, new DateTime(2026, 3, 15), yaLiquidado: true); // ya liquidado
		AddRegistro(ctx, parcela, jornalero, new DateTime(2026, 2, 1)); // fuera de rango

		var handler = new GetRegistrosPendientesLiquidacionQueryHandler(ctx);
		var result = await handler.Handle(
			new GetRegistrosPendientesLiquidacionQuery(jornalero.Id, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)),
			CancellationToken.None);

		Assert.True(result.IsSuccess);
		var registro = Assert.Single(result.Value!.Registros);
		Assert.Equal(new DateTime(2026, 3, 5), registro.FechaActividad);
		Assert.Equal(6, result.Value.TotalHoras);
		Assert.Equal(60, result.Value.TotalPagar);
	}
}
