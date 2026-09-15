using FarmControlAPI.Application.Reportes.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Xunit;

namespace FarmControlAPI.Tests.Reportes.Queries;

public class GetEstacionalidadPreciosQueryHandlerTests
{
	private static FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext CreateContext()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		return TestDbContextFactory.Create(currentUser.Object);
	}

	private static Producto SeedProductoConHistorial(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var producto = new Producto { ClienteId = cliente.Id, Nombre = "Café" };
		ctx.Productos.Add(producto);
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		var comprador = new Persona { ClienteId = cliente.Id, Nombre = "Comprador 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Comprador };
		ctx.Personas.Add(comprador);
		ctx.SaveChanges();

		// Dos ventas en enero (años distintos) y una en marzo, para probar agrupación por mes-del-año.
		var venta1 = new Venta { ParcelaId = parcela.Id, CompradorId = comprador.Id, Fecha = new DateTime(2024, 1, 15), Total = 100 };
		var venta2 = new Venta { ParcelaId = parcela.Id, CompradorId = comprador.Id, Fecha = new DateTime(2025, 1, 20), Total = 120 };
		var venta3 = new Venta { ParcelaId = parcela.Id, CompradorId = comprador.Id, Fecha = new DateTime(2025, 3, 10), Total = 80 };
		ctx.Ventas.AddRange(venta1, venta2, venta3);
		ctx.SaveChanges();

		ctx.HistorialPreciosVenta.AddRange(
			new HistorialPrecioVenta { ProductoId = producto.Id, Clasificacion = "Primera", PrecioUnitario = 1000, UnidadMedida = UnidadMedidaVenta.Kilo, Fecha = venta1.Fecha, VentaId = venta1.Id },
			new HistorialPrecioVenta { ProductoId = producto.Id, Clasificacion = "Primera", PrecioUnitario = 1200, UnidadMedida = UnidadMedidaVenta.Kilo, Fecha = venta2.Fecha, VentaId = venta2.Id },
			new HistorialPrecioVenta { ProductoId = producto.Id, Clasificacion = "Primera", PrecioUnitario = 900, UnidadMedida = UnidadMedidaVenta.Kilo, Fecha = venta3.Fecha, VentaId = venta3.Id });
		ctx.SaveChanges();

		return producto;
	}

	[Fact]
	public async Task Handle_ConHistorialMultiAnio_AgrupaPorMesDelAnioIgnorandoElAnio()
	{
		var ctx = CreateContext();
		var producto = SeedProductoConHistorial(ctx);
		var handler = new GetEstacionalidadPreciosQueryHandler(ctx);

		var result = await handler.Handle(new GetEstacionalidadPreciosQuery(producto.Id, null), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(12, result.Value!.PorMes.Count);

		var enero = result.Value.PorMes.Single(m => m.Mes == 1);
		Assert.Equal(2, enero.NumVentas);
		Assert.Equal(1100m, enero.PrecioPromedio);
		Assert.Equal(1000m, enero.PrecioMin);
		Assert.Equal(1200m, enero.PrecioMax);

		var marzo = result.Value.PorMes.Single(m => m.Mes == 3);
		Assert.Equal(1, marzo.NumVentas);
		Assert.Equal(900m, marzo.PrecioPromedio);

		var febrero = result.Value.PorMes.Single(m => m.Mes == 2);
		Assert.Equal(0, febrero.NumVentas);
		Assert.Null(febrero.PrecioPromedio);
	}

	[Fact]
	public async Task Handle_ProductoInexistente_RetornaFallo()
	{
		var ctx = CreateContext();
		var handler = new GetEstacionalidadPreciosQueryHandler(ctx);

		var result = await handler.Handle(new GetEstacionalidadPreciosQuery(9999, null), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
