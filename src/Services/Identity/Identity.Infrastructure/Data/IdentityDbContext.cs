using Identity.Application.Abstractions;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Data;

public class IdentityDbContext : DbContext, IUnitOfWork
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd();
            
            builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
            builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(72).IsRequired();
            builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            builder.Property(u => u.CreatedAt).HasColumnName("created_at");
            builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
