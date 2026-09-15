using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Tareas.Commands;
using FarmControlAPI.Domain.Entities;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Tareas.Commands;

public class CreateTareaRecurrenteCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static Parcela SeedParcela(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, int fincaId = 1)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { Id = fincaId, ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var parcela = new Parcela { FincaId = fincaId, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		ctx.SaveChanges();
		return parcela;
	}

	[Fact]
	public async Task Handle_ConParcelaValida_CreaLaTarea()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var parcela = SeedParcela(ctx);
		var handler = new CreateTareaRecurrenteCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new CreateTareaRecurrenteCommand(parcela.Id, "Fumigación", 15, DateTime.UtcNow.Date.AddDays(15)),
			CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal("Fumigación", result.Value!.Descripcion);
		Assert.Equal(15, result.Value.FrecuenciaDias);
		Assert.True(result.Value.Activa);
	}

	[Fact]
	public async Task Handle_ConParcelaInexistente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var handler = new CreateTareaRecurrenteCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new CreateTareaRecurrenteCommand(999, "Fumigación", 15, DateTime.UtcNow.Date.AddDays(15)),
			CancellationToken.None);

		Assert.False(result.IsSuccess);
		Assert.Contains("no encontrada", result.Error);
	}

	[Fact]
	public async Task Handle_ConFrecuenciaCero_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var parcela = SeedParcela(ctx);
		var handler = new CreateTareaRecurrenteCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new CreateTareaRecurrenteCommand(parcela.Id, "Fumigación", 0, DateTime.UtcNow.Date),
			CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
