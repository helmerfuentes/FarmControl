using FarmControlAPI.Application.Ventas.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Xunit;

namespace FarmControlAPI.Tests.Ventas.Queries;

public class GetCompradoresFrecuentesQueryHandlerTests
{
	private static FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext CreateContext()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		return TestDbContextFactory.Create(currentUser.Object);
	}

	private static (Persona compradorA, Persona compradorB, Parcela parcela) SeedBase(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		var compradorA = new Persona { ClienteId = cliente.Id, Nombre = "Comprador A", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Comprador };
		var compradorB = new Persona { ClienteId = cliente.Id, Nombre = "Comprador B", Documento = "2", Telefono = "2", TipoPersona = TipoPersona.Comprador };
		ctx.Personas.AddRange(compradorA, compradorB);
		ctx.SaveChanges();
		return (compradorA, compradorB, parcela);
	}

	private static void SeedVentaConDetalle(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, int parcelaId, int compradorId, DateTime fecha, decimal subtotal)
	{
		var venta = new Venta { ParcelaId = parcelaId, CompradorId = compradorId, Fecha = fecha, Total = subtotal };
		ctx.Ventas.Add(venta);
		ctx.SaveChanges();
		ctx.DetallesVenta.Add(new DetalleVenta
		{
			VentaId = venta.Id,
			Clasificacion = "Primera",
			Cantidad = 1,
			UnidadMedida = UnidadMedidaVenta.Kilo,
			PrecioUnitario = subtotal,
			Subtotal = subtotal
		});
		ctx.SaveChanges();
	}

	[Fact]
	public async Task Handle_AgrupaPorCompradorYCalculaTotales()
	{
		var ctx = CreateContext();
		var (compradorA, compradorB, parcela) = SeedBase(ctx);

		SeedVentaConDetalle(ctx, parcela.Id, compradorA.Id, new DateTime(2026, 1, 10), 1000);
		SeedVentaConDetalle(ctx, parcela.Id, compradorA.Id, new DateTime(2026, 2, 15), 500);
		SeedVentaConDetalle(ctx, parcela.Id, compradorB.Id, new DateTime(2026, 1, 5), 300);

		var handler = new GetCompradoresFrecuentesQueryHandler(ctx);
		var result = await handler.Handle(new GetCompradoresFrecuentesQuery(), CancellationToken.None);

		Assert.True(result.IsSuccess);
		var lista = result.Value!;
		Assert.Equal(2, lista.Count);

		var dtoA = lista.Single(c => c.CompradorId == compradorA.Id);
		Assert.Equal(2, dtoA.NumVentas);
		Assert.Equal(1500, dtoA.VolumenTotal);
		Assert.Equal(new DateTime(2026, 2, 15), dtoA.UltimaVenta);

		var dtoB = lista.Single(c => c.CompradorId == compradorB.Id);
		Assert.Equal(1, dtoB.NumVentas);
		Assert.Equal(300, dtoB.VolumenTotal);

		// Ordenado descendente por volumen total.
		Assert.Equal(compradorA.Id, lista[0].CompradorId);
	}
}
