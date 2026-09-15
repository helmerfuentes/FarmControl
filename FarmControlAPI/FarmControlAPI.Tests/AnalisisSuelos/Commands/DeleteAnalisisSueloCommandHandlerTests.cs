using FarmControlAPI.Application.AnalisisSuelos.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.AnalisisSuelos.Commands;

public class DeleteAnalisisSueloCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static AnalisisSuelo SeedAnalisis(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
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
		var analisis = new AnalisisSuelo { ParcelaId = parcela.Id, Fecha = DateTime.UtcNow, Ph = 6.5m };
		ctx.AnalisisSuelo.Add(analisis);
		ctx.SaveChanges();
		return analisis;
	}

	[Fact]
	public async Task Handle_AnalisisExistente_LoElimina()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var analisis = SeedAnalisis(ctx);
		var handler = new DeleteAnalisisSueloCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new DeleteAnalisisSueloCommand(analisis.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Empty(ctx.AnalisisSuelo);
	}

	[Fact]
	public async Task Handle_AnalisisInexistente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var handler = new DeleteAnalisisSueloCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new DeleteAnalisisSueloCommand(9999), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
