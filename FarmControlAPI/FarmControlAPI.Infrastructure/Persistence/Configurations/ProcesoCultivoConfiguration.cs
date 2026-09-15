using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class ProcesoCultivoConfiguration : IEntityTypeConfiguration<ProcesoCultivo>
{
	public void Configure(EntityTypeBuilder<ProcesoCultivo> builder)
	{
		builder.HasOne(pc => pc.Parcela)
			.WithMany(p => p.ProcesosCultivo)
			.HasForeignKey(pc => pc.ParcelaId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(pc => pc.Producto)
			.WithMany()
			.HasForeignKey(pc => pc.ProductoId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(pc => pc.Socio)
			.WithMany()
			.HasForeignKey(pc => pc.SocioId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.Property(pc => pc.CostoInicial).HasColumnType("decimal(18,2)");
	}
}
