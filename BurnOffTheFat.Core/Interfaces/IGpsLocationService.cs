using BurnOffTheFat.Core.Events;

namespace BurnOffTheFat.Core.Interfaces;
/// <summary>
/// Интерфейс для работы с местоположением
/// </summary>
public interface IGpsLocationService
{
    /// <summary>
    /// Признак того, что сервис получает события GPS
    /// </summary>
    bool IsListening { get; }
    /// <summary>
    /// Признак того, что сервис обрабатывает события GPS и записывает трек
    /// </summary>
    bool IsRecording { get; }
    /// <summary>
    /// Коллекция собранных точек
    /// </summary>
    List<Location> Points { get; }
    /// <summary>
    /// Количество записанных точек
    /// </summary>
    int PointCount { get; }
    /// <summary>
    /// Количество отброшенных точек
    /// </summary>
    int DiscardedCount { get; }
    /// <summary>
    /// Максимальная скорость, определенная для фильтрации выбросов
    /// </summary>
    double MaxSpeedKmH { get; set; }

    /// <summary>
    /// Событие получения новой валидной точки местоположения
    /// </summary>
    event EventHandler<LocationPointEventArgs>? LocationReceived;
    
    /// <summary>
    /// Запуск прослушивания событий GPS для определения необходимых параметров для запуска трека
    /// </summary>
    /// <returns></returns>
    Task StartListenAsync();
    /// <summary>
    /// Запуск трека
    /// </summary>
    /// <returns></returns>
    Task<bool> StartRecordingAsync();
    /// <summary>
    /// Завершение трека
    /// </summary>
    /// <returns></returns>
    Task StopRecordingAsync();
    /// <summary>
    /// Получение текущего значения местоположения: приблизительного и точного
    /// </summary>
    /// <returns></returns>
    Task<(Location? Coarse, Location? Fine)> GetTestLocationsAsync();
}
