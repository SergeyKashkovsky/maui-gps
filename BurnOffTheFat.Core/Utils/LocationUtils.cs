using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Core.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace BurnOffTheFat.Core.Utils;

public static class LocationUtils
{
    /// <summary>Максимально допустимая погрешность точки (в метрах).</summary>
    public const double MaxAccuracyMeters = 20.0;

    /// <summary>Минимальное расстояние между точками трека (в метрах).</summary>
    public const double MinDistanceMeters = 1.5;

    /// <summary>Максимальная скорость по умолчанию (км/ч).</summary>
    public const double DefaultMaxSpeedKmH = 45.0;

    /// <summary>
    /// Проверяет, можно ли добавить точку в трек.
    /// </summary>
    /// <param name="newLocation">Новая точка.</param>
    /// <param name="lastPoint">Предыдущая точка трека (может быть null).</param>
    /// <param name="speedMode">Режим скорости для фильтра отскоков.</param>
    /// <param name="reason">Причина отбраковки (для диагностики).</param>
    public static bool IsPointAcceptable(
        Location newLocation,
        Location? lastPoint,
        SpeedMode speedMode,
        ILogger<GpsLocationService> logger,
        out string reason,
        out double distance)
    {
        reason = string.Empty;
        distance = 0;

        // Фильтр по точности
        if (newLocation.Accuracy.HasValue && newLocation.Accuracy.Value > MaxAccuracyMeters)
        {
            reason = $"Точность {newLocation.Accuracy.Value:F1} м > {MaxAccuracyMeters} м";
            return false;
        }

        // Фильтр по вертикальной точности
        if (newLocation.VerticalAccuracy.HasValue && newLocation.VerticalAccuracy.Value > MaxAccuracyMeters)
        {
            reason = $"Вертикальная точность {newLocation.VerticalAccuracy.Value:F1} м > {MaxAccuracyMeters} м";
            return false;
        }

        if (lastPoint == null)
            return true;
        
        // убираем точки из прошлого
        if(lastPoint.Timestamp > newLocation.Timestamp)
        {
            reason = $"Точка из прошлого: {lastPoint.Timestamp:F}(предыдущая точка) > {newLocation.Timestamp:F}(новая точка)";
            return false;
        }

        // убираем, если стоим, или очень медленно двигаемся
        distance = lastPoint.CalculateDistance3DMeters(newLocation);
        if (distance < MinDistanceMeters)
        {
            reason = $"Расстояние между точками: {distance:F2} м < {MinDistanceMeters} м";
            return false;
        }

        var timeDeltaSeconds = (newLocation.Timestamp - lastPoint.Timestamp).TotalSeconds;
        // Убираем, если скорость минимально допустима
        if (timeDeltaSeconds <= 0)
        {
            reason = $"Одновременные события или точка из прошлого: {timeDeltaSeconds:F2} с";
            return false;
        }
        var currentSpeedMs = distance / timeDeltaSeconds;
        var currentSpeedKmH = currentSpeedMs * 3.6;
        // TODO: вынести профиль скоростей в Common
        var maxSpeedKmH = 90;
        var maxAcceleration = 8.0;
        switch (speedMode)
        {
            case SpeedMode.Walking:
                maxSpeedKmH = 15;
                maxAcceleration = 1.2;
                break;
            case SpeedMode.Running:
                maxSpeedKmH = 25;
                maxAcceleration = 2.0;
                break;
            case SpeedMode.Cycling:
                maxSpeedKmH = 45;
                maxAcceleration = 4.0;
                break;
        }

        if (currentSpeedKmH > maxSpeedKmH)
        {
            reason = $"Отскок: {currentSpeedKmH:F1} км/ч > {maxSpeedKmH:F1} км/ч";
            return false;
        }

        // 4. Расчет ускорения относительно ГЕОМЕТРИЧЕСКОЙ скорости прошлого шага
        // Мы гарантируем, что в lastValid.Speed записана именно геометрическая скорость,
        // так как мы сами сохранили её туда на предыдущем шаге (см. конец метода)
        var previousGeomSpeedMS = lastPoint!.Speed.HasValue ? lastPoint.Speed.Value : 0.0;

        var acceleration = Math.Abs(currentSpeedMs - previousGeomSpeedMS) / timeDeltaSeconds;

        // 5. Проверка на выброс по ускорению
        if (acceleration > maxAcceleration)
        {
            reason = $"Ускорение: {acceleration:F2} м/с² > {maxAcceleration:F2} км/ч";
            return false;
        }

        logger.LogDebug("Рассчитаны параметры валидной точки: расстояние от предыдущей - {distance}м, время - {timeDeltaSeconds}с, скорость - {currentSpeedMs}км/ч, ускорение - {acceleration}м/с^2.{nl} Параметры старой точки: {@oldlocation}.{nl} Параметры новой точки: {@newlocation}", distance, timeDeltaSeconds, currentSpeedKmH, acceleration, Environment.NewLine, lastPoint, Environment.NewLine, newLocation);

        newLocation.Speed = currentSpeedMs;

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
            var time = p.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

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
    /// <summary>
    /// Читаем документ GPX-файла
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static List<Location> ReadGpx(XDocument doc)
    {
        var locations = new List<Location>();
        // В GPX файлах всегда есть пространство имен (Namespace), его важно учитывать при поиске тегов
        XNamespace ns = doc.Root?.GetDefaultNamespace() ?? "http://topografix.com";

        // Ищем все теги <trkpt> (track point) в документе
        var trackPoints = doc.Descendants(ns + "trkpt");

        foreach (var pt in trackPoints)
        {
            var latAttr = pt.Attribute("lat")?.Value;
            var lonAttr = pt.Attribute("lon")?.Value;

            if (double.TryParse(latAttr, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(lonAttr, System.Globalization.CultureInfo.InvariantCulture, out double lon))
            {
                var location = new Location(lat, lon);

                // Пытаемся прочитать высоту <ele>, если она есть
                var eleElem = pt.Element(ns + "ele")?.Value;
                if (double.TryParse(eleElem, System.Globalization.CultureInfo.InvariantCulture, out double ele))
                    location.Altitude = ele;

                // Пытаемся прочитать время <time>, если необходимо
                var timeElem = pt.Element(ns + "time")?.Value;
                if (DateTime.TryParse(timeElem, out DateTime time))
                    location.Timestamp = time;

                // Пытаемся прочитать погрешность <hdop>, если необходимо
                var hdopElem = pt.Element(ns + "hdop")?.Value;
                if (double.TryParse(hdopElem, CultureInfo.InvariantCulture, out double hdop))
                    location.Accuracy = hdop * 5.0;

                locations.Add(location);
            }
        }
        return locations;
    }

    /// <summary>
    /// Вычисляет расстояние между двумя точками с учетом перепада высот.
    /// </summary>
    /// <returns>Расстояние в метрах.</returns>
    public static double CalculateDistance3DMeters(this Location start, Location end)
    {
        // 1. Получаем плоское расстояние по дуге большого круга (в километрах)
        double flatDistanceKm = Location.CalculateDistance(start, end, DistanceUnits.Kilometers);

        // Переводим в метры
        double flatDistanceMeters = flatDistanceKm * 1000.0;

        // 2. Считаем разницу высот (если высота не указана, считаем её равной 0)
        double startAltitude = start.Altitude ?? 0.0;
        double endAltitude = end.Altitude ?? 0.0;
        double heightDelta = endAltitude - startAltitude;

        // 3. Применяем теорему Пифагора для трехмерного пространства
        double distance3D = Math.Sqrt(Math.Pow(flatDistanceMeters, 2) + Math.Pow(heightDelta, 2));

        return distance3D;
    }
}
