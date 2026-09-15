using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
	public void Configure(EntityTypeBuilder<Venta> builder)
	{
		builder.HasOne(v => v.Comprador)
			.WithMany(p => p.Ventas)
			.HasForeignKey(v => v.CompradorId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(v => v.ProcesoCultivo)
			.WithMany()
			.HasForeignKey(v => v.ProcesoCultivoId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.Property(v => v.Total).HasColumnType("decimal(18,2)");
		builder.Property(v => v.ValorTransporte).HasColumnType("decimal(18,2)");
	}
}
