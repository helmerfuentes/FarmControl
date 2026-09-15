using FarmControlAPI.Application.Common;
using FarmControlAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FarmControlAPI.Tests;

internal static class TestDbContextFactory
{
	public static FarmControlDbContext Create(ICurrentUserContext currentUser)
	{
		var options = new DbContextOptionsBuilder<FarmControlDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new FarmControlDbContext(options, currentUser);
	}

	public static Mock<ICurrentUserContext> CreateCurrentUserMock(
		bool tieneAccesoGlobal = true,
		IReadOnlyList<int>? fincaIds = null,
		int? personaId = 1,
		int? clienteId = 1)
	{
		var mock = new Mock<ICurrentUserContext>();
		mock.SetupGet(m => m.TieneAccesoGlobal).Returns(tieneAccesoGlobal);
		mock.SetupGet(m => m.IsSuperAdmin).Returns(tieneAccesoGlobal);
		mock.SetupGet(m => m.FincaIds).Returns(fincaIds ?? []);
		mock.SetupGet(m => m.FincaIdsSoloLectura).Returns([]);
		mock.SetupGet(m => m.FincaIdsSinEliminar).Returns([]);
		mock.SetupGet(m => m.PersonaActiva).Returns(true);
		mock.SetupGet(m => m.PersonaId).Returns(personaId);
		mock.SetupGet(m => m.ClienteId).Returns(clienteId);
		mock.Setup(m => m.TieneAccesoAFinca(It.IsAny<int>())).Returns(true);
		mock.Setup(m => m.PuedeEscribirEnFinca(It.IsAny<int>())).Returns(true);
		mock.Setup(m => m.PuedeEliminarEnFinca(It.IsAny<int>())).Returns(true);
		return mock;
	}
}
