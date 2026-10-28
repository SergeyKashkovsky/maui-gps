namespace BurnOffTheFat.Common.Models;
/// <summary>
/// Параметры точки на местности, полученные от GPS
/// </summary>
public class GeoPoint
{
    /// <summary>
    /// Дата и время получения точки
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Широта
    /// </summary>
    public double Latitude { get; set; }
    /// <summary>
    /// Долгота
    /// </summary>
    public double Longitude { get; set; }
    /// <summary>
    /// Высота
    /// </summary>
    public double? Altitude { get; set; }
    /// <summary>
    /// Горизонтальная точность в метрах
    /// </summary>
    public double? Accuracy { get; set; }
    /// <summary>
    /// Вертикальная точность в метрах
    /// </summary>
    public double? VerticalAccuracy { get; set; }
    /// <summary>
    /// Скорость
    /// </summary>
    public double? Speed { get; set; }
    /// <summary>
    /// Курс
    /// </summary>
    public double? Course { get; set; }
}
