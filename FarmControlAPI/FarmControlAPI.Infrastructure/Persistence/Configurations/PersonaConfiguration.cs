using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
	public void Configure(EntityTypeBuilder<Persona> builder)
	{
		builder.HasOne(p => p.Cliente)
			.WithMany()
			.HasForeignKey(p => p.ClienteId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
