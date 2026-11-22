using BurnOffTheFat.Common.Models.Dto;

namespace BurnOffTheFat.Common.Interfaces;
/// <summary>
/// Репозиторий для профайла пользователя
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>
    /// Получение по идентификатору
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<UserProfile?> GetByIdAsync(Guid id);
    /// <summary>
    /// Получение по логину
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    Task<UserProfile?> GetByUserNameAsync(string userName);
    /// <summary>
    /// Получение всех пользователей
    /// </summary>
    /// <returns></returns>
    Task<List<UserProfile>> GetAllAsync();
    /// <summary>
    /// Создание или обновление пользователя
    /// </summary>
    /// <param name="profile"></param>
    /// <returns></returns>
    Task SaveAsync(UserProfile profile);
    /// <summary>
    /// Удаление пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task DeleteAsync(Guid id);
}
