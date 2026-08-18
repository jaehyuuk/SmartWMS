using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Data;

/// <summary>
/// SmartWMS의 Entity 및 DB 제약조건을 설정하는 DbContext
/// </summary>
public class SmartWmsDbContext : DbContext {

    public SmartWmsDbContext(DbContextOptions<SmartWmsDbContext> options) : base(options) {}

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inbound> Inbounds => Set<Inbound>();
    public DbSet<Outbound> Outbounds => Set<Outbound>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        // 상품
        modelBuilder.Entity<Product>(entity => {
            entity.ToTable(
                "Products",
                table => table.HasCheckConstraint(
                    "CK_Products_StockQuantity",
                    "[StockQuantity] >= 0"));

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(30)
                .IsRequired();

            // 상품 코드는 중복될 수 없음
            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.StockQuantity)
                .IsRequired();
        });

        // 입고 이력
        modelBuilder.Entity<Inbound>(entity => {
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

            // 입고 이력이 있는 상품의 삭제를 제한
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 출고 이력
        modelBuilder.Entity<Outbound>(entity => {
            entity.ToTable(
                "Outbounds",
                table => table.HasCheckConstraint(
                    "CK_Outbounds_Quantity",
                    "[Quantity] > 0"));

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Quantity)
                .IsRequired();

            entity.Property(x => x.OutboundDate)
                .IsRequired();

            entity.Property(x => x.Memo)
                .HasMaxLength(200);

            // 출고 이력이 있는 상품의 삭제를 제한
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 사용자
        modelBuilder.Entity<User>(entity => {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .HasMaxLength(50)
                .IsRequired();

            // 로그인 아이디는 중복될 수 없음
            entity.HasIndex(x => x.UserId)
                .IsUnique();

            entity.Property(x => x.PasswordHash)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Role)
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}
