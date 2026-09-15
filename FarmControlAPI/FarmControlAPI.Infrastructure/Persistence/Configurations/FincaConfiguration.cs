using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class FincaConfiguration : IEntityTypeConfiguration<Finca>
{
	public void Configure(EntityTypeBuilder<Finca> builder)
	{
		builder.Property(f => f.AreaTotal).HasColumnType("decimal(18,4)");
		builder.Property(f => f.CostoTerreno).HasColumnType("decimal(18,2)");

		builder.HasOne(f => f.Cliente)
			.WithMany(c => c.Fincas)
			.HasForeignKey(f => f.ClienteId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
