using FarmControlAPI.Application.Auth;
using FarmControlAPI.Application.Common;
using FarmControlAPI.Infrastructure.Persistence;
using FarmControlAPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FarmControlAPI.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("DefaultConnection")
			?? "Host=localhost;Port=5432;Database=farmcontrol;Username=farmcontrol;Password=farmcontrol";

		services.AddDbContext<FarmControlDbContext>(options =>
			options.UseNpgsql(connectionString));

		services.AddScoped<IFarmControlDbContext>(provider =>
			provider.GetRequiredService<FarmControlDbContext>());

		services.AddScoped<ITokenService, TokenService>();

		services.AddHttpContextAccessor();
		services.AddScoped<ICurrentUserContext, CurrentUserContext>();
		services.AddScoped<IBitacoraService, BitacoraService>();
		services.AddSingleton<IBackupService, BackupService>();
		services.AddHostedService<BackupBackgroundService>();

		return services;
	}
}
