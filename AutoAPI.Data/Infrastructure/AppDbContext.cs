using AutoAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AutoAPI.Data.Infrastructure
{
    public partial class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Narmin> Narmins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}