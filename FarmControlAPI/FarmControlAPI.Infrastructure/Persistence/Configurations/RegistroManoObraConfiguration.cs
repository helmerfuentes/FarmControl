using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class RegistroManoObraConfiguration : IEntityTypeConfiguration<RegistroManoObra>
{
	public void Configure(EntityTypeBuilder<RegistroManoObra> builder)
	{
		// Dos FK hacia Persona — EF no puede inferirlas sin configuración explícita
		builder.HasOne(r => r.Jornalero)
			.WithMany(p => p.RegistrosComoJornalero)
			.HasForeignKey(r => r.JornaleroId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(r => r.Socio)
			.WithMany(p => p.RegistrosComoPagador)
			.HasForeignKey(r => r.SocioId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
