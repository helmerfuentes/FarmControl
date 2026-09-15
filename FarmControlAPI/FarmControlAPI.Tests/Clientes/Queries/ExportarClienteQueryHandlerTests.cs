using FarmControlAPI.Application.Clientes.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Xunit;

namespace FarmControlAPI.Tests.Clientes.Queries;

public class ExportarClienteQueryHandlerTests
{
	[Fact]
	public async Task Handle_ClienteConDatosCompletos_ExportaTodoSinPasswordHash()
	{
		// El usuario actual deliberadamente NO tiene acceso a las fincas de este cliente,
		// para comprobar que IgnoreQueryFilters() permite igual exportar (SuperAdmin).
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		var ctx = TestDbContextFactory.Create(currentUser.Object);

		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();

		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);

		var persona = new Persona
		{
			ClienteId = cliente.Id, Nombre = "Persona 1", Documento = "1", Telefono = "1",
			TipoPersona = TipoPersona.Socio, NombreUsuario = "persona1", PasswordHash = "hash-secreto-no-debe-exportarse",
		};
		ctx.Personas.Add(persona);

		var producto = new Producto { ClienteId = cliente.Id, Nombre = "Tomate" };
		ctx.Productos.Add(producto);
		ctx.SaveChanges();

		var compra = new Compra { FincaId = finca.Id, SocioId = persona.Id, Descripcion = "Insumo", Valor = 100, Fecha = DateTime.UtcNow, TipoCompra = TipoCompra.Insumo, Proveedor = "Proveedor X" };
		ctx.Compras.Add(compra);
		ctx.SaveChanges();
		ctx.PagosCompra.Add(new PagoCompra { CompraId = compra.Id, Monto = 50, Fecha = DateTime.UtcNow });

		var venta = new Venta { ParcelaId = parcela.Id, CompradorId = persona.Id, Fecha = DateTime.UtcNow, Total = 200 };
		ctx.Ventas.Add(venta);
		ctx.SaveChanges();
		ctx.DetallesVenta.Add(new DetalleVenta { VentaId = venta.Id, Clasificacion = "Primera", Cantidad = 10, UnidadMedida = UnidadMedidaVenta.Kilo, PrecioUnitario = 20, Subtotal = 200 });
		ctx.PagosVenta.Add(new PagoVenta { VentaId = venta.Id, Monto = 200, Fecha = DateTime.UtcNow });
		ctx.SaveChanges();

		var handler = new ExportarClienteQueryHandler(ctx);
		var result = await handler.Handle(new ExportarClienteQuery(cliente.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		var dto = result.Value!;
		Assert.Equal("Cliente 1", dto.RazonSocial);
		var fincaDto = Assert.Single(dto.Fincas);
		Assert.Single(fincaDto.Parcelas);
		var personaDto = Assert.Single(dto.Personas);
		Assert.Equal("persona1", personaDto.NombreUsuario);
		Assert.DoesNotContain("PasswordHash", typeof(PersonaExportDto).GetProperties().Select(p => p.Name));
		Assert.Single(dto.Productos);
		var compraDto = Assert.Single(dto.Compras);
		Assert.Equal("Proveedor X", compraDto.Proveedor);
		Assert.Single(compraDto.Pagos);
		var ventaDto = Assert.Single(dto.Ventas);
		Assert.Single(ventaDto.Detalles);
		Assert.Single(ventaDto.Pagos);
	}

	[Fact]
	public async Task Handle_ClienteInexistente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var handler = new ExportarClienteQueryHandler(ctx);

		var result = await handler.Handle(new ExportarClienteQuery(9999), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
