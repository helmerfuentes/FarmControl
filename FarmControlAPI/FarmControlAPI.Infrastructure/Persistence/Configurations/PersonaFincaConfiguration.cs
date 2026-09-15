using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class PersonaFincaConfiguration : IEntityTypeConfiguration<PersonaFinca>
{
	public void Configure(EntityTypeBuilder<PersonaFinca> builder)
	{
		builder.HasIndex(pf => new { pf.PersonaId, pf.FincaId }).IsUnique();

		builder.HasOne(pf => pf.Persona)
			.WithMany(p => p.AsignacionesFinca)
			.HasForeignKey(pf => pf.PersonaId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(pf => pf.Finca)
			.WithMany(f => f.AsignacionesPersona)
			.HasForeignKey(pf => pf.FincaId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
