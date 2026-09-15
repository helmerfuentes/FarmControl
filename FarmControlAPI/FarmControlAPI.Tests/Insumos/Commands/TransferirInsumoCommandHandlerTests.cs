using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Insumos.Commands;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Insumos.Commands;

public class TransferirInsumoCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static (Insumo insumo, Parcela origen, Parcela destino) SeedDatos(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();
		var origen = new Parcela { FincaId = finca.Id, Nombre = "Parcela Origen", Area = 5 };
		var destino = new Parcela { FincaId = finca.Id, Nombre = "Parcela Destino", Area = 5 };
		ctx.Parcelas.AddRange(origen, destino);
		ctx.SaveChanges();
		var tipoInsumo = new TipoInsumo { ClienteId = cliente.Id, Nombre = "Fertilizante", Descripcion = "d" };
		ctx.TiposInsumo.Add(tipoInsumo);
		ctx.SaveChanges();
		var insumo = new Insumo
		{
			TipoInsumoId = tipoInsumo.Id,
			Nombre = "Urea",
			Marca = "Marca",
			Descripcion = "d",
			PrecioUnitario = 1000,
			UnidadMedida = "kg"
		};
		ctx.Insumos.Add(insumo);
		ctx.SaveChanges();
		return (insumo, origen, destino);
	}

	[Fact]
	public async Task Handle_ConDatosValidos_CreaMovimientoSalidaYEntrada()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var (insumo, origen, destino) = SeedDatos(ctx);
		var handler = new TransferirInsumoCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new TransferirInsumoCommand(insumo.Id, origen.Id, destino.Id, 10, DateTime.UtcNow, "obs"),
			CancellationToken.None);

		Assert.True(result.IsSuccess);
		var salida = await ctx.MovimientosInsumo.FirstAsync(m => m.Id == result.Value!.MovimientoSalidaId);
		var entrada = await ctx.MovimientosInsumo.FirstAsync(m => m.Id == result.Value!.MovimientoEntradaId);
		Assert.Equal(TipoMovimientoInsumo.Salida, salida.TipoMovimiento);
		Assert.Equal(origen.Id, salida.ParcelaId);
		Assert.Equal(TipoMovimientoInsumo.Entrada, entrada.TipoMovimiento);
		Assert.Equal(destino.Id, entrada.ParcelaId);
		Assert.Equal(10, salida.Cantidad);
		Assert.Equal(10, entrada.Cantidad);
	}

	[Fact]
	public async Task Handle_MismaParcelaOrigenYDestino_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var (insumo, origen, _) = SeedDatos(ctx);
		var handler = new TransferirInsumoCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new TransferirInsumoCommand(insumo.Id, origen.Id, origen.Id, 10, DateTime.UtcNow, null),
			CancellationToken.None);

		Assert.False(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_CantidadCeroOMenor_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var (insumo, origen, destino) = SeedDatos(ctx);
		var handler = new TransferirInsumoCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(
			new TransferirInsumoCommand(insumo.Id, origen.Id, destino.Id, 0, DateTime.UtcNow, null),
			CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
