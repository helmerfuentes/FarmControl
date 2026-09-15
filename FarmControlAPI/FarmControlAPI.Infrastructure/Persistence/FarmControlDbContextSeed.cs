using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Infrastructure.Persistence;

public static class FarmControlDbContextSeed
{
	private const string _CLIENTE_DEMO_NIT = "000000000-0";

	public static async Task SeedAsync(FarmControlDbContext context)
	{
		var clienteDemo = await SeedClienteDemoAsync(context);
		await SeedTiposInsumoAsync(context, clienteDemo);
		await SeedProductosAsync(context, clienteDemo);
		await SeedPlanesAsync(context);
		await context.SaveChangesAsync();
	}

	private static async Task SeedPlanesAsync(FarmControlDbContext context)
	{
		var hayPlanes = await context.Planes.AnyAsync();
		if (hayPlanes)
		{
			return;
		}

		var planes = new List<Plan>
		{
			new() { Nombre = "Básico", Descripcion = "Hasta 1 finca y 3 usuarios", MaxFincas = 1, MaxUsuarios = 3 },
			new() { Nombre = "Profesional", Descripcion = "Hasta 5 fincas y 15 usuarios", MaxFincas = 5, MaxUsuarios = 15 },
			new() { Nombre = "Ilimitado", Descripcion = "Sin límite de fincas ni usuarios", MaxFincas = -1, MaxUsuarios = -1 }
		};

		await context.Planes.AddRangeAsync(planes);
	}

	private static async Task<Cliente> SeedClienteDemoAsync(FarmControlDbContext context)
	{
		var clienteDemo = await context.Clientes.FirstOrDefaultAsync(c => c.NIT == _CLIENTE_DEMO_NIT);
		if (clienteDemo is null)
		{
			clienteDemo = new Cliente
			{
				RazonSocial = "Cliente Demo",
				NIT = _CLIENTE_DEMO_NIT,
				Email = "demo@farmcontrol.local",
				Activo = true,
				FechaAlta = DateTime.UtcNow
			};

			await context.Clientes.AddAsync(clienteDemo);
			await context.SaveChangesAsync();
		}

		return clienteDemo;
	}

	private static async Task SeedTiposInsumoAsync(FarmControlDbContext context, Cliente clienteDemo)
	{
		var hayDatos = await context.TiposInsumo.AnyAsync();
		if (hayDatos)
		{
			return;
		}

		var tipos = new List<TipoInsumo>
		{
			new() { ClienteId = clienteDemo.Id, Nombre = "Fertilizante", Descripcion = "Abonos y nutrientes para el suelo" },
			new() { ClienteId = clienteDemo.Id, Nombre = "Fungicida", Descripcion = "Control de hongos y enfermedades" },
			new() { ClienteId = clienteDemo.Id, Nombre = "Herbicida", Descripcion = "Control de malezas" },
			new() { ClienteId = clienteDemo.Id, Nombre = "Insecticida", Descripcion = "Control de plagas e insectos" },
			new() { ClienteId = clienteDemo.Id, Nombre = "Material vegetal", Descripcion = "Semillas, plántulas y material de siembra" },
			new() { ClienteId = clienteDemo.Id, Nombre = "Otro", Descripcion = "Insumo de categoría general" }
		};

		await context.TiposInsumo.AddRangeAsync(tipos);
	}

	private static async Task SeedProductosAsync(FarmControlDbContext context, Cliente clienteDemo)
	{
		var hayDatos = await context.Productos.AnyAsync();
		if (hayDatos)
		{
			return;
		}

		var productos = new List<Producto>
		{
			new()
			{
				ClienteId = clienteDemo.Id,
				Nombre = "Habichuela",
				Clasificaciones =
				[
					new ClasificacionProducto { Nombre = "Grueso",   UnidadMedida = Domain.Enums.UnidadMedidaVenta.Bulto, PesoUnidadKg = 60 },
					new ClasificacionProducto { Nombre = "Parejo",   UnidadMedida = Domain.Enums.UnidadMedidaVenta.Bulto, PesoUnidadKg = 60 },
					new ClasificacionProducto { Nombre = "Tercera",  UnidadMedida = Domain.Enums.UnidadMedidaVenta.Bulto, PesoUnidadKg = 60 }
				]
			},
			new()
			{
				ClienteId = clienteDemo.Id,
				Nombre = "Tomate guiso",
				Clasificaciones =
				[
					new ClasificacionProducto { Nombre = "Grueso",     UnidadMedida = Domain.Enums.UnidadMedidaVenta.Canastilla, PesoUnidadKg = 22 },
					new ClasificacionProducto { Nombre = "SemiGrueso", UnidadMedida = Domain.Enums.UnidadMedidaVenta.Canastilla, PesoUnidadKg = 22 },
					new ClasificacionProducto { Nombre = "Parejo",     UnidadMedida = Domain.Enums.UnidadMedidaVenta.Canastilla, PesoUnidadKg = 22 },
					new ClasificacionProducto { Nombre = "Rico",       UnidadMedida = Domain.Enums.UnidadMedidaVenta.Canastilla, PesoUnidadKg = 22 },
					new ClasificacionProducto { Nombre = "Carajola",   UnidadMedida = Domain.Enums.UnidadMedidaVenta.Canastilla, PesoUnidadKg = 22 }
				]
			},
			new()
			{
				ClienteId = clienteDemo.Id,
				Nombre = "Tomate de árbol",
				Clasificaciones =
				[
					new ClasificacionProducto { Nombre = "Grueso",  UnidadMedida = Domain.Enums.UnidadMedidaVenta.Canastilla, PesoUnidadKg = 25 },
					new ClasificacionProducto { Nombre = "Parejo",  UnidadMedida = Domain.Enums.UnidadMedidaVenta.Canastilla, PesoUnidadKg = 25 },
					new ClasificacionProducto { Nombre = "Tercera", UnidadMedida = Domain.Enums.UnidadMedidaVenta.Canastilla, PesoUnidadKg = 25 }
				]
			},
			new()
			{
				ClienteId = clienteDemo.Id,
				Nombre = "Granadilla",
				Clasificaciones =
				[
					new ClasificacionProducto { Nombre = "Fijas",  UnidadMedida = Domain.Enums.UnidadMedidaVenta.Caja, PesoUnidadKg = 14 },
					new ClasificacionProducto { Nombre = "Parejas", UnidadMedida = Domain.Enums.UnidadMedidaVenta.Caja, PesoUnidadKg = 14 }
				]
			}
		};

		await context.Productos.AddRangeAsync(productos);
	}
}
