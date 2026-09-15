namespace FarmControlAPI.Application.Common;

public record BackupInfo(string NombreArchivo, DateTime Fecha, long TamanoBytes);

public interface IBackupService
{
	Task<BackupInfo> CrearBackupAsync(CancellationToken cancellationToken = default);
	List<BackupInfo> ListarBackups();
	string? ObtenerRutaBackup(string nombreArchivo);
	long ObtenerTamanoBaseDatosBytes();
}
