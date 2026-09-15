using FarmControlAPI.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FarmControlAPI.Infrastructure.Services;

public class BackupBackgroundService : BackgroundService
{
	private static readonly TimeSpan _INTERVALO = TimeSpan.FromHours(24);

	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<BackupBackgroundService> _logger;

	public BackupBackgroundService(IServiceScopeFactory scopeFactory, ILogger<BackupBackgroundService> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(_INTERVALO);

		do
		{
			try
			{
				using var scope = _scopeFactory.CreateScope();
				var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
				await backupService.CrearBackupAsync(stoppingToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "Falló el backup automático programado.");
			}
		}
		while (await timer.WaitForNextTickAsync(stoppingToken));
	}
}
