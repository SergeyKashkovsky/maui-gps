using BurnOffTheFat.Common.Interfaces;
using BurnOffTheFat.Common.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace BurnOffTheFat.Core.Data;

/// <summary>
/// Репозиторий для работы с данными профайла пользователя
/// </summary>
public class SqliteUserProfileRepository : IUserProfileRepository
{
    private readonly IDbContextFactory<TrackerDbContext> _contextFactory;

    public SqliteUserProfileRepository(IDbContextFactory<TrackerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    /// <inheritdoc/> 
    public async Task<UserProfile?> GetByIdAsync(Guid id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }
    /// <inheritdoc/>
    public async Task<UserProfile?> GetByUserNameAsync(string userName)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName);
    }
    /// <inheritdoc/>
    public async Task<List<UserProfile>> GetAllAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.UserProfiles.AsNoTracking().OrderBy(u => u.UserName).ToListAsync();
    }
    /// <inheritdoc/>
    public async Task SaveAsync(UserProfile profile)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        profile.UpdatedAt = DateTime.UtcNow;
        profile.IsSynced = false;

        bool exists = await db.UserProfiles.AnyAsync(u => u.Id == profile.Id);
        if (exists)
            db.UserProfiles.Update(profile);
        else
            db.UserProfiles.Add(profile);

        await db.SaveChangesAsync();
    }
    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        await db.UserProfiles.Where(u => u.Id == id).ExecuteDeleteAsync();
    }
}
