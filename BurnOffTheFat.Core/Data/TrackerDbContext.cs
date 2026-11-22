using BurnOffTheFat.Common.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace BurnOffTheFat.Core.Data;
/// <summary>
/// Контекст базы данных для приложения
/// </summary>
public class TrackerDbContext : DbContext
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<TrackPoint> TrackPoints => Set<TrackPoint>();

    public TrackerDbContext(DbContextOptions<TrackerDbContext> options) : base(options)
    {
        // Создаем БД при первом обращении и включаем режим WAL
        Database.EnsureCreated();
        Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }
    ///<inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserProfile>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<UserProfile>()
            .HasMany(u => u.Tracks)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Индексы для быстрой фильтрации по пользователю и выгрузки точек трека
        modelBuilder.Entity<Track>()
            .HasIndex(t => t.UserId);

        modelBuilder.Entity<TrackPoint>()
            .HasIndex(p => p.TrackId);

        // Каскадное удаление точек при удалении трека
        modelBuilder.Entity<Track>()
            .HasMany(t => t.Points)
            .WithOne()
            .HasForeignKey(p => p.TrackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}