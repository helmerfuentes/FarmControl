using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Sistema.Queries;
using FarmControlAPI.Domain.Entities;
using FarmControlAPI.Domain.Enums;
using Moq;
using Xunit;

namespace FarmControlAPI.Tests.Sistema.Queries;

public class GetSaludSistemaQueryHandlerTests
{
	[Fact]
	public async Task Handle_SinAccesoDeTenantAlUsuarioActual_AgregaTotalesDeTodosLosClientes()
	{
		// El usuario actual deliberadamente NO tiene acceso global ni fincaIds propios,
		// para comprobar que IgnoreQueryFilters() permite igual ver todo el sistema (SuperAdmin).
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(tieneAccesoGlobal: false, fincaIds: []);
		var ctx = TestDbContextFactory.Create(currentUser.Object);

		var cliente1 = new Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow, Activo = true };
		var cliente2 = new Cliente { RazonSocial = "Cliente 2", NIT = "2", Email = "b@b.com", FechaAlta = DateTime.UtcNow, Activo = false };
		ctx.Clientes.AddRange(cliente1, cliente2);
		ctx.SaveChanges();

		var finca1 = new Finca { ClienteId = cliente1.Id, Nombre = "Finca 1", AreaTotal = 10, CostoTerreno = 100 };
		var finca2 = new Finca { ClienteId = cliente2.Id, Nombre = "Finca 2", AreaTotal = 20, CostoTerreno = 200 };
		ctx.Fincas.AddRange(finca1, finca2);
		ctx.SaveChanges();

		var persona1 = new Persona { ClienteId = cliente1.Id, Nombre = "Persona 1", Documento = "1", Telefono = "1", TipoPersona = TipoPersona.Socio };
		var persona2 = new Persona { ClienteId = cliente2.Id, Nombre = "Persona 2", Documento = "2", Telefono = "2", TipoPersona = TipoPersona.Socio };
		ctx.Personas.AddRange(persona1, persona2);
		ctx.SaveChanges();

		ctx.BitacoraEntries.Add(new BitacoraEntry { FechaHora = DateTime.UtcNow, ClienteId = cliente1.Id, ActorNombre = "Persona 1", Accion = "Login", Detalle = "Inicio de sesión" });
		ctx.SaveChanges();

		var backupService = new Mock<IBackupService>();
		backupService.Setup(b => b.ObtenerTamanoBaseDatosBytes()).Returns(123456L);

		var handler = new GetSaludSistemaQueryHandler(ctx, backupService.Object);
		var result = await handler.Handle(new GetSaludSistemaQuery(), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(2, result.Value!.TotalClientes);
		Assert.Equal(1, result.Value.ClientesActivos);
		Assert.Equal(2, result.Value.TotalFincas);
		Assert.Equal(2, result.Value.TotalPersonas);
		Assert.Equal(123456L, result.Value.TamanoBaseDatosBytes);
		Assert.Equal(2, result.Value.Clientes.Count);
		Assert.Single(result.Value.BitacoraReciente);
		Assert.Equal("Cliente 1", result.Value.BitacoraReciente[0].ClienteRazonSocial);
	}
}
