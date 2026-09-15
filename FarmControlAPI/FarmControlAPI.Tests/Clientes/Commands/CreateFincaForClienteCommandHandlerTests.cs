using FarmControlAPI.Application.Clientes.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Clientes.Commands;

public class CreateFincaForClienteCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	[Fact]
	public async Task Handle_SinPlanAsignado_NoAplicaLimite()
	{
		var (ctx, bitacora) = CreateContext();
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var handler = new CreateFincaForClienteCommandHandler(ctx, bitacora.Object);

		var result = await handler.Handle(new CreateFincaForClienteCommand(cliente.Id, "Finca 1", null, 10, 100), CancellationToken.None);

		Assert.True(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_PlanIlimitado_PermiteCrearMasFincasDelLimiteBase()
	{
		var (ctx, bitacora) = CreateContext();
		var plan = new Plan { Nombre = "Ilimitado", MaxFincas = -1, MaxUsuarios = -1 };
		ctx.Planes.Add(plan);
		ctx.SaveChanges();
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow, PlanId = plan.Id };
		ctx.Clientes.Add(cliente);
		ctx.Fincas.Add(new Finca { ClienteId = cliente.Id, Nombre = "Existente", AreaTotal = 1, CostoTerreno = 1 });
		ctx.SaveChanges();
		var handler = new CreateFincaForClienteCommandHandler(ctx, bitacora.Object);

		var result = await handler.Handle(new CreateFincaForClienteCommand(cliente.Id, "Finca nueva", null, 10, 100), CancellationToken.None);

		Assert.True(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_LimiteDeFincasAlcanzado_RetornaFallo()
	{
		var (ctx, bitacora) = CreateContext();
		var plan = new Plan { Nombre = "Básico", MaxFincas = 1, MaxUsuarios = 3 };
		ctx.Planes.Add(plan);
		ctx.SaveChanges();
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow, PlanId = plan.Id };
		ctx.Clientes.Add(cliente);
		ctx.Fincas.Add(new Finca { ClienteId = cliente.Id, Nombre = "Finca única permitida", AreaTotal = 1, CostoTerreno = 1 });
		ctx.SaveChanges();
		var handler = new CreateFincaForClienteCommandHandler(ctx, bitacora.Object);

		var result = await handler.Handle(new CreateFincaForClienteCommand(cliente.Id, "Segunda finca", null, 10, 100), CancellationToken.None);

		Assert.False(result.IsSuccess);
		Assert.Contains("límite", result.Error);
	}
}
