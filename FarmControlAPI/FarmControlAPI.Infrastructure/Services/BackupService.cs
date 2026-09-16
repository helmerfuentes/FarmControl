using System.Diagnostics;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FarmControlAPI.Infrastructure.Services;

public class BackupService : IBackupService
{
	private const int _MAX_BACKUPS_A_CONSERVAR = 30;
	private const string _CARPETA_BACKUPS = "Backups";
	private const string _PREFIJO_ARCHIVO = "farmcontrol-backup-";
	private const string _EXTENSION_ARCHIVO = ".sql";

	private readonly string _connectionString;
	private readonly string _carpetaBackups;

	public BackupService(IConfiguration configuration)
	{
		_connectionString = PostgresConnectionStringHelper.Normalize(
			configuration.GetConnectionString("DefaultConnection")
				?? "Host=localhost;Port=5432;Database=farmcontrol;Username=farmcontrol;Password=farmcontrol");
		_carpetaBackups = Path.Combine(Directory.GetCurrentDirectory(), _CARPETA_BACKUPS);
	}

	public async Task<BackupInfo> CrearBackupAsync(CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(_carpetaBackups);

		var nombreArchivo = $"{_PREFIJO_ARCHIVO}{DateTime.UtcNow:yyyyMMdd-HHmmss}{_EXTENSION_ARCHIVO}";
		var rutaDestino = Path.Combine(_carpetaBackups, nombreArchivo);
		var builder = new NpgsqlConnectionStringBuilder(_connectionString);

		var proceso = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "pg_dump",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			}
		};
		proceso.StartInfo.ArgumentList.Add($"--host={builder.Host}");
		proceso.StartInfo.ArgumentList.Add($"--port={builder.Port}");
		proceso.StartInfo.ArgumentList.Add($"--username={builder.Username}");
		proceso.StartInfo.ArgumentList.Add($"--dbname={builder.Database}");
		proceso.StartInfo.ArgumentList.Add("--no-password");
		proceso.StartInfo.EnvironmentVariables["PGPASSWORD"] = builder.Password;

		proceso.Start();
		await using (var archivoDestino = File.Create(rutaDestino))
		{
			await proceso.StandardOutput.BaseStream.CopyToAsync(archivoDestino, cancellationToken);
		}
		await proceso.WaitForExitAsync(cancellationToken);

		if (proceso.ExitCode != 0)
		{
			var error = await proceso.StandardError.ReadToEndAsync(cancellationToken);
			File.Delete(rutaDestino);
			throw new InvalidOperationException($"pg_dump finalizó con código {proceso.ExitCode}: {error}");
		}

		PurgarBackupsAntiguos();

		var info = new FileInfo(rutaDestino);
		return new BackupInfo(nombreArchivo, info.LastWriteTimeUtc, info.Length);
	}

	public List<BackupInfo> ListarBackups()
	{
		if (!Directory.Exists(_carpetaBackups))
		{
			return [];
		}

		return Directory.GetFiles(_carpetaBackups, $"{_PREFIJO_ARCHIVO}*{_EXTENSION_ARCHIVO}")
			.Select(ruta => new FileInfo(ruta))
			.OrderByDescending(f => f.LastWriteTimeUtc)
			.Select(f => new BackupInfo(f.Name, f.LastWriteTimeUtc, f.Length))
			.ToList();
	}

	public string? ObtenerRutaBackup(string nombreArchivo)
	{
		string? ruta;

		if (nombreArchivo.Contains("..") || Path.IsPathRooted(nombreArchivo) || !nombreArchivo.StartsWith(_PREFIJO_ARCHIVO))
		{
			ruta = null;
		}
		else
		{
			var candidata = Path.GetFullPath(Path.Combine(_carpetaBackups, nombreArchivo));
			var carpetaCompleta = Path.GetFullPath(_carpetaBackups);
			ruta = candidata.StartsWith(carpetaCompleta) && File.Exists(candidata) ? candidata : null;
		}

		return ruta;
	}

	public long ObtenerTamanoBaseDatosBytes()
	{
		using var conexion = new NpgsqlConnection(_connectionString);
		conexion.Open();
		using var comando = conexion.CreateCommand();
		comando.CommandText = "SELECT pg_database_size(current_database())";
		var resultado = comando.ExecuteScalar();
		return resultado is long tamano ? tamano : 0;
	}

	private void PurgarBackupsAntiguos()
	{
		var backups = Directory.GetFiles(_carpetaBackups, $"{_PREFIJO_ARCHIVO}*{_EXTENSION_ARCHIVO}")
			.Select(ruta => new FileInfo(ruta))
			.OrderByDescending(f => f.LastWriteTimeUtc)
			.ToList();

		foreach (var viejo in backups.Skip(_MAX_BACKUPS_A_CONSERVAR))
		{
			viejo.Delete();
		}
	}
}
