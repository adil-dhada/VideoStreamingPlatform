using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using VideoManagement.Application.Abstractions;
using VideoManagement.Domain;

namespace VideoManagement.Infrastructure.Data;

public class VideoManagementDbContext : DbContext, IUnitOfWork
{
    public VideoManagementDbContext(DbContextOptions<VideoManagementDbContext> options) : base(options) { }

    public DbSet<Video> Videos => Set<Video>();
    public DbSet<VideoVariant> VideoVariants => Set<VideoVariant>();
    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("video_mgmt");

        modelBuilder.Entity<Video>(b =>
        {
            b.ToTable("videos");
            b.HasKey(v => v.Id);
            b.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(v => v.OwnerId).HasColumnName("owner_id");
            b.Property(v => v.Title).HasColumnName("title").HasMaxLength(200);
            b.Property(v => v.Description).HasColumnName("description");
            b.Property(v => v.Status).HasColumnName("status").HasConversion<string>();
            b.Property(v => v.Visibility).HasColumnName("visibility").HasConversion<string>();
            b.Property(v => v.FileSizeBytes).HasColumnName("file_size_bytes");
            b.Property(v => v.DurationSeconds).HasColumnName("duration_seconds");
            b.Property(v => v.RawFilePath).HasColumnName("raw_file_path").HasMaxLength(1000);
            b.Property(v => v.TranscodingError).HasColumnName("transcoding_error");
            b.Property(v => v.CreatedAt).HasColumnName("created_at");
            b.Property(v => v.UpdatedAt).HasColumnName("updated_at");
            b.Property(v => v.PublishedAt).HasColumnName("published_at");

            b.HasMany(v => v.Variants)
             .WithOne()
             .HasForeignKey(vv => vv.VideoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VideoVariant>(b =>
        {
            b.ToTable("video_variants");
            b.HasKey(vv => vv.Id);
            b.Property(vv => vv.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(vv => vv.VideoId).HasColumnName("video_id");
            b.Property(vv => vv.Resolution).HasColumnName("resolution").HasConversion<string>();
            b.Property(vv => vv.BitrateKbps).HasColumnName("bitrate_kbps");
            b.Property(vv => vv.Width).HasColumnName("width");
            b.Property(vv => vv.Height).HasColumnName("height");
            b.Property(vv => vv.ManifestPath).HasColumnName("manifest_path").HasMaxLength(1000);
            b.Property(vv => vv.SegmentDirectory).HasColumnName("segment_directory").HasMaxLength(1000);
            b.Property(vv => vv.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<UploadSession>(b =>
        {
            b.ToTable("upload_sessions");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(s => s.UserId).HasColumnName("user_id");
            b.Property(s => s.FileName).HasColumnName("file_name").HasMaxLength(500);
            b.Property(s => s.TotalFileSizeBytes).HasColumnName("total_file_size");
            b.Property(s => s.TotalChunks).HasColumnName("total_chunks");
            b.Property(s => s.ChunkSizeBytes).HasColumnName("chunk_size_bytes");
            b.Property(s => s.ReceivedChunksJson).HasColumnName("received_chunks");
            b.Property(s => s.Status).HasColumnName("status").HasConversion<string>();
            b.Property(s => s.TempDirectory).HasColumnName("temp_directory").HasMaxLength(1000);
            b.Property(s => s.CreatedAt).HasColumnName("created_at");
            b.Property(s => s.ExpiresAt).HasColumnName("expires_at");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
