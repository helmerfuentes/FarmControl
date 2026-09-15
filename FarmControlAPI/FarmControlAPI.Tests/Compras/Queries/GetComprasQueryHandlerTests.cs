using FarmControlAPI.Application.Compras.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Xunit;

namespace FarmControlAPI.Tests.Compras.Queries;

public class GetComprasQueryHandlerTests
{
	[Fact]
	public async Task Handle_ConVariosPagos_CalculaTotalPagadoYSaldoPendiente()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var ctx = TestDbContextFactory.Create(currentUser.Object);

		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		var socio = new Persona { ClienteId = cliente.Id, Nombre = "Socio 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Socio };
		ctx.Personas.Add(socio);
		ctx.SaveChanges();
		var compra = new Compra { FincaId = finca.Id, SocioId = socio.Id, Descripcion = "Insumo X", Valor = 1000, Fecha = DateTime.UtcNow, TipoCompra = TipoCompra.Insumo, Proveedor = "Agroinsumos SA" };
		ctx.Compras.Add(compra);
		ctx.SaveChanges();
		ctx.PagosCompra.Add(new PagoCompra { CompraId = compra.Id, Monto = 300, Fecha = DateTime.UtcNow });
		ctx.PagosCompra.Add(new PagoCompra { CompraId = compra.Id, Monto = 200, Fecha = DateTime.UtcNow });
		ctx.SaveChanges();

		var handler = new GetComprasQueryHandler(ctx);
		var result = await handler.Handle(new GetComprasQuery(null), CancellationToken.None);

		Assert.True(result.IsSuccess);
		var dto = Assert.Single(result.Value!);
		Assert.Equal("Agroinsumos SA", dto.Proveedor);
		Assert.Equal(500, dto.TotalPagado);
		Assert.Equal(500, dto.SaldoPendiente);
	}
}
