using FarmControlAPI.Application.Common;
using Microsoft.AspNetCore.Http;

namespace FarmControlAPI.Infrastructure.Services;

public class CurrentUserContext : ICurrentUserContext
{
	private const string _ROL_SUPER_ADMIN = "SuperAdmin";
	private const string _ROL_AUDITOR = "Auditor";
	private const string _CLIENTE_ID_CLAIM = "clienteId";
	private const string _FINCA_IDS_CLAIM = "fincaIds";
	private const string _FINCA_IDS_SOLO_LECTURA_CLAIM = "fincaIdsSoloLectura";
	private const string _FINCA_IDS_SIN_ELIMINAR_CLAIM = "fincaIdsSinEliminar";
	private const string _PERSONA_ACTIVA_CLAIM = "personaActiva";
	private const string _PERSONA_ID_CLAIM = "personaId";
	private const char _FINCA_IDS_SEPARATOR = ',';

	private readonly System.Security.Claims.ClaimsPrincipal? _usuario;
	private readonly Lazy<IReadOnlyList<int>> _fincaIds;
	private readonly Lazy<IReadOnlyList<int>> _fincaIdsSoloLectura;
	private readonly Lazy<IReadOnlyList<int>> _fincaIdsSinEliminar;

	public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
	{
		_usuario = httpContextAccessor.HttpContext?.User;
		_fincaIds = new Lazy<IReadOnlyList<int>>(() => ParseFincaIdsClaim(_FINCA_IDS_CLAIM));
		_fincaIdsSoloLectura = new Lazy<IReadOnlyList<int>>(() => ParseFincaIdsClaim(_FINCA_IDS_SOLO_LECTURA_CLAIM));
		_fincaIdsSinEliminar = new Lazy<IReadOnlyList<int>>(() => ParseFincaIdsClaim(_FINCA_IDS_SIN_ELIMINAR_CLAIM));
	}

	public bool IsSuperAdmin => _usuario?.IsInRole(_ROL_SUPER_ADMIN) ?? false;

	public bool TieneAccesoGlobal => IsSuperAdmin || (_usuario?.IsInRole(_ROL_AUDITOR) ?? false);

	public int? ClienteId => ParseIntClaim(_CLIENTE_ID_CLAIM);

	public IReadOnlyList<int> FincaIds => _fincaIds.Value;

	public IReadOnlyList<int> FincaIdsSoloLectura => _fincaIdsSoloLectura.Value;

	public IReadOnlyList<int> FincaIdsSinEliminar => _fincaIdsSinEliminar.Value;

	public bool PersonaActiva => _usuario?.FindFirst(_PERSONA_ACTIVA_CLAIM)?.Value != "false";

	public int? PersonaId => ParseIntClaim(_PERSONA_ID_CLAIM);

	public bool TieneAccesoAFinca(int fincaId)
	{
		return IsSuperAdmin || FincaIds.Contains(fincaId);
	}

	public bool PuedeEscribirEnFinca(int fincaId)
	{
		return IsSuperAdmin || (TieneAccesoAFinca(fincaId) && PersonaActiva && !FincaIdsSoloLectura.Contains(fincaId));
	}

	public bool PuedeEliminarEnFinca(int fincaId)
	{
		return IsSuperAdmin || (PuedeEscribirEnFinca(fincaId) && !FincaIdsSinEliminar.Contains(fincaId));
	}

	private int? ParseIntClaim(string claimType)
	{
		int? valor;
		var claim = _usuario?.FindFirst(claimType)?.Value;
		if (int.TryParse(claim, out var parsed))
		{
			valor = parsed;
		}
		else
		{
			valor = null;
		}

		return valor;
	}

	private IReadOnlyList<int> ParseFincaIdsClaim(string claimType)
	{
		List<int> fincaIds;
		var claim = _usuario?.FindFirst(claimType)?.Value;

		if (string.IsNullOrWhiteSpace(claim))
		{
			fincaIds = [];
		}
		else
		{
			fincaIds = claim
				.Split(_FINCA_IDS_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
				.Select(int.Parse)
				.ToList();
		}

		return fincaIds;
	}
}
