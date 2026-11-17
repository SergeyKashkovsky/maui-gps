namespace BurnOffTheFat.Core.Models;

/// <summary>
/// Результат проверки и фильтрации новой точки трека
/// </summary>
public class CheckNewPointResult
{
    /// <summary>
    /// Признак того, что точка не была отфильтрована
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Результат. Содержит либо параметры новой точки, либо старой, если фильтр не пройден
    /// </summary>
    public required Location Result { get; set; }
    /// <summary>
    /// Причина, по которой точка не прошла фильтрацию
    /// </summary>
    public required string Reason { get; set; }
    /// <summary>
    /// Рассчитанное растояние до фильтрации
    /// </summary>
    public double DistanceMBeforeFilter { get; set; }
    /// <summary>
    /// Рассчитанное расстояние после фильтрации
    /// </summary>
    public double DistanceM { get; set; }
    /// <summary>
    /// Рассчитанная скорость до фильтрации
    /// </summary>
    public double CurrentSpeedBeforeFilter { get; set; }
    /// <summary>
    /// Рассчитанная скорость после фильтрации
    /// </summary>
    public double CurrentSpeed { get; set; }
    /// <summary>
    /// Высота до фильтрации
    /// </summary>
    public double AltitudeBeforeFilter { get; set; }
    /// <summary>
    /// Рассчитанный набор высоты/спуск 
    /// </summary>
    public double Acsent { get; set; }
}
