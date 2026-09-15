namespace FarmControlAPI.Application.Common;

public interface IBitacoraService
{
	/// <summary>
	/// Registra una entrada de bitácora. Si <paramref name="clienteId"/> no se indica, se usa el
	/// cliente del usuario actual — pásalo explícitamente cuando la acción la ejecuta el SuperAdmin
	/// sobre un cliente puntual (ej. alta de cliente, creación de su primer usuario), para que la
	/// entrada quede visible en la bitácora de ese cliente y no huérfana bajo un ClienteId nulo.
	/// </summary>
	Task RegistrarAsync(string accion, string detalle, int? clienteId = null, CancellationToken cancellationToken = default);
}
