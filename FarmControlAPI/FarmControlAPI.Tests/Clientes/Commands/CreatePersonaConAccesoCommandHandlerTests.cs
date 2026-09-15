using FarmControlAPI.Application.Clientes.Commands;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Clientes.Commands;

public class CreatePersonaConAccesoCommandHandlerTests
{
	private static (FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, Mock<IBitacoraService> bitacora) CreateContext(Mock<ICurrentUserContext> currentUser)
	{
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var bitacora = new Mock<IBitacoraService>();
		bitacora.Setup(b => b.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return (ctx, bitacora);
	}

	private static (Cliente cliente, Finca finca, Plan plan) SeedClienteConPlan(
		FarmControlAPI.Infrastructure.Persistence.FarmControlDbContext ctx, int maxUsuarios, int usuariosExistentes)
	{
		var plan = new Plan { Nombre = "Básico", MaxFincas = -1, MaxUsuarios = maxUsuarios };
		ctx.Planes.Add(plan);
		ctx.SaveChanges();
		var cliente = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow, PlanId = plan.Id };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var finca = new Finca { ClienteId = cliente.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		ctx.Fincas.Add(finca);
		ctx.SaveChanges();

		for (var i = 0; i < usuariosExistentes; i++)
		{
			ctx.Personas.Add(new Persona
			{
				ClienteId = cliente.Id,
				Nombre = $"Usuario existente {i}",
				Documento = $"doc-{i}",
				Telefono = "1",
				TipoPersona = TipoPersona.Admin,
				NombreUsuario = $"usuario{i}"
			});
		}
		ctx.SaveChanges();

		return (cliente, finca, plan);
	}

	[Fact]
	public async Task Handle_LimiteDeUsuariosAlcanzado_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var (cliente, finca, _) = SeedClienteConPlan(ctx, maxUsuarios: 1, usuariosExistentes: 1);
		var handler = new CreatePersonaConAccesoCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CreatePersonaConAccesoCommand(
			cliente.Id, "Nuevo Admin", "doc-x", "1", null, TipoPersona.Admin, "nuevoadmin", "clave123",
			[new AccesoFincaInput(finca.Id, false, true)]), CancellationToken.None);

		Assert.False(result.IsSuccess);
		Assert.Contains("límite", result.Error);
	}

	[Fact]
	public async Task Handle_PlanIlimitado_PermiteCrearMasUsuarios()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var (cliente, finca, _) = SeedClienteConPlan(ctx, maxUsuarios: -1, usuariosExistentes: 5);
		var handler = new CreatePersonaConAccesoCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CreatePersonaConAccesoCommand(
			cliente.Id, "Nuevo Admin", "doc-x", "1", null, TipoPersona.Admin, "nuevoadmin2", "clave123",
			[new AccesoFincaInput(finca.Id, false, true)]), CancellationToken.None);

		Assert.True(result.IsSuccess);
	}

	[Fact]
	public async Task Handle_DentroDelLimite_CreaElUsuario()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock();
		var (ctx, bitacora) = CreateContext(currentUser);
		var (cliente, finca, _) = SeedClienteConPlan(ctx, maxUsuarios: 3, usuariosExistentes: 1);
		var handler = new CreatePersonaConAccesoCommandHandler(ctx, currentUser.Object, bitacora.Object);

		var result = await handler.Handle(new CreatePersonaConAccesoCommand(
			cliente.Id, "Nuevo Admin", "doc-x", "1", null, TipoPersona.Admin, "nuevoadmin3", "clave123",
			[new AccesoFincaInput(finca.Id, false, true)]), CancellationToken.None);

		Assert.True(result.IsSuccess);
	}
}
