using BurnOffTheFat.Common.Models.Dto;

namespace BurnOffTheFat.Common.Interfaces;
/// <summary>
/// Репозиторий для работы с треками
/// </summary>
public interface ITrackRepository
{
    /// <summary>
    /// Создать трек
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="title"></param>
    /// <returns></returns>
    Task<Track> CreateTrackAsync(Guid userId, string title);
    /// <summary>
    /// Добавление пакета точек
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    Task AddPointsBatchAsync(IEnumerable<TrackPoint> points);
    /// <summary>
    /// Завершение трека
    /// </summary>
    /// <param name="trackId"></param>
    /// <param name="totalDistance"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    Task FinishTrackAsync(Guid trackId, double totalDistance, TimeSpan duration);
    /// <summary>
    /// Получение трека пользователя
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<List<Track>> GetUserTracksAsync(Guid userId);
    /// <summary>
    /// Получение трека с точками
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    Task<Track?> GetTrackWithPointsAsync(Guid trackId);

    #region Синхронизация с сервером (TODO)
    /// <summary>
    /// Получение только не синхронизованных треков
    /// </summary>
    /// <returns></returns>
    Task<List<Track>> GetUnsyncedTracksAsync();
    /// <summary>
    /// Отметка трека синхронизированным
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    Task MarkAsSyncedAsync(Guid trackId);
    #endregion
    /// <summary>
    /// Получение не завершенных треков для аварийной остановки
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Track?> GetUnfinishedTrackAsync(Guid userId);
    /// <summary>
    /// Удаление трека
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    Task DeleteTrackAsync(Guid trackId);
}
