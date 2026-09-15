using FarmControlAPI.Application.Common;

namespace FarmControlAPI.Endpoints;

internal static class BackupsEndpoints
{
	internal static void MapBackupsEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/backups").RequireAuthorization("SuperAdmin");

		group.MapGet("/", (IBackupService backupService) =>
		{
			var backups = backupService.ListarBackups();
			return Results.Ok(backups);
		});

		group.MapPost("/", async (IBackupService backupService) =>
		{
			var info = await backupService.CrearBackupAsync();
			return Results.Ok(info);
		});

		group.MapGet("/{nombreArchivo}/descargar", (string nombreArchivo, IBackupService backupService) =>
		{
			var ruta = backupService.ObtenerRutaBackup(nombreArchivo);
			return ruta is null
				? Results.NotFound(new { error = "Backup no encontrado." })
				: Results.File(ruta, "application/octet-stream", nombreArchivo);
		});
	}
}
