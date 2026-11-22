using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Interfaces;
using BurnOffTheFat.Common.Models.Dto;

namespace BurnOffTheFat.Core.Services;

/// <summary>
/// Сервис для работы с БД текущего пользователя приложения
/// </summary>
public class CurrentUserService
{
    private const string CurrentUserIdKey = "current_user_profile_id";
    private readonly IUserProfileRepository _profileRepository;
    /// <summary>
    /// Текущий пользователь
    /// </summary>
    public UserProfile? CurrentUser { get; private set; }

    public CurrentUserService(IUserProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    /// <summary>
    /// Загружает профиль при старте или создает дефолтный при первом запуске
    /// </summary>
    public async Task<UserProfile> InitializeAsync()
    {
        string savedIdStr = Preferences.Default.Get(CurrentUserIdKey, string.Empty);

        if (Guid.TryParse(savedIdStr, out Guid savedId))
        {
            var existing = await _profileRepository.GetByIdAsync(savedId);
            if (existing != null)
            {
                CurrentUser = existing;
                return CurrentUser;
            }
        }

        // Первый запуск: создаем профиль по умолчанию
        var newProfile = new UserProfile
        {
            UserName = $"Пользователь{DateTimeOffset.Now.ToUnixTimeSeconds()}",
            FastTreiningMode = SpeedMode.Running
        };

        await _profileRepository.SaveAsync(newProfile);
        Preferences.Default.Set(CurrentUserIdKey, newProfile.Id.ToString());

        CurrentUser = newProfile;
        return CurrentUser;
    }

    /// <summary>
    /// Обновляет режим быстрой тренировки в БД и в памяти
    /// </summary>
    public async Task UpdateFastTrainingModeAsync(SpeedMode newMode)
    {
        if (CurrentUser == null || CurrentUser.FastTreiningMode == newMode)
            return;

        CurrentUser.FastTreiningMode = newMode;
        await _profileRepository.SaveAsync(CurrentUser);
    }
}
