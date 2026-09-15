namespace FarmControlAPI.Application.Auth;

public interface ITokenService
{
	string GenerateToken(
		string usuario,
		string rol,
		int? personaId = null,
		int? clienteId = null,
		IReadOnlyList<int>? fincaIds = null,
		bool personaActiva = true,
		IReadOnlyList<int>? fincaIdsSoloLectura = null,
		IReadOnlyList<int>? fincaIdsSinEliminar = null);
}
