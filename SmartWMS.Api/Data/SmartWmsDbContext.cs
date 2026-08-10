using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Data;

/// <summary>
/// DB 제약 설정
/// </summary>
public class SmartWmsDbContext : DbContext {
    public SmartWmsDbContext(
        DbContextOptions<SmartWmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inbound> Inbounds => Set<Inbound>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        
        modelBuilder.Entity<Product>(entity => { // Product
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

        modelBuilder.Entity<Inbound>(entity => { // Inbound
            entity.ToTable(
                "Inbounds",
                table => table.HasCheckConstraint(
                    "CK_Inbounds_Quantity",
                    "[Quantity] > 0"));

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Quantity)
                .IsRequired();

            entity.Property(x => x.InboundDate)
                .IsRequired();

            entity.Property(x => x.Memo)
                .HasMaxLength(200);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // 연결 이력이 있는 경우 삭제 못하도록
        });
    }
}