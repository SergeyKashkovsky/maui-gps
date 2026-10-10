using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Utils;

namespace BurnOffTheFat.Core.Services;

/// <summary>
/// Работаем с гео-локацией, треком и всем, что с этим связано
/// </summary>
public class GpsLocationService : IGpsLocationService, IDisposable
{
    /// <summary>
    /// Интервал получения точек трека
    /// </summary>
    const int GetLocationInterval = 2;
    private readonly List<Location> _trackPoints = new();
    /// <inheritdoc/>
    public List<Location> Points => _trackPoints;
    /// <inheritdoc/>
    public double MaxSpeedKmH { get; set; } = LocationUtils.DefaultMaxSpeedKmH;
    
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
    /// Получаем все разрешения на работе в фоне
    /// </summary>
    /// <returns></returns>
    private async Task<bool> GetPermissions()
    {
        if (!await CheckAndRequestGpsPermissionAsync()) return false;
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

        _trackPoints.Clear();
        DiscardedCount = 0;

        _isRecording = true;
        return _isRecording;
    }

    /// <summary>
    /// Останавливает запись трека и сохраняет GPX-файл.
    /// </summary>
    public async Task StopRecordingAsync()
    {
        if (!IsRecording) return;
        _isRecording = false;

        Geolocation.Default.LocationChanged -= OnLocationChanged;
        
        //TODO: оптимизация трека
    }

    /// <summary>
    /// Проверяет статус GPS и получает координаты с разной точностью.
    /// </summary>
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
    /// Проверяет, выдан ли доступ к GPS.
    /// </summary>
    public static async Task<bool> CheckAndRequestGpsPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        return status == PermissionStatus.Granted;
    }

    /// <summary>
    /// Обработчик события получения точки от GPS
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        // TODO: Реализовать запись трека по флагу записи
        if (e.Location == null) return;
        if (_isRecording)
        {
            var lastPoint = _trackPoints.Count > 0 ? _trackPoints[^1] : null;

            if (!LocationUtils.IsPointAcceptable(e.Location, lastPoint, MaxSpeedKmH, out var reason))
            {
                DiscardedCount++;
                System.Diagnostics.Debug.WriteLine($"[GPS Filter] {reason}");
                return;
            }

            _trackPoints.Add(e.Location);
        }
        
        LocationReceived?.Invoke(this, new LocationPointEventArgs
        {
            Location = e.Location,
            PointCount = _trackPoints.Count,
            DiscardedCount = DiscardedCount
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Geolocation.Default.StopListeningForeground();
#if ANDROID
        AndriodPermissionService.StopAndroidGpsService();
#endif
    }
}
