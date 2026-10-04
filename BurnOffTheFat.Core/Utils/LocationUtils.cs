using System.Globalization;
using System.Text;

namespace BurnOffTheFat.Core.Utils;

public static class LocationUtils
{
    /// <summary>Максимально допустимая погрешность точки (в метрах).</summary>
    public const double MaxAccuracyMeters = 35.0;

    /// <summary>Минимальное расстояние между точками трека (в метрах).</summary>
    public const double MinDistanceMeters = 3.0;

    /// <summary>Максимальная скорость по умолчанию (км/ч).</summary>
    public const double DefaultMaxSpeedKmH = 45.0;

    /// <summary>
    /// Проверяет, можно ли добавить точку в трек.
    /// </summary>
    /// <param name="newLocation">Новая точка.</param>
    /// <param name="lastPoint">Предыдущая точка трека (может быть null).</param>
    /// <param name="maxSpeedKmH">Лимит скорости для фильтра отскоков.</param>
    /// <param name="reason">Причина отбраковки (для диагностики).</param>
    public static bool IsPointAcceptable(
        Location newLocation,
        Location? lastPoint,
        double maxSpeedKmH,
        out string reason)
    {
        reason = string.Empty;

        // 1. Фильтр по точности
        if (newLocation.Accuracy.HasValue && newLocation.Accuracy.Value > MaxAccuracyMeters)
        {
            reason = $"Точность {newLocation.Accuracy.Value:F1} м > {MaxAccuracyMeters} м";
            return false;
        }

        if (lastPoint == null)
            return true;

        // 2. Расстояние между точками
        var distanceMeters = Location.CalculateDistance(lastPoint, newLocation, DistanceUnits.Kilometers) * 1000;
        var timeDeltaSeconds = (newLocation.Timestamp - lastPoint.Timestamp).TotalSeconds;

        if (distanceMeters < MinDistanceMeters)
        {
            reason = $"Стоянка: {distanceMeters:F2} м < {MinDistanceMeters} м";
            return false;
        }

        // 3. Фильтр отскоков по скорости
        if (timeDeltaSeconds > 0)
        {
            var speedKmH = (distanceMeters / timeDeltaSeconds) * 3.6;
            if (speedKmH > maxSpeedKmH)
            {
                reason = $"Отскок: {speedKmH:F1} км/ч > {maxSpeedKmH:F1} км/ч";
                return false;
            }
        }

        return true;
    }
    /// <summary>
    /// Форматирует координаты для отображения.
    /// </summary>
    public static string FormatCoords(Location loc, int digits = 6)
        => $"{loc.Latitude.ToString($"F{digits}", CultureInfo.InvariantCulture)}, " +
           $"{loc.Longitude.ToString($"F{digits}", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Формирует GPX-файл по списку точек.
    /// </summary>
    public static string BuildGpx(List<Location> points)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
        sb.AppendLine("<gpx version=\"1.1\" creator=\"MauiGpsTracker\" xmlns=\"http://topografix.com\" xmlns:xsi=\"http://w3.org\" xsi:schemaLocation=\"http://topografix.com http://topografix.com/gpx.xsd\">");
        sb.AppendLine("  <metadata>");
        sb.AppendLine($"    <time>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</time>");
        sb.AppendLine("  </metadata>");
        sb.AppendLine("  <trk>");
        sb.AppendLine($"    <name>Трек {DateTime.Now:yyyy-MM-dd HH:mm}</name>");
        sb.AppendLine("    <trkseg>");

        foreach (var p in points)
        {
            var lat = p.Latitude.ToString(CultureInfo.InvariantCulture);
            var lon = p.Longitude.ToString(CultureInfo.InvariantCulture);
            var time = p.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");

            sb.AppendLine($"      <trkpt lat=\"{lat}\" lon=\"{lon}\">");
            if (p.Altitude.HasValue)
            {
                var ele = p.Altitude.Value.ToString(CultureInfo.InvariantCulture);
                sb.AppendLine($"        <ele>{ele}</ele>");
            }
            sb.AppendLine($"        <time>{time}</time>");
            if (p.Accuracy.HasValue)
            {
                var hdop = Math.Max(1.0, p.Accuracy.Value / 5.0)
                    .ToString("F2", CultureInfo.InvariantCulture);
                sb.AppendLine($"        <hdop>{hdop}</hdop>");
            }
            sb.AppendLine("      </trkpt>");
        }

        sb.AppendLine("    </trkseg>");
        sb.AppendLine("  </trk>");
        sb.AppendLine("</gpx>");
        return sb.ToString();
    }
}
