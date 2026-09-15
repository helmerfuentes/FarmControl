using FarmControlAPI.Application.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace FarmControlAPI.Infrastructure.Services;

public class BackupService : IBackupService
{
	private const int _MAX_BACKUPS_A_CONSERVAR = 30;
	private const string _CARPETA_BACKUPS = "Backups";
	private const string _PREFIJO_ARCHIVO = "farmcontrol-backup-";

	private readonly string _connectionString;
	private readonly string _carpetaBackups;

	public BackupService(IConfiguration configuration)
	{
		_connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=farmcontrol.db";
		_carpetaBackups = Path.Combine(Directory.GetCurrentDirectory(), _CARPETA_BACKUPS);
	}

	public async Task<BackupInfo> CrearBackupAsync(CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(_carpetaBackups);

		var nombreArchivo = $"{_PREFIJO_ARCHIVO}{DateTime.UtcNow:yyyyMMdd-HHmmss}.db";
		var rutaDestino = Path.Combine(_carpetaBackups, nombreArchivo);

		await using (var conexion = new SqliteConnection(_connectionString))
		{
			await conexion.OpenAsync(cancellationToken);
			await using var comando = conexion.CreateCommand();
			comando.CommandText = "VACUUM INTO $ruta";
			comando.Parameters.AddWithValue("$ruta", rutaDestino);
			await comando.ExecuteNonQueryAsync(cancellationToken);
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

		return Directory.GetFiles(_carpetaBackups, $"{_PREFIJO_ARCHIVO}*.db")
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
		var dataSource = new SqliteConnectionStringBuilder(_connectionString).DataSource;
		var ruta = Path.IsPathRooted(dataSource) ? dataSource : Path.Combine(Directory.GetCurrentDirectory(), dataSource);
		return File.Exists(ruta) ? new FileInfo(ruta).Length : 0;
	}

	private void PurgarBackupsAntiguos()
	{
		var backups = Directory.GetFiles(_carpetaBackups, $"{_PREFIJO_ARCHIVO}*.db")
			.Select(ruta => new FileInfo(ruta))
			.OrderByDescending(f => f.LastWriteTimeUtc)
			.ToList();

		foreach (var viejo in backups.Skip(_MAX_BACKUPS_A_CONSERVAR))
		{
			viejo.Delete();
		}
	}
}
