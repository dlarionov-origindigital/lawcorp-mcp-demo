using LawCorp.Mcp.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawCorp.Mcp.Data.Configurations;

public class PracticeGroupConfiguration : IEntityTypeConfiguration<PracticeGroup>
{
    public void Configure(EntityTypeBuilder<PracticeGroup> builder)
    {
        // Reference data with explicit stable IDs — not auto-generated
        builder.HasKey(e => e.Id)
            .IsClustered();

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasMaxLength(1024);
    }
}
