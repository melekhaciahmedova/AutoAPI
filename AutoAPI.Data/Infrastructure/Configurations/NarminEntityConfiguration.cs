using AutoAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoAPI.Data.Infrastructure.Configurations
{
    public class NarminConfiguration : IEntityTypeConfiguration<Narmin>
    {
        public void Configure(EntityTypeBuilder<Narmin> builder)
        {   
            builder.HasKey(x => x.Id);


            builder.Property(x => x.Id).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Price).HasColumnType("decimal(18,2)").IsRequired();
        }
    }
}