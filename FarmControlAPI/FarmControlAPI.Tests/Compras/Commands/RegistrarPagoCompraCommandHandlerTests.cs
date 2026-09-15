using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Compras.Commands;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Compras.Commands;

public class RegistrarPagoCompraCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static Compra SeedCompra(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, decimal valor = 1000, decimal pagoPrevio = 0)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var socio = new Persona { ClienteId = cliente.Id, Nombre = "Socio 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Socio };
		ctx.Personas.Add(socio);
		ctx.SaveChanges();
		var compra = new Compra { FincaId = finca.Id, SocioId = socio.Id, Descripcion = "Insumo X", Valor = valor, Fecha = DateTime.UtcNow, TipoCompra = TipoCompra.Insumo };
		ctx.Compras.Add(compra);
		ctx.SaveChanges();
		if (pagoPrevio > 0)
		{
			ctx.PagosCompra.Add(new PagoCompra { CompraId = compra.Id, Monto = pagoPrevio, Fecha = DateTime.UtcNow });
			ctx.SaveChanges();
		}
		return compra;
	}

	[Fact]
	public async Task Handle_MontoDentroDelSaldo_RegistraElPago()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var compra = SeedCompra(ctx, valor: 1000);
		var handler = new RegistrarPagoCompraCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new RegistrarPagoCompraCommand(compra.Id, 400, DateTime.UtcNow, "abono"), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(400, result.Value!.Monto);
	}

	[Fact]
	public async Task Handle_MontoCeroOMenor_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var compra = SeedCompra(ctx, valor: 1000);
		var handler = new RegistrarPagoCompraCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new RegistrarPagoCompraCommand(compra.Id, 0, DateTime.UtcNow, null), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_MontoSuperaSaldoPendiente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var compra = SeedCompra(ctx, valor: 1000, pagoPrevio: 800);
		var handler = new RegistrarPagoCompraCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new RegistrarPagoCompraCommand(compra.Id, 500, DateTime.UtcNow, null), CancellationToken.None);

		Assert.False(result.IsSuccess);
		Assert.Contains("supera el saldo", result.Error);
	}
}
