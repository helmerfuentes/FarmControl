using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Tareas.Commands;
using FarmControlAPI.Domain.Entities;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Tareas.Commands;

public class CompletarTareaRecurrenteCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static TareaRecurrente SeedTarea(FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, int frecuenciaDias = 15)
	{
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();
		var parcela = new Parcela { FincaId = finca.Id, Nombre = "Parcela 1", Area = 5 };
		ctx.Parcelas.Add(parcela);
		ctx.SaveChanges();
		var tarea = new TareaRecurrente
		{
			ParcelaId = parcela.Id,
			Descripcion = "Fumigación",
			FrecuenciaDias = frecuenciaDias,
			ProximaFecha = DateTime.UtcNow.Date,
			Activa = true
		};
		ctx.TareasRecurrentes.Add(tarea);
		ctx.SaveChanges();
		return tarea;
	}

	[Fact]
	public async Task Handle_TareaExistente_ActualizaUltimaEjecucionYProximaFecha()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var tarea = SeedTarea(ctx, frecuenciaDias: 15);
		var handler = new CompletarTareaRecurrenteCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var hoy = DateTime.UtcNow.Date;
		var result = await handler.Handle(new CompletarTareaRecurrenteCommand(tarea.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(hoy, result.Value!.UltimaEjecucion);
		Assert.Equal(hoy.AddDays(15), result.Value.ProximaFecha);
	}

	[Fact]
	public async Task Handle_TareaInexistente_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var handler = new CompletarTareaRecurrenteCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CompletarTareaRecurrenteCommand(999), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
