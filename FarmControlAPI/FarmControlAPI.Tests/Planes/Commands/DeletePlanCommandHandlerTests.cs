using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Planes.Commands;
using FarmControlAPI.Domain.Entities;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Planes.Commands;

public class DeletePlanCommandHandlerTests
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
	public async Task Handle_PlanSinClientesAsignados_LoElimina()
	{
		var (ctx, bitacora) = CreateContext();
		var plan = new Plan { Nombre = "Básico", MaxFincas = 1, MaxUsuarios = 3 };
		ctx.Planes.Add(plan);
		ctx.SaveChanges();
		var handler = new DeletePlanCommandHandler(ctx, bitacora.Object);

		var result = await handler.Handle(new DeletePlanCommand(plan.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Empty(ctx.Planes);
	}

	[Fact]
	public async Task Handle_PlanConClientesAsignados_RetornaFallo()
	{
		var (ctx, bitacora) = CreateContext();
		var plan = new Plan { Nombre = "Básico", MaxFincas = 1, MaxUsuarios = 3 };
		ctx.Planes.Add(plan);
		ctx.SaveChanges();
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow, PlanId = plan.Id };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var handler = new DeletePlanCommandHandler(ctx, bitacora.Object);

		var result = await handler.Handle(new DeletePlanCommand(plan.Id), CancellationToken.None);

		Assert.False(result.IsSuccess);
		Assert.Single(ctx.Planes);
	}
}
