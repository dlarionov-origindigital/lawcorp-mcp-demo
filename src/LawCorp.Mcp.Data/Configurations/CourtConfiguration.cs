using LawCorp.Mcp.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawCorp.Mcp.Data.Configurations;

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        // Reference data with explicit stable IDs — not auto-generated
        builder.HasKey(e => e.Id)
            .IsClustered();

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Jurisdiction)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Address)
            .HasMaxLength(512);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(50);
    }
}
