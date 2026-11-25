using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Models.Dto;
using BurnOffTheFat.Common.Utils;

namespace BurnOffTheFat.Core.Models;

/// <summary>
/// Модель элемента списка тренировок на главной
/// </summary>
public class TrainingCardModel
{
    /// <summary>
    /// Тип тренировки
    /// </summary>
    public SpeedMode TrainingType { get; set; }
    /// <summary>
    /// Заголовок
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Продолжительность
    /// </summary>
    public TimeSpan Duration { get; set; }
    /// <summary>
    /// Расстояние
    /// </summary>
    public double DistanceMeters { get; set; }

    /// <summary>
    /// Средняя скорость
    /// </summary>
    public double AverageSpeedKmh { get; set; } // Для ходьбы и др.
    /// <summary>
    /// Средний темп
    /// </summary>
    public string AveragePace { get; set; } = string.Empty; // PaceFormatter.Format(AverageSpeedKmh);

    // Удобные свойства для вывода в XAML
    public string FormattedDuration => Duration.ToString(@"hh\:mm\:ss");
    public string FormattedDistance => $"{(DistanceMeters / 1000.0):F2} км";
    public string FormattedSpeed => $"{AverageSpeedKmh:F1} км/ч";
    public static TrainingCardModel FromTrack(Track track)
    {
        var speed = track.Duration.TotalHours > 0 ? track.TotalDistanceMeters / (1000 * track.Duration.TotalHours) : 0;
        return new()
        {
            TrainingType = track.SpeedMode,
            Title = track.Title,
            DistanceMeters = track.TotalDistanceMeters,
            Duration = track.Duration,
            AverageSpeedKmh = track.Duration.TotalHours > 0 ? track.TotalDistanceMeters/(1000*track.Duration.TotalHours) : 0,
            AveragePace = speed > 0 ? PaceFormatter.Format(1/speed) : "...",
        };

    }
}
