using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Models;
using BurnOffTheFat.Common.Utils;
using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace BurnOffTheFat.Core.Utils;

public static class LocationUtils
{
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

    #region Методы преобразования Location в GeoPoint и назад
    /// <summary>
    /// Обратное преобразование
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Location ToLocation(this GeoPoint point)
        => new()
        {
            Accuracy = point.Accuracy,
            Altitude = point.Altitude,
            Course = point.Course,
            Latitude = point.Latitude,
            AltitudeReferenceSystem = AltitudeReferenceSystem.Geoid,
            Longitude = point.Longitude,
            Speed = point.Speed,
            Timestamp = point.Timestamp,
            VerticalAccuracy = point.VerticalAccuracy,
        };

    /// <summary>
    /// Прямое преобразование
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static GeoPoint ToGeoPoint(this Location point)
        => new()
        {
            Accuracy = point.Accuracy,
            Altitude = point.Altitude,
            Course = point.Course,
            Latitude = point.Latitude,
            Longitude = point.Longitude,
            Speed = point.Speed,
            Timestamp = point.Timestamp,
            VerticalAccuracy = point.VerticalAccuracy,
        };

    #endregion

    /// <summary>
    /// Проверяем достоверность новой точки, фильтруем и корректируем по широте и долготе
    /// </summary>
    /// <param name="newLocation"></param>
    /// <param name="speedMode"></param>
    /// <param name="filter"></param>
    /// <param name="lastLocation"></param>
    /// <returns></returns>
    public static CheckNewPointResult CheckNewPoint(this Location newLocation, List<Location> trackPoints, SpeedMode speedMode, RealtimeGpsKalmanFilter? filter, int windowSize)
    {
        var lastLocation = trackPoints.Count > 0 ? trackPoints[^1] : null;
        var lastPoint = lastLocation?.ToGeoPoint();
        var newPoint = newLocation.ToGeoPoint();

        // получаем все параметры пройденного пути
        var distance = newPoint.GetDistanceM(lastPoint);
        var timeDelta = newPoint.Timestamp.GetTimeDeltaSec(lastPoint?.Timestamp ?? newPoint.Timestamp);
        var currentSpeedMS = newPoint.GetCurrentSpeedMS(lastPoint);
        var acceleration = newPoint.GetAcceleration(lastPoint);

        var result = new CheckNewPointResult
        {
            Result = new(),
            Reason = string.Empty,
            DistanceMBeforeFilter = distance,
            CurrentSpeedBeforeFilter = currentSpeedMS,
            AltitudeBeforeFilter = newLocation?.Altitude ?? 0
        };

        // производим грубую фильтрацию
        if (!GeoPointsUtils.CoarsePointFilter(
            newPoint,
            lastPoint,
            currentSpeedMS.SpeedMSToKmH(),
            acceleration,
            distance,
            speedMode,
            out var reason
            ))
        {
            // Если точка нам не подходит, возвращаем параметры старой точки
            result.Result = new()
            {
                Accuracy = newLocation!.Accuracy,
                Altitude = lastPoint?.Altitude ?? newLocation.Altitude,
                Latitude = lastPoint?.Latitude ?? newLocation.Latitude,
                Longitude = lastPoint?.Longitude ?? newLocation.Longitude,
                Speed = lastPoint?.Speed,
            };
            result.Success = false;
            result.Reason = reason;
            return result;
        }
        // Корректировка фильтром Калмана
        if(filter != null)
        {
            filter.SetProfile(speedMode);
            (newPoint.Latitude, newPoint.Longitude) = filter.Process(newPoint);
        }
        
        // Пересчет расстояния и скорости
        result.DistanceM = newPoint.GetDistanceM(lastPoint);
        result.CurrentSpeed = newPoint.GetCurrentSpeedMS(lastPoint);
        newLocation!.Latitude = newPoint.Latitude;
        newLocation.Longitude = newPoint.Longitude;

        // сглаживание высоты
        var lastPoints = trackPoints.TakeLast(windowSize).Select(x => x.ToGeoPoint());

        newLocation.Altitude = newPoint.GetFilteredAltitude(lastPoints);

        result.Acsent = newLocation.Altitude - lastPoint?.Altitude ?? 0;

        result.Result = newLocation;
        result.Success = true;
        return result;
        
    }
}
