using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Data;

public class SmartWmsDbContext : DbContext {
    public SmartWmsDbContext(
        DbContextOptions<SmartWmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity => {
            entity.ToTable(
                "Products",
                table => table.HasCheckConstraint( // 체크 제약 마이너스 확인
                    "CK_Products_StockQuantity",
                    "[StockQuantity] >= 0"));

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(x => x.Code) // 유니크 제약
                .IsUnique();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.StockQuantity)
                .IsRequired();
        });
    }
}