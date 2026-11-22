using BurnOffTheFat.Common.Interfaces;
using BurnOffTheFat.Common.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace BurnOffTheFat.Core.Data;

/// <summary>
/// Репозиторий для работы с данными трека
/// </summary>
public class SqliteTrackRepository : ITrackRepository
{
    private readonly IDbContextFactory<TrackerDbContext> _contextFactory;

    public SqliteTrackRepository(IDbContextFactory<TrackerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    /// <inheritdoc/>
    public async Task<Track> CreateTrackAsync(Guid userId, string title)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var track = new Track
        {
            UserId = userId,
            Title = title,
            StartTime = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tracks.Add(track);
        await db.SaveChangesAsync();
        return track;
    }
    /// <inheritdoc/>
    public async Task AddPointsBatchAsync(IEnumerable<TrackPoint> points)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        // Отключаем трекинг для ускорения массовой вставки
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        await db.TrackPoints.AddRangeAsync(points);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task FinishTrackAsync(Guid trackId, double totalDistance, TimeSpan duration)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        await db.Tracks
            .Where(t => t.Id == trackId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.EndTime, DateTime.UtcNow)
                .SetProperty(t => t.TotalDistanceMeters, totalDistance)
                .SetProperty(t => t.Duration, duration)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow)
                .SetProperty(t => t.IsSynced, false));
    }

    /// <inheritdoc/>
    public async Task<List<Track>> GetUserTracksAsync(Guid userId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        // Загружаем только заголовки без точек!
        return await db.Tracks
            .AsNoTracking()
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Track?> GetTrackWithPointsAsync(Guid trackId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Tracks
            .AsNoTracking()
            .Include(t => t.Points)
            .FirstOrDefaultAsync(t => t.Id == trackId);
    }

    /// <inheritdoc/>
    public async Task<List<Track>> GetUnsyncedTracksAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Tracks
            .AsNoTracking()
            .Include(t => t.Points)
            .Where(t => !t.IsSynced)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task MarkAsSyncedAsync(Guid trackId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        await db.Tracks
            .Where(t => t.Id == trackId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsSynced, true));
    }

    /// <inheritdoc/>
    public async Task<Track?> GetUnfinishedTrackAsync(Guid userId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Tracks
            .AsNoTracking()
            .Include(t => t.Points)
            .FirstOrDefaultAsync(t => t.UserId == userId && t.EndTime == null && !t.IsDeleted);
    }

    /// <inheritdoc/>
    public async Task DeleteTrackAsync(Guid trackId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        await db.Tracks.Where(t => t.Id == trackId).ExecuteDeleteAsync();
    }
}
