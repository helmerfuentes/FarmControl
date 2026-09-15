using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Ventas.Commands;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Ventas.Commands;

public class RegistrarPagoVentaCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static Venta SeedVenta(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, decimal total = 1000, decimal pagoPrevio = 0)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		var comprador = new Persona { ClienteId = cliente.Id, Nombre = "Comprador 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Comprador };
		ctx.Personas.Add(comprador);
		ctx.SaveChanges();
		var venta = new Venta { ParcelaId = parcela.Id, CompradorId = comprador.Id, Fecha = DateTime.UtcNow, Total = total };
		ctx.Ventas.Add(venta);
		ctx.SaveChanges();
		if (pagoPrevio > 0)
		{
			ctx.PagosVenta.Add(new PagoVenta { VentaId = venta.Id, Monto = pagoPrevio, Fecha = DateTime.UtcNow });
			ctx.SaveChanges();
		}
		return venta;
	}

	[Fact]
	public async Task Handle_MontoDentroDelSaldo_RegistraElPago()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var venta = SeedVenta(ctx, total: 1000);
		var handler = new RegistrarPagoVentaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new RegistrarPagoVentaCommand(venta.Id, 400, DateTime.UtcNow, "abono"), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(400, result.Value!.Monto);
	}

	[Fact]
	public async Task Handle_MontoCeroOMenor_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var venta = SeedVenta(ctx, total: 1000);
		var handler = new RegistrarPagoVentaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new RegistrarPagoVentaCommand(venta.Id, 0, DateTime.UtcNow, null), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_MontoSuperaSaldoPendiente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var venta = SeedVenta(ctx, total: 1000, pagoPrevio: 800);
		var handler = new RegistrarPagoVentaCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new RegistrarPagoVentaCommand(venta.Id, 500, DateTime.UtcNow, null), CancellationToken.None);

		Assert.False(result.IsSuccess);
		Assert.Contains("supera el saldo", result.Error);
	}
}
