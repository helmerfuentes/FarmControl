using Npgsql;

namespace FarmControlAPI.Infrastructure.Extensions;

public static class PostgresConnectionStringHelper
{
	public static string Normalize(string connectionString)
	{
		string result;

		if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
			|| connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
		{
			var uri = new Uri(connectionString);
			var credenciales = uri.UserInfo.Split(':', 2);
			var builder = new NpgsqlConnectionStringBuilder
			{
				Host = uri.Host,
				Port = uri.Port > 0 ? uri.Port : 5432,
				Username = Uri.UnescapeDataString(credenciales[0]),
				Password = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : string.Empty,
				Database = uri.AbsolutePath.TrimStart('/'),
				SslMode = SslMode.Require
			};
			result = builder.ConnectionString;
		}
		else
		{
			result = connectionString;
		}

		return result;
	}
}
