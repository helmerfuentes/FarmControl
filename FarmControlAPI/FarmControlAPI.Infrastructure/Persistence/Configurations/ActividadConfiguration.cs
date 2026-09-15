using FarmControlAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmControlAPI.Infrastructure.Persistence.Configurations;

public class ActividadConfiguration : IEntityTypeConfiguration<Actividad>
{
	public void Configure(EntityTypeBuilder<Actividad> builder)
	{
		builder.HasOne(a => a.ProcesoCultivo)
			.WithMany()
			.HasForeignKey(a => a.ProcesoCultivoId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.Property(a => a.ValorDiaUsado).HasColumnType("decimal(18,2)");
	}
}
