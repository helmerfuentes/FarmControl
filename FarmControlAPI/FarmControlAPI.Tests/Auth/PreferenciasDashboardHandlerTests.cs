using FarmControlAPI.Application.Auth;
using Xunit;

namespace FarmControlAPI.Tests.Auth;

public class PreferenciasDashboardHandlerTests
{
	[Fact]
	public async Task Handle_GuardarYLeer_HaceRoundtrip()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: 1);
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var cliente = new FarmControlAPI.Domain.Entities.Cliente { RazonSocial = "Cliente 1", NIT = "1", Email = "a@a.com", FechaAlta = DateTime.UtcNow };
		ctx.Clientes.Add(cliente);
		ctx.SaveChanges();
		var persona = new FarmControlAPI.Domain.Entities.Persona
		{
			Id = 1,
			ClienteId = cliente.Id,
			Nombre = "Admin 1",
			Documento = "1",
			Telefono = "1",
			TipoPersona = FarmControlAPI.Domain.Enums.TipoPersona.Admin
		};
		ctx.Personas.Add(persona);
		ctx.SaveChanges();

		var guardarHandler = new GuardarPreferenciasDashboardCommandHandler(ctx, currentUser.Object);
		var guardarResult = await guardarHandler.Handle(new GuardarPreferenciasDashboardCommand("[\"ingresos-mes\",\"alertas-insumos\"]"), CancellationToken.None);

		Assert.True(guardarResult.IsSuccess);

		var leerHandler = new GetPreferenciasDashboardQueryHandler(ctx, currentUser.Object);
		var leerResult = await leerHandler.Handle(new GetPreferenciasDashboardQuery(), CancellationToken.None);

		Assert.True(leerResult.IsSuccess);
		Assert.Equal("[\"ingresos-mes\",\"alertas-insumos\"]", leerResult.Value!.PreferenciasJson);
	}

	[Fact]
	public async Task Handle_SinPersonaId_RetornaFallo()
	{
		var currentUser = TestDbContextFactory.CreateCurrentUserMock(personaId: null);
		var ctx = TestDbContextFactory.Create(currentUser.Object);
		var handler = new GuardarPreferenciasDashboardCommandHandler(ctx, currentUser.Object);

		var result = await handler.Handle(new GuardarPreferenciasDashboardCommand("[]"), CancellationToken.None);

		Assert.False(result.IsSuccess);
	}
}
