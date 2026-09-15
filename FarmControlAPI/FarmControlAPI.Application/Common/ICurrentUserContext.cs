namespace FarmControlAPI.Application.Common;

public interface ICurrentUserContext
{
	bool IsSuperAdmin { get; }
	bool TieneAccesoGlobal { get; }
	int? ClienteId { get; }
	IReadOnlyList<int> FincaIds { get; }
	IReadOnlyList<int> FincaIdsSoloLectura { get; }
	IReadOnlyList<int> FincaIdsSinEliminar { get; }
	bool PersonaActiva { get; }
	int? PersonaId { get; }
	bool TieneAccesoAFinca(int fincaId);
	bool PuedeEscribirEnFinca(int fincaId);
	bool PuedeEliminarEnFinca(int fincaId);
}
