using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BurnOffTheFat.Common.Utils;
/// <summary>
/// Фильтр КЛмана для сглаживания трека
/// </summary>
public class RealtimeGpsKalmanFilter
{
    private float _qMetresPerSecond;
    private long _timeStampMilliseconds;
    private double _lat;
    private double _lng;
    private SpeedMode _currentProfile;
    /// <summary>
    /// дисперсия оценки — это мера текущей неуверенности фильтра в том, что его вычисленные координаты (_lat, _lng) совпадают с реальным положением объекта.
    /// </summary>
    private double _variance = -1;

    /// <summary>
    /// Порог времени (в мс), после которого фильтр сбрасывается (например, 30 секунд потери сигнала)
    /// </summary>
    private const long ResetTimeoutMs = 30_000;

    public RealtimeGpsKalmanFilter(SpeedMode initialProfile)
    {
        InitProfile(initialProfile);
    }
    /// <summary>
    /// Устанавливает пороговое значение шума в зависимости от профиля скорости
    /// </summary>
    /// <param name="profile"></param>
    public void InitProfile(SpeedMode profile)
    {
        _qMetresPerSecond = profile switch
        {
            SpeedMode.Walking => 1.5f,
            SpeedMode.Running => 3.0f,
            SpeedMode.Cycling => 6.0f,
            SpeedMode.Motorized => 12.0f,
            _ => 12.0f
        };
    }

    /// <summary>
    /// Обновление параметров профайла, если он изменился
    /// </summary>
    /// <param name="profile"></param>
    public void SetProfile(SpeedMode profile)
    {
        if(_currentProfile == profile) return;
        _currentProfile = profile;
        InitProfile(profile);
    }
    /// <summary>
    /// Сброс 
    /// </summary>
    public void Reset()
    {
        _variance = -1;
    }

    /// <summary>
    /// Двумерная фильтрация точек
    /// </summary>
    /// <param name="newPoint"></param>
    /// <param name="timeStampMilliseconds"></param>
    /// <returns></returns>
    public (double Latitude, double Longitude) Process(GeoPoint newPoint)
    {
        var lat = newPoint.Latitude;
        var lng = newPoint.Longitude;
        var accuracy = newPoint.Accuracy.HasValue ? newPoint.Accuracy.Value : 0;
        var timeStampMilliseconds = newPoint.Timestamp.ToUnixTimeMilliseconds();
        if (accuracy < 1) accuracy = 1;

        // Если сигнал пропадал слишком надолго, сбрасываем состояние, чтобы избежать "телепортации"
        if (_variance >= 0 && (timeStampMilliseconds - _timeStampMilliseconds) > ResetTimeoutMs)
            Reset();

        if (_variance < 0)
        {
            _timeStampMilliseconds = timeStampMilliseconds;
            _lat = lat;
            _lng = lng;
            _variance = accuracy * accuracy;
        }
        else
        {
            var timeIncMs = timeStampMilliseconds - _timeStampMilliseconds;
            if (timeIncMs > 0)
            {
                _variance += timeIncMs * _qMetresPerSecond * _qMetresPerSecond / 1000.0f;
                _timeStampMilliseconds = timeStampMilliseconds;
            }

            var k = _variance / (_variance + accuracy * accuracy);
            _lat += k * (lat - _lat);
            _lng += k * (lng - _lng);
            _variance = (1 - k) * _variance;
        }

        return (_lat, _lng);
    }
}
