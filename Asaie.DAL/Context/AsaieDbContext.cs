using Asaie.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Asaie.DAL.Context;

public class AsaieDbContext : DbContext
{
    public AsaieDbContext(DbContextOptions<AsaieDbContext> options) : base(options) { }

    public DbSet<AgriRiskRecord> AgriRiskRecords => Set<AgriRiskRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension in PostgreSQL
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<AgriRiskRecord>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.MemberStateCode);
        });
    }
}