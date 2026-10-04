using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Utils;
using CommunityToolkit.Maui.Storage;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace BurnOffTheFat.Core.Services;

/// <summary>
/// Работаем с гео-локацией, треком и всем, что с этим связано
/// </summary>
public class GpsLocationService : INotifyPropertyChanged
{
    /// <summary>
    /// Интервал получения точек трека
    /// </summary>
    const int GetLocationInterval = 2;
    #region Привязки
    private double _maxSpeedKmH = LocationUtils.DefaultMaxSpeedKmH;
    /// <summary>Максимально допустимая скорость между точками (км/ч).</summary>
    public double MaxSpeedKmH
    {
        get => _maxSpeedKmH;
        set => SetField(ref _maxSpeedKmH, value);
    }
    private bool _isRecording;
    /// <summary>
    /// Производится ли запись трека
    /// </summary>
    public bool IsRecording
    {
        get => _isRecording;
        private set
        {
            if (SetField(ref _isRecording, value))
            {
                OnPropertyChanged(nameof(TrackButtonText));
                OnPropertyChanged(nameof(TrackButtonColor));
            }
        }
    }
    private string _status = "Статус: Готов к записи";
    /// <summary>
    /// Статус записи
    /// </summary>
    /// TODO: переработать в Enum
    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }
    private string _currentCoords = "Текущие координаты отобразятся при записи";
    /// <summary>
    /// Текущие координаты
    /// </summary>
    /// TODO: переработать тип в Location
    public string CurrentCoords
    {
        get => _currentCoords;
        private set => SetField(ref _currentCoords, value);
    }

    private string _altitude = "Высота отобразится при записи";
    /// <summary>
    /// Высота
    /// </summary>
    public string Altitude
    {
        get => _altitude;
        private set => SetField(ref _altitude, value);
    }

    private int _pointCount;
    /// <summary>
    /// Количество записаных точек
    /// </summary>
    public int PointCount
    {
        get => _pointCount;
        private set => SetField(ref _pointCount, value);
    }

    private int _discardedCount;
    /// <summary>
    /// Количество отброшеных точек
    /// </summary>
    public int DiscardedCount
    {
        get => _discardedCount;
        private set => SetField(ref _discardedCount, value);
    }
    #endregion

    /// <summary>Текст на кнопке Старт/Стоп.</summary>
    public string TrackButtonText => IsRecording
        ? "Остановить и сохранить трек"
        : "Начать запись трека";

    /// <summary>Цвет кнопки Старт/Стоп.</summary>
    public Color TrackButtonColor => IsRecording ? Colors.Red : Colors.Green;

    private readonly List<Location> _trackPoints = new();

    #region События
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Возникает по завершении записи трека.
    /// </summary>
    public event EventHandler<TrackSavedEventArgs>? TrackSaved;
    #endregion

    /// <summary>
    /// Запускает запись трека.
    /// </summary>
    public async Task<bool> StartRecordingAsync()
    {
        if (IsRecording) return false;

        var hasPermission = await CheckGpsPermissionAsync();
        if (!hasPermission)
        {
            Status = "Статус: Нет разрешений на GPS";
            return false;
        }

#if ANDROID
        await RequestBackgroundLocationPermissionAsync();
#endif

        _trackPoints.Clear();
        PointCount = 0;
        DiscardedCount = 0;

        IsRecording = true;
        Status = "Статус: Запись трека...";

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
            Status = $"Статус: Ошибка запуска ({ex.Message})";
            IsRecording = false;
            Geolocation.Default.LocationChanged -= OnLocationChanged;
            return false;
        }
    }

#if ANDROID
    private static async Task RequestBackgroundLocationPermissionAsync()
    {
        var bgStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (bgStatus != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.LocationAlways>();
    }

#endif

    /// <summary>
    /// Останавливает запись трека и сохраняет GPX-файл.
    /// </summary>
    public async Task StopRecordingAsync()
    {
        if (!IsRecording) return;

        IsRecording = false;
        Status = "Статус: Сохранение...";

        Geolocation.Default.LocationChanged -= OnLocationChanged;
        Geolocation.Default.StopListeningForeground();

        if (_trackPoints.Count > 0)
        {
            var result = await SaveGpxFileAsync(_trackPoints);
            Status = "Статус: Готов к записи";

            TrackSaved?.Invoke(this, new TrackSavedEventArgs(
                _trackPoints.Count,
                result.FilePath,
                result.Success,
                result.Message));
        }
        else
        {
            Status = "Статус: Готов к записи";
            TrackSaved?.Invoke(this, new TrackSavedEventArgs(
                0, null, false, "Не удалось записать ни одной точки. Файл не создан."));
        }
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
        catch { /* игнорируем */ }

        try
        {
            var reqFine = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(15));
            fine = await Geolocation.Default.GetLocationAsync(reqFine);
        }
        catch { /* игнорируем */ }

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
        if (e.Location == null) return;

        var lastPoint = _trackPoints.Count > 0 ? _trackPoints[^1] : null;

        if (!LocationUtils.IsPointAcceptable(e.Location, lastPoint, MaxSpeedKmH, out var reason))
        {
            DiscardedCount++;
            System.Diagnostics.Debug.WriteLine($"[GPS Filter] {reason}");
            return;
        }

        _trackPoints.Add(e.Location);
        PointCount = _trackPoints.Count;

        //TODO: Вынести из логики формирование строки, хранить только текущую координату и статистику записи
        CurrentCoords = $"Текущие: {LocationUtils.FormatCoords(e.Location)} " +
                        $"(Точек: {PointCount}, отфильтровано: {DiscardedCount})";
        Altitude = e.Location.Altitude.HasValue
            ? $"Высота: {e.Location.Altitude.Value:F1} м"
            : "Высота: определение...";
    }

    /// <summary>
    /// Сохранение файла в память
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    private static async Task<SaveResult> SaveGpxFileAsync(List<Location> points)
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
                ? new SaveResult(true, result.FilePath, "OK")
                : new SaveResult(false, null, "Сохранение отменено пользователем");
        }
        catch (Exception ex)
        {
            return new SaveResult(false, null, $"Ошибка при сохранении: {ex.Message}");
        }
    }

    private record SaveResult(bool Success, string? FilePath, string Message);

    #region INotifyPropertyChanged
    /// <summary>
    /// Генерация события изменения значения поля
    /// </summary>
    /// <param name="name"></param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    /// <summary>
    /// Установка значения поля с генерацией события его изменения для привязки
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
    #endregion

}
