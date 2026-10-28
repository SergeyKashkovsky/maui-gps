using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Utils;
using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Utils;
using Microsoft.Extensions.Logging;

namespace BurnOffTheFat.Core.Services;

/// <summary>
/// Работаем с гео-локацией, треком и всем, что с этим связано
/// </summary>
public class GpsLocationService(ILogger<GpsLocationService> logger) : IGpsLocationService
{

    /// <summary>
    /// Интервал получения точек трека
    /// </summary>
    const int GetLocationInterval = 2;
    private readonly List<Location> _trackPoints = [];
    /// <inheritdoc/>
    public List<Location> Points => _trackPoints;
    /// <summary>
    /// Исходный, не отфиольтрованный массив точек.
    /// </summary>
    private readonly List<Location> _sourceTrackPoints = [];
    /// <inheritdoc/>
    public List<Location> SourcePoints => _sourceTrackPoints;

    /// <inheritdoc/>
    public double CurrentSpeedKmH { get; private set; } = 0;
    /// <inheritdoc/>
    public double TotalDistance { get; private set; } = 0;
    /// <inheritdoc/>
    public double TotalAscentM { get; private set; } = 0;
    /// <inheritdoc/>
    public double TotalDescentM { get; private set; } = 0;
    /// <inheritdoc/>
    public SpeedMode SpeedMode { get; set; }

    private bool _isListening;
    /// <inheritdoc/>
    public bool IsListening => _isListening;
    
    private bool _isRecording;
    /// <inheritdoc/>
    public bool IsRecording => _isRecording;
    
    /// <inheritdoc/>
    public int PointCount => _trackPoints.Count;

    /// <inheritdoc/>
    public int DiscardedCount { get; private set; }
    

    /// <inheritdoc/>
    public event EventHandler<LocationPointEventArgs>? LocationReceived;
    /// <inheritdoc/>
    public async Task StartListenAsync()
    {
        if(_isListening) return;
        
        if (!await GetPermissions()) return;

        Geolocation.Default.LocationChanged += OnLocationChanged;

        try
        {
            var request = new GeolocationListeningRequest(
                GeolocationAccuracy.High,
                TimeSpan.FromSeconds(GetLocationInterval));
            await Geolocation.Default.StartListeningForegroundAsync(request);
        }
        catch
        {
            Geolocation.Default.LocationChanged -= OnLocationChanged;
        }
        _isListening = true;
    }

    /// <summary>
    /// Метод остановки прослушивания GPS. TODO: вынести в интерфейс
    /// </summary>
    public void StopListen()
    {
        _isListening = false;
        Geolocation.Default.StopListeningForeground();
        Geolocation.Default.LocationChanged -= OnLocationChanged;
#if ANDROID
        AndriodPermissionService.StopAndroidGpsService();
#endif
    }
    /// <summary>
    /// Получаем все разрешения на работе в фоне
    /// </summary>
    /// <returns></returns>
    private static async Task<bool> GetPermissions()
    {
        if (!await AndriodPermissionService.CheckAndRequestGpsPermissionAsync()) return false;
#if ANDROID
        await AndriodPermissionService.RequestBackgroundLocationPermissionAsync();
        AndriodPermissionService.StartAndroidGpsService();
#endif
        return true;
    }

    /// <summary>
    /// Запускает запись трека.
    /// </summary>
    public async Task<bool> StartRecordingAsync()
    {
        if (!IsListening ||
            IsRecording ||
            !await GetPermissions())
            return false;

        logger.LogDebug("Начинаю запись трека");

        _trackPoints.Clear();
        _sourceTrackPoints.Clear();
        DiscardedCount = 0;
        TotalDistance = 0;

        _isRecording = true;
        return _isRecording;
    }

    /// <summary>
    /// Останавливает запись трека
    /// </summary>
    public async Task StopRecordingAsync()
    {
        if (!IsRecording) return;
        _isRecording = false;

        Geolocation.Default.LocationChanged -= OnLocationChanged;
        logger.LogDebug("Запись трека остановлена. Пройденный путь {distance} метров.", TotalDistance);
        
    }

    /// <summary>
    /// Проверяет статус GPS и получает координаты с разной точностью.
    /// </summary>
    [Obsolete("Архитектура приложения не требует этого метода, но пока оставим")]
    public async Task<(Location? Coarse, Location? Fine)> GetTestLocationsAsync()
    {
        Location? coarse = null, fine = null;

        try
        {
            var reqCoarse = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            coarse = await Geolocation.Default.GetLocationAsync(reqCoarse);
        }
        catch {}

        try
        {
            var reqFine = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(15));
            fine = await Geolocation.Default.GetLocationAsync(reqFine);
        }
        catch {}

        return (coarse, fine);
    }

    /// <summary>
    /// Обработчик события получения точки от GPS
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        if (e.Location == null) return;
        // Если производится запись трека
        if (_isRecording)
        {
            var lastPoint = _trackPoints.Count > 0 ? _trackPoints[^1].ToGeoPoint() : null;
            _sourceTrackPoints.Add(e.Location);
            var newPoint = e.Location.ToGeoPoint();
            
            // получаем все параметры пройденного пути
            var distance = newPoint.GetDistanceM(lastPoint);
            var timeDelta = newPoint.Timestamp.GetTimeDeltaSec(lastPoint?.Timestamp ?? newPoint.Timestamp);
            var currentSpeedMS = newPoint.GetCurrentSpeedMS(lastPoint);
            var acceleration = newPoint.GetAcceleration(lastPoint);
            
            // производим грубую фильтрацию
            if (!GeoPointsUtils.CoarsePointFilter(
                newPoint,
                lastPoint,
                currentSpeedMS.SpeedMSToKmH(),
                acceleration,
                distance,
                SpeedMode,
                out var reason
                ))
            {
                // Если точка нам не подходит, возвращаем параметры старой точки
                DiscardedCount++;
                logger.LogDebug("Точка не прошла фильтр: {reason}", reason);
                LocationReceived?.Invoke(this, new LocationPointEventArgs
                {
                    Location = new()
                    {
                        Accuracy = e.Location.Accuracy,
                        Altitude = lastPoint?.Altitude ?? e.Location.Altitude,
                        Latitude = lastPoint?.Latitude ?? e.Location.Latitude,
                        Longitude = lastPoint?.Longitude ?? e.Location.Longitude,
                        Speed = lastPoint?.Speed,
                    },
                    PointCount = _trackPoints.Count,
                    DiscardedCount = DiscardedCount,
                    TotalAscentM = (int)TotalAscentM,
                    TotalDescentM = (int)TotalDescentM,
                    TotalDistanceM = (int)TotalDistance
                });
                return;

            }
            TotalDistance += distance;
            // TODO: сглаживание трека по горизонтали и вертикали
            var ascent = e.Location.Altitude - lastPoint?.Altitude ?? 0;
            if (ascent < 0)
                TotalDescentM += -ascent;
            else
                TotalAscentM += ascent;

            _trackPoints.Add(e.Location);
        }

        LocationReceived?.Invoke(this, new LocationPointEventArgs
        {
            Location = e.Location,
            PointCount = _trackPoints.Count,
            DiscardedCount = DiscardedCount,
            TotalAscentM = (int)TotalAscentM,
            TotalDescentM = (int)TotalDescentM,
            TotalDistanceM = (int)TotalDistance
        });
    }

}
