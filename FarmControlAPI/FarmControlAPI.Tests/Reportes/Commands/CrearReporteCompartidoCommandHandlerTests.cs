using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Reportes.Commands;
using FarmControlAPI.Domain.Entities;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Reportes.Commands;

public class CrearReporteCompartidoCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora, Finca finca) CreateContext()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();

		return (ctx, bitacora, finca);
	}

	[Fact]
	public async Task Handle_ConDiasValidez_GeneraTokenYExpiracion()
	{
		var (ctx, bitacora, finca) = CreateContext();
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var handler = new CrearReporteCompartidoCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var antes = DateTime.UtcNow;
		var result = await handler.Handle(new CrearReporteCompartidoCommand(finca.Id, 7), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.False(string.IsNullOrWhiteSpace(result.Value!.Token));
		Assert.Equal(finca.Id, result.Value.FincaId);
		Assert.NotNull(result.Value.FechaExpiracion);
		Assert.True(result.Value.FechaExpiracion!.Value >= antes.AddDays(7));
	}

	[Fact]
	public async Task Handle_SinDiasValidez_NoExpira()
	{
		var (ctx, bitacora, finca) = CreateContext();
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var handler = new CrearReporteCompartidoCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CrearReporteCompartidoCommand(finca.Id, null), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Null(result.Value!.FechaExpiracion);
	}

	[Fact]
	public async Task Handle_FincaInexistente_RetornaFallo()
	{
		var (ctx, bitacora, _) = CreateContext();
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var handler = new CrearReporteCompartidoCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CrearReporteCompartidoCommand(999, null), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
