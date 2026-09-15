using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class HistorialPrecioVentaConfiguration : IEntityTypeConfiguration<HistorialPrecioVenta>
{
	public void Configure(EntityTypeBuilder<HistorialPrecioVenta> builder)
	{
		builder.HasOne(h => h.Producto)
			.WithMany()
			.HasForeignKey(h => h.ProductoId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(h => h.Venta)
			.WithMany()
			.HasForeignKey(h => h.VentaId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(h => h.PrecioUnitario).HasColumnType("decimal(18,2)");
		builder.Property(h => h.Clasificacion).HasMaxLength(100);
	}
}
