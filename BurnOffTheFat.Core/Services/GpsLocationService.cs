using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Utils;
using CommunityToolkit.Maui.Storage;
using System.Text;

namespace BurnOffTheFat.Core.Services;

/// <summary>
/// Работаем с гео-локацией, треком и всем, что с этим связано
/// </summary>
public class GpsLocationService : IGpsLocationService
{
    /// <summary>
    /// Интервал получения точек трека
    /// </summary>
    const int GetLocationInterval = 2;
    private readonly List<Location> _trackPoints = new();
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
    public event EventHandler<TrackSavedEventArgs>? TrackSaved;

    /// <inheritdoc/>
    public async Task StartListenAsync()
    {
        // TODO: реализовать предзапуск прослушивания GPS без сбора точек для получения уровня сигнала GPS
    }

    /// <summary>
    /// Запускает запись трека.
    /// </summary>
    public async Task<bool> StartRecordingAsync()
    {
        if (IsRecording) return false;

        var hasPermission = await CheckGpsPermissionAsync();
        if (!hasPermission)
            return false;

#if ANDROID
        await RequestBackgroundLocationPermissionAsync();
        // запуск фоновой службы на android
        var context = Platform.AppContext;
        var intent = new Android.Content.Intent();
        intent.SetClassName(context.PackageName, "burnoffthefat.GpsService");

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
#endif

        _trackPoints.Clear();
        DiscardedCount = 0;

        _isRecording = true;

        Geolocation.Default.LocationChanged += OnLocationChanged;

        try
        {
            var request = new GeolocationListeningRequest(
                GeolocationAccuracy.High,
                TimeSpan.FromSeconds(GetLocationInterval));

            await Geolocation.Default.StartListeningForegroundAsync(request);
            return true;
        }
        catch (Exception ex)
        {
            Geolocation.Default.LocationChanged -= OnLocationChanged;
#if ANDROID
            StopAndroidGpsService();
#endif
            return false;
        }
    }

#if ANDROID
    /// <summary>
    /// Запрос работы приложения как фоновой службы
    /// </summary>
    /// <returns></returns>
    private static async Task RequestBackgroundLocationPermissionAsync()
    {
        var bgStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (bgStatus != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.LocationAlways>();
    }
    /// <summary>
    /// Остановка фоновой службы уведомлений
    /// </summary>
    private void StopAndroidGpsService()
    {
        var context = Platform.AppContext;
        var intent = new Android.Content.Intent();
        intent.SetClassName(context.PackageName, "burnoffthefat.GpsService");
        Android.App.Application.Context.StopService(intent);
    }

#endif

    /// <summary>
    /// Останавливает запись трека и сохраняет GPX-файл.
    /// </summary>
    public async Task StopRecordingAsync()
    {
        if (!IsRecording) return;
        _isRecording = false;

        Geolocation.Default.LocationChanged -= OnLocationChanged;
        Geolocation.Default.StopListeningForeground();
#if ANDROID
        StopAndroidGpsService();
#endif

        if (_trackPoints.Count == 0)
        {
            TrackSaved?.Invoke(this, new TrackSavedEventArgs("Не удалось записать ни одной точки. Файл не создан."));
            return;
        }
        //TODO: оптимизация трека
        var result = await SaveGpxFileAsync(_trackPoints);
        TrackSaved?.Invoke(this, new TrackSavedEventArgs(_trackPoints.Count, result));
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
    public static async Task<bool> CheckGpsPermissionAsync()
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

        var lastPoint = _trackPoints.Count > 0 ? _trackPoints[^1] : null;

        if (!LocationUtils.IsPointAcceptable(e.Location, lastPoint, MaxSpeedKmH, out var reason))
        {
            DiscardedCount++;
            System.Diagnostics.Debug.WriteLine($"[GPS Filter] {reason}");
            return;
        }

        _trackPoints.Add(e.Location);
        LocationReceived?.Invoke(this, new LocationPointEventArgs
        {
            Location = e.Location,
            PointCount = _trackPoints.Count,
            DiscardedCount = DiscardedCount
        });
    }

    /// <summary>
    /// Сохранение файла в память
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    private static async Task<FileSaveResult> SaveGpxFileAsync(List<Location> points)
    {
        var gpx = LocationUtils.BuildGpx(points);
        var utf8NoBom = new UTF8Encoding(false);
        var bytes = utf8NoBom.GetBytes(gpx);

        using var stream = new MemoryStream(bytes);
        var fileName = $"track_{DateTime.Now:yyyyMMdd_HHmmss}.gpx";

        try
        {
            var result = await FileSaver.Default.SaveAsync(fileName, stream, cancellationToken: default);
            return result.IsSuccessful
                ? new FileSaveResult(true, result.FilePath, "OK")
                : new FileSaveResult(false, null, "Сохранение отменено пользователем");
        }
        catch (Exception ex)
        {
            return new FileSaveResult(false, null, $"Ошибка при сохранении: {ex.Message}");
        }
    }


}
