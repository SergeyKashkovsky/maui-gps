namespace BurnOffTheFat.Core.Events;

/// <summary>
/// Параметры события получения новой точки при записи трека
/// </summary>
public class LocationPointEventArgs
{
    /// <summary>
    /// Полученная точка
    /// </summary>
    public Location Location { get; init; } = default!;
    /// <summary>
    /// Количество записанных точек TODO: зачем?
    /// </summary>
    public int PointCount { get; init; }
    /// <summary>
    /// Количество отброшенных точек TODO: зачем?
    /// </summary>
    public int DiscardedCount { get; init; }
    /// <summary>
    /// Пройденный путь в метрах
    /// </summary>
    public int TotalDistanceM {  get; init; }
    /// <summary>
    /// Накопленный подъем
    /// </summary>
    public int TotalAscentM { get; init; }
    /// <summary>
    /// Накопленный спуск
    /// </summary>
    public int TotalDescentM { get; init; }
}
