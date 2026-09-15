using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Planes.Commands;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Planes.Commands;

public class CreatePlanCommandHandlerTests
{
	[Fact]
	public async Task Handle_DatosValidos_CreaElPlan()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		var handler = new CreatePlanCommandHandler(ctx, bitacora.Object);

		var result = await handler.Handle(new CreatePlanCommand("Profesional", "Plan intermedio", 5, 15), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal("Profesional", result.Value!.Nombre);
		Assert.Equal(5, result.Value.MaxFincas);
		Assert.Single(ctx.Planes);
	}
}
