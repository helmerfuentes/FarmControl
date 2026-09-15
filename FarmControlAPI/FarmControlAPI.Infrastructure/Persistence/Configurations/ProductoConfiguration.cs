using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
	public void Configure(EntityTypeBuilder<Producto> builder)
	{
		builder.HasOne(p => p.Cliente)
			.WithMany(c => c.Productos)
			.HasForeignKey(p => p.ClienteId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
