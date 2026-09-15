using FarmControlAPI.Application.AnalisisSuelos.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.AnalisisSuelos.Commands;

public class CreateAnalisisSueloCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static Parcela SeedParcela(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
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
		return parcela;
	}

	[Fact]
	public async Task Handle_DatosValidos_RegistraElAnalisis()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var parcela = SeedParcela(ctx);
		var handler = new CreateAnalisisSueloCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new CreateAnalisisSueloCommand(parcela.Id, new DateTime(2026, 3, 1), 6.5m, 3.2m, 40m, 20m, 100m, "Muestra tomada en la mañana"),
			CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(parcela.Id, result.Value!.ParcelaId);
		Assert.Equal(6.5m, result.Value.Ph);
	}

	[Fact]
	public async Task Handle_ParcelaInexistente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var handler = new CreateAnalisisSueloCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new CreateAnalisisSueloCommand(9999, DateTime.UtcNow, null, null, null, null, null, null),
			CancellationToken.None);

		Assert.False(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_FincaSoloLectura_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: [1]);
		currentUser.Setup(m => m.PuedeEscribirEnFinca(It.IsAny<int>())).Returns(false);
		var (ctx, bitacora) = CreateContext(currentUser);
		var parcela = SeedParcela(ctx);
		var handler = new CreateAnalisisSueloCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new CreateAnalisisSueloCommand(parcela.Id, DateTime.UtcNow, null, null, null, null, null, null),
			CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
