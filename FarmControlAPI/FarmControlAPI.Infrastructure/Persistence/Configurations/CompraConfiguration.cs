using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
	public void Configure(EntityTypeBuilder<Compra> builder)
	{
		builder.HasOne(c => c.Parcela)
			.WithMany()
			.HasForeignKey(c => c.ParcelaId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(c => c.ProcesoCultivo)
			.WithMany()
			.HasForeignKey(c => c.ProcesoCultivoId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.Property(c => c.Valor).HasColumnType("decimal(18,2)");
	}
}
