using FarmControlAPI.Application.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FarmControlAPI.Infrastructure.Services;

public class TokenService : ITokenService
{
	private const int _EXPIRATION_HOURS = 8;
	private const string _PERSONA_ID_CLAIM = "personaId";
	private const string _CLIENTE_ID_CLAIM = "clienteId";
	private const string _FINCA_IDS_CLAIM = "fincaIds";
	private const string _PERSONA_ACTIVA_CLAIM = "personaActiva";
	private const string _FINCA_IDS_SOLO_LECTURA_CLAIM = "fincaIdsSoloLectura";
	private const string _FINCA_IDS_SIN_ELIMINAR_CLAIM = "fincaIdsSinEliminar";
	private const char _FINCA_IDS_SEPARATOR = ',';

	private readonly IConfiguration _configuration;

	public TokenService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string GenerateToken(
		string usuario,
		string rol,
		int? personaId = null,
		int? clienteId = null,
		IReadOnlyList<int>? fincaIds = null,
		bool personaActiva = true,
		IReadOnlyList<int>? fincaIdsSoloLectura = null,
		IReadOnlyList<int>? fincaIdsSinEliminar = null)
	{
		var key = _configuration["Jwt:Key"] ?? "FarmControlSecretKey_ChangeInProduction_2024!";
		var issuer = _configuration["Jwt:Issuer"] ?? "FarmControlAPI";
		var audience = _configuration["Jwt:Audience"] ?? "FarmControlWeb";

		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.Name, usuario),
			new Claim(ClaimTypes.Role, rol)
		};

		if (personaId.HasValue)
		{
			claims.Add(new Claim(_PERSONA_ID_CLAIM, personaId.Value.ToString()));
		}

		if (clienteId.HasValue)
		{
			claims.Add(new Claim(_CLIENTE_ID_CLAIM, clienteId.Value.ToString()));
		}

		if (fincaIds is { Count: > 0 })
		{
			claims.Add(new Claim(_FINCA_IDS_CLAIM, string.Join(_FINCA_IDS_SEPARATOR, fincaIds)));
		}

		if (personaId.HasValue && !personaActiva)
		{
			claims.Add(new Claim(_PERSONA_ACTIVA_CLAIM, "false"));
		}

		if (fincaIdsSoloLectura is { Count: > 0 })
		{
			claims.Add(new Claim(_FINCA_IDS_SOLO_LECTURA_CLAIM, string.Join(_FINCA_IDS_SEPARATOR, fincaIdsSoloLectura)));
		}

		if (fincaIdsSinEliminar is { Count: > 0 })
		{
			claims.Add(new Claim(_FINCA_IDS_SIN_ELIMINAR_CLAIM, string.Join(_FINCA_IDS_SEPARATOR, fincaIdsSinEliminar)));
		}

		var token = new JwtSecurityToken(
			issuer: issuer,
			audience: audience,
			claims: claims,
			expires: DateTime.UtcNow.AddHours(_EXPIRATION_HOURS),
			signingCredentials: credentials);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
