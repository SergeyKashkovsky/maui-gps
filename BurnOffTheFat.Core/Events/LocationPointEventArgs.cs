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
    //TODO: пройденный путь, набранная высота, общий спуск
}
