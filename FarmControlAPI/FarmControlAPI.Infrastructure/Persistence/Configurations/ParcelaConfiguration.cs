using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class ParcelaConfiguration : IEntityTypeConfiguration<Parcela>
{
	public void Configure(EntityTypeBuilder<Parcela> builder)
	{
		builder.Property(p => p.Area).HasColumnType("decimal(18,4)");
	}
}
