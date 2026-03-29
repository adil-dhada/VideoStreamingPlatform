using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Transcoding.Application.Abstractions;
using Transcoding.Domain;

namespace Transcoding.Infrastructure.Data;

public class TranscodingDbContext : DbContext, IUnitOfWork
{
    public TranscodingDbContext(DbContextOptions<TranscodingDbContext> options) : base(options) { }

    public DbSet<TranscodingJob> TranscodingJobs => Set<TranscodingJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("transcoding");

        modelBuilder.Entity<TranscodingJob>(b =>
        {
            b.ToTable("transcoding_jobs");
            b.HasKey(j => j.Id);
            b.Property(j => j.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(j => j.VideoId).HasColumnName("video_id");
            b.Property(j => j.Status).HasColumnName("status").HasConversion<string>();
            b.Property(j => j.ErrorMessage).HasColumnName("error_message");
            b.Property(j => j.StartedAt).HasColumnName("started_at");
            b.Property(j => j.CompletedAt).HasColumnName("completed_at");
            b.Property(j => j.CreatedAt).HasColumnName("created_at");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
