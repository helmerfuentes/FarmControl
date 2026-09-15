using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class TipoInsumoConfiguration : IEntityTypeConfiguration<TipoInsumo>
{
	public void Configure(EntityTypeBuilder<TipoInsumo> builder)
	{
		builder.HasOne(t => t.Cliente)
			.WithMany(c => c.TiposInsumo)
			.HasForeignKey(t => t.ClienteId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
