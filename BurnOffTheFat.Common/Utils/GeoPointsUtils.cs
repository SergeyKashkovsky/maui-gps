using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Models;

namespace BurnOffTheFat.Common.Utils;
/// <summary>
/// Общие математические операции для расчета взаимодействия точек на местности
/// </summary>
public static class GeoPointsUtils
{
    /// <summary>Максимально допустимая горизонтальная погрешность точки (в метрах).</summary>
    public const double MaxAccuracyMeters = 20.0;
    /// <summary>Максимально допустимая вертикальная погрешность точки (в метрах).</summary>
    public const double MaxVerticalAccuracyMeters = 20.0;

    /// <summary>Минимальное расстояние между точками трека (в метрах).</summary>
    public const double MinDistanceMeters = 1.5;

    #region Профиль скорости
    /// <summary>
    /// Максимальная скорость при ходьбе
    /// </summary>
    public const int MaxSpeedKmHWalking = 15;
    /// <summary>
    /// Максимальное ускорение при ходьбе
    /// </summary>
    public const double MaxAccelerationWalking = 1.2;
    /// <summary>
    /// Максимальная скорость при беге
    /// </summary>
    public const int MaxSpeedKmHRunning = 25;
    /// <summary>
    /// Максимальное ускорение при беге
    /// </summary>
    public const double MaxAccelerationRunning = 2.0;
    /// <summary>
    /// Максимальная скорость при езде на велосипеде
    /// </summary>
    public const int MaxSpeedKmHCycling = 45;
    /// <summary>
    /// Максимальное ускорение езде на велосипеде
    /// </summary>
    public const double MaxAccelerationCycling = 4.0;
    /// <summary>
    /// Максимальная скорость при езде на машине/мотоцикле
    /// </summary>
    public const int MaxSpeedKmHMotor = 90;
    /// <summary>
    /// Максимальное ускорение при езде на машине/мотоцикле
    /// </summary>
    public const double MaxAccelerationMotor = 8.0;
    /// <summary>
    /// Получение максимальной скорости в зависимости от текущего режима тренировки
    /// </summary>
    /// <param name="speedMode"></param>
    /// <returns></returns>
    public static int GetMaxSpeedProfile(SpeedMode speedMode)
        => speedMode switch
        {
            SpeedMode.Walking => MaxSpeedKmHWalking,
            SpeedMode.Running => MaxSpeedKmHRunning,
            SpeedMode.Cycling => MaxSpeedKmHCycling,
            SpeedMode.Motorized => MaxSpeedKmHMotor,
            _ => MaxSpeedKmHMotor,
        };
    /// <summary>
    /// Получение максимального ускорения в зависимости от текущего режима тренировки
    /// </summary>
    /// <param name="speedMode"></param>
    /// <returns></returns>
    public static double GetMaxAccelerationProfile(SpeedMode speedMode)
        => speedMode switch
        {
            SpeedMode.Walking => MaxAccelerationWalking,
            SpeedMode.Running => MaxAccelerationRunning,
            SpeedMode.Cycling => MaxAccelerationCycling,
            SpeedMode.Motorized => MaxAccelerationMotor,
            _ => MaxAccelerationMotor,
        };
    #endregion

    /// <summary>
    /// Вычисляет расстояние между двумя точками с учетом перепада высот.
    /// </summary>
    /// <returns>Расстояние в метрах.</returns>
    public static double CalculateDistance3DMeters(this GeoPoint start, GeoPoint end)
    {
        var flatDistanceMeters = CalculateDistanceInMeters(start, end);

        var startAltitude = start.Altitude ?? 0.0;
        var endAltitude = end.Altitude ?? 0.0;
        var heightDelta = endAltitude - startAltitude;

        var distance3D = Math.Sqrt(Math.Pow(flatDistanceMeters, 2) + Math.Pow(heightDelta, 2));

        return distance3D;
    }

    /// <summary>
    /// Расчет расстояния между точками с учетом кривизны поверхности Земли
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    public static double CalculateDistanceInMeters(this GeoPoint p1, GeoPoint p2)
    {
        var r = 6371000; // Радиус Земли в метрах
        var dLat = ToRadians(p2.Latitude - p1.Latitude);
        var dLon = ToRadians(p2.Longitude - p1.Longitude);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(p1.Latitude)) * Math.Cos(ToRadians(p2.Latitude)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }
    /// <summary>
    /// Градусы в радианы
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    private static double ToRadians(double val) => (Math.PI / 180) * val;
    /// <summary>
    /// Грубый фильтр поступившей точки
    /// </summary>
    /// <param name="newPoint">Новая точка</param>
    /// <param name="oldPoint">Старая точка</param>
    /// <param name="currentSpeedKmH">Рассчитанная скорость в км/ч</param>
    /// <param name="acceleration">Рассчитанное ускорение в м/(с*с)</param>
    /// <param name="distance">Рассчитанное расстояние между точками</param>
    /// <param name="speedMode">режим скорости тренировки</param>
    /// <param name="reason">Причина, почему точка была отфильтрована</param>
    /// <returns></returns>
    public static bool CoarsePointFilter(
        GeoPoint newPoint,
        GeoPoint? oldPoint,
        double currentSpeedKmH,
        double acceleration,
        double distance,
        SpeedMode speedMode,
        out string reason)
    {
        reason = string.Empty;
        // Фильтр по точности
        if (newPoint.Accuracy.HasValue && newPoint.Accuracy.Value > MaxAccuracyMeters)
        {
            reason = $"Точность {newPoint.Accuracy.Value:F1} м > {MaxAccuracyMeters} м";
            return false;
        }

        // Фильтр по вертикальной точности
        if (newPoint.VerticalAccuracy.HasValue && newPoint.VerticalAccuracy.Value > MaxVerticalAccuracyMeters)
        {
            reason = $"Вертикальная точность {newPoint.VerticalAccuracy.Value:F1} м > {MaxVerticalAccuracyMeters} м";
            return false;
        }

        if (oldPoint == null)
            return true;

        // убираем точки из прошлого
        if (oldPoint.Timestamp > newPoint.Timestamp)
        {
            reason = $"Точка из прошлого: {oldPoint.Timestamp:F}(предыдущая точка) > {newPoint.Timestamp:F}(новая точка)";
            return false;
        }

        // убираем, если стоим, или очень медленно двигаемся
        if (distance < MinDistanceMeters)
        {
            reason = $"Расстояние между точками: {distance:F2} м < {MinDistanceMeters} м";
            return false;
        }

        var timeDeltaSeconds = (newPoint.Timestamp - oldPoint.Timestamp).TotalSeconds;
        // Убираем, если точка из прошлого, или если мы уже ее обрабатывали
        if (timeDeltaSeconds <= 0)
        {
            reason = $"Одновременные события или точка из прошлого: {timeDeltaSeconds:F2} с";
            return false;
        }

        var maxSpeedKmH = GetMaxSpeedProfile(speedMode);
        // отскок по скорости
        if (currentSpeedKmH > maxSpeedKmH)
        {
            reason = $"Отскок: {currentSpeedKmH:F1} км/ч > {maxSpeedKmH:F1} км/ч";
            return false;
        }

        var maxAcceleration = GetMaxAccelerationProfile(speedMode);
        // отскок по ускорению
        if (acceleration > maxAcceleration)
        {
            reason = $"Ускорение: {acceleration:F2} м/с² > {maxAcceleration:F2} км/ч";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Преобразование скорости из м/с в км/ч
    /// </summary>
    /// <param name="speedMS"></param>
    /// <returns></returns>
    public static double SpeedMSToKmH(this double speedMS)
        => speedMS * 3.6;

    /// <summary>
    /// Расчет расстояния между точками в метрах
    /// </summary>
    /// <param name="newPoint"></param>
    /// <param name="oldPoint"></param>
    /// <returns></returns>
    public static double GetDistanceM(this GeoPoint newPoint, GeoPoint? oldPoint)
    {
        if (oldPoint == null) return 0.0;
        return CalculateDistance3DMeters(oldPoint, newPoint);
    }

    /// <summary>
    /// Расчет времени в секундах между точками
    /// </summary>
    /// <param name="newPoint"></param>
    /// <param name="oldPoint"></param>
    /// <returns></returns>
    public static double GetTimeDeltaSec(this DateTimeOffset newPointTimestamp, DateTimeOffset oldPointTimestamp)
        => (newPointTimestamp - oldPointTimestamp).TotalSeconds;

    /// <summary>
    /// Расчет мгновенной скорости в метрах в секунду
    /// </summary>
    /// <param name="newPoint"></param>
    /// <param name="oldPoint"></param>
    /// <returns></returns>
    public static double GetCurrentSpeedMS(this GeoPoint newPoint, GeoPoint? oldPoint)
    {
        if (oldPoint == null) return 0.0;
        return CalculateDistance3DMeters(newPoint, oldPoint) / GetTimeDeltaSec(newPoint.Timestamp, oldPoint.Timestamp);
    }

    /// <summary>
    /// Расчет ускорения относительно предыдущей точки
    /// </summary>
    /// <param name="newPoint"></param>
    /// <param name="oldPoint"></param>
    /// <returns></returns>
    public static double GetAcceleration(this GeoPoint newPoint, GeoPoint? oldPoint)
    {
        if(oldPoint is null) return 0.0;
        var oldSpeed = oldPoint.Speed.HasValue ? oldPoint.Speed.Value : 0.0;
        return Math.Abs(GetCurrentSpeedMS(newPoint, oldPoint) - oldSpeed) / GetTimeDeltaSec(newPoint.Timestamp, oldPoint.Timestamp);
    }
    /// <summary>
    /// Сглаживаем погрешность измерений высоты по коллекции предыдущих точек
    /// </summary>
    /// <param name="newPoint">новая точка</param>
    /// <param name="lastPoints">коллекция предыдущих валидных точек</param>
    /// <returns></returns>
    public static double GetFilteredAltitude(this GeoPoint newPoint, IEnumerable<GeoPoint> lastPoints)
    {
        var alt = lastPoints.Select(x => x.Altitude);
        return GetFilteredParameter(newPoint.Altitude, alt);
    }
    /// <summary>
    /// Сглаживаем погрешность измерения параметра относительно коллекции предыдущих измерений
    /// </summary>
    /// <param name="newParameter">значение нового параметра</param>
    /// <param name="oldParameters">значения предыдущих измерений</param>
    /// <returns></returns>
    private static double GetFilteredParameter(double? newParameter, IEnumerable<double?> oldParameters)
    {
        var avr = oldParameters.Where(x => x != null).Average() ?? 0.0;
        if (newParameter is null) return avr;
        var notNull = oldParameters.Select(x => x ?? avr).ToList();
        notNull.Add(newParameter.Value);
        return notNull.Average();
    }
}
