using FarmControlAPI.Application.Common;
using FarmControlAPI.Endpoints;
using FarmControlAPI.Infrastructure.Extensions;
using FarmControlAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "FarmControlSecretKey_ChangeInProduction_2024!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FarmControlAPI",
			ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FarmControlWeb",
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
		};
	});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
	options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
});

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
		policy.WithOrigins("http://localhost:4200")
			.AllowAnyHeader()
			.AllowAnyMethod());
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<FarmControlDbContext>();
	await dbContext.Database.MigrateAsync();
	await FarmControlDbContextSeed.SeedAsync(dbContext);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapClientesEndpoints();
app.MapTiposInsumoEndpoints();
app.MapProductosEndpoints();
app.MapPersonasEndpoints();
app.MapFincasEndpoints();
app.MapInsumosEndpoints();
app.MapActividadesEndpoints();
app.MapComprasEndpoints();
app.MapVentasEndpoints();
app.MapProcesosCultivoEndpoints();
app.MapSocioEndpoints();
app.MapReportesEndpoints();
app.MapBitacoraEndpoints();
app.MapNotificacionesEndpoints();
app.MapTareasEndpoints();
app.MapBackupsEndpoints();
app.MapSistemaEndpoints();
app.MapAsistenciasEndpoints();
app.MapNominaEndpoints();
app.MapAnalisisSueloEndpoints();
app.MapPlanesEndpoints();
app.MapComentariosEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
