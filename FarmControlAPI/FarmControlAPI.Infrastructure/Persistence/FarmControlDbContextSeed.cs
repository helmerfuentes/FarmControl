using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Infrastructure.Persistence;

public static class FarmControlDbContextSeed
{
	public static async Task SeedAsync(FarmControlDbContext context)
	{
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
}
