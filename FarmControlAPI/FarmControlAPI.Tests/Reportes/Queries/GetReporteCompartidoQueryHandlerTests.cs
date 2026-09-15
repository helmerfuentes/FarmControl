using FarmControlAPI.Application.Reportes.Queries;
using FarmControlAPI.Domain.Entities;
using Xunit;

namespace FarmControlAPI.Tests.Reportes.Queries;

public class GetReporteCompartidoQueryHandlerTests
{
	private static FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext CreateContextSinAccesoALaFinca(int fincaId)
	{
		// El usuario actual deliberadamente NO tiene acceso a esta finca (fincaIds vacío, sin acceso global),
		// para comprobar que IgnoreQueryFilters() permite igual resolver el enlace público.
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		return TestDbContextFactory.Create(currentUser.Object);
	}

	private static Finca SeedFinca(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca Compartida", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();
		return finca;
	}

	[Fact]
	public async Task Handle_ConTokenValidoYNoExpirado_RetornaResumenIgnorandoElAislamientoDeTenant()
	{
		var finca = default(Finca)!;
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		finca = SeedFinca(ctx);
		var compartido = new ReporteCompartido
		{
			Token = "token-valido",
			FincaId = finca.Id,
			FechaCreacion = DateTime.UtcNow,
			FechaExpiracion = DateTime.UtcNow.AddDays(1)
		};
		ctx.ReportesCompartidos.Add(compartido);
		ctx.SaveChanges();

		var handler = new GetReporteCompartidoQueryHandler(ctx);
		var result = await handler.Handle(new GetReporteCompartidoQuery("token-valido"), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(finca.Id, result.Value!.FincaId);
		Assert.Equal("Finca Compartida", result.Value.Nombre);
	}

	[Fact]
	public async Task Handle_ConTokenDesconocido_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		SeedFinca(ctx);

		var handler = new GetReporteCompartidoQueryHandler(ctx);
		var result = await handler.Handle(new GetReporteCompartidoQuery("token-inexistente"), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_ConTokenExpirado_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var finca = SeedFinca(ctx);
		ctx.ReportesCompartidos.Add(new ReporteCompartido
		{
			Token = "token-expirado",
			FincaId = finca.Id,
			FechaCreacion = DateTime.UtcNow.AddDays(-10),
			FechaExpiracion = DateTime.UtcNow.AddDays(-1)
		});
		ctx.SaveChanges();

		var handler = new GetReporteCompartidoQueryHandler(ctx);
		var result = await handler.Handle(new GetReporteCompartidoQuery("token-expirado"), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
