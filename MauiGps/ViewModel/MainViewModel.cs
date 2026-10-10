using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Services;
using BurnOffTheFat.Core.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MauiGps.ViewModel;

public partial class MainViewModel : ObservableObject
{
    private readonly IGpsLocationService _gps;
    private readonly IGpxFileService _gpxFileService;
    public MainViewModel(IGpsLocationService gps, IGpxFileService fileService)
    {
        _gps = gps;
        _gpxFileService = fileService;

        _gps.LocationReceived += OnLocationReceived;
    }

    [ObservableProperty]
    private string _status = "Статус: Готов к записи";

    [ObservableProperty]
    private string _currentCoords = "Текущие координаты отобразятся при записи";

    [ObservableProperty]
    private string _altitude = "Высота отобразится при записи";

    [ObservableProperty]
    private string _coarseCoords = "Примерные: Нажмите обновить";

    [ObservableProperty]
    private string _fineCoords = "Точные: Нажмите обновить";
    
    [ObservableProperty]
    private bool _isListening;
    
    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private double _maxSpeedKmH = LocationUtils.DefaultMaxSpeedKmH;

    // Свойства, вычисляемые из состояния
    public string TrackButtonText => IsRecording
        ? "Остановить и сохранить трек"
        : "Начать запись трека";

    public Color TrackButtonColor => IsRecording ? Colors.Red : Colors.Green;

    // Автоматически вызывается при изменении IsRecording
    partial void OnIsRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(TrackButtonText));
        OnPropertyChanged(nameof(TrackButtonColor));
    }

    // Передаём значение в сервис
    partial void OnMaxSpeedKmHChanged(double value)
        => _gps.MaxSpeedKmH = value;

    //TODO: метод запуска получения параметров местоположения для начала записи трека

    /// <summary>
    /// Запустить или остановить запись трека
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ToggleTrackAsync()
    {
        if (!IsRecording)
        {
            CheckAndRequestBatteryOptimizations();

            try
            {
                var started = await _gps.StartRecordingAsync();
                if (!started)
                {
                    await ShowAlertAsync("Ошибка",
                        "Не удалось начать запись. Проверьте разрешения GPS.");
                    return;
                }

                IsRecording = true;
                Status = "Статус: Запись трека...";
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Ошибка", $"Сбой запуска: {ex.Message}");
            }
            
        }
        else
        {
            Status = "Статус: Сохранение...";
            await _gps.StopRecordingAsync();
            var gpx = LocationUtils.BuildGpx(_gps.Points);
            var fileName = $"track_{DateTime.Now:yyyyMMdd_HHmmss}.gpx";
            var fileResult = await _gpxFileService.CreateGpxFromTextAsync(fileName, gpx);
            if (fileResult.Success)
                await ShowAlertAsync("Успех",
                    $"Трек сохранен!\nКоличество точек: {_gps.PointCount}\nФайл: {fileResult.FilePath}");
            else
                await ShowAlertAsync("Внимание", fileResult.Message);

            IsRecording = false;
            Status = "Статус: Готов к записи";
        }
    }
    /// <summary>
    /// Проверка статуса GPS TODO: не относится к треку, можно пометить Obsolete
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task CheckGpsAsync()
    {
        var ok = await GpsLocationService.CheckGpsPermissionAsync();
        if (!ok)
        {
            CoarseCoords = "Примерные: Нет разрешений";
            FineCoords = "Точные: Нет разрешений";
            return;
        }

        var (coarse, fine) = await _gps.GetTestLocationsAsync();

        CoarseCoords = coarse != null
            ? $"Примерные: {LocationUtils.FormatCoords(coarse, 4)} (Погрешность: {coarse.Accuracy:F0}м)"
            : "Примерные: Ошибка получения";

        FineCoords = fine != null
            ? $"Точные: {LocationUtils.FormatCoords(fine)} (Погрешность: {fine.Accuracy:F0}м)"
            : "Точные: Ошибка получения";
    }

    /// <summary>
    /// Отображение диалогового окна с указанием предупреждения
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = Application.Current?.Windows[0].Page
            ?? throw new InvalidOperationException("Нет активной страницы");
        await page.DisplayAlertAsync(title, message, cancel);
    }

    /// <summary>
    /// Запустить диалог переключения режима энергосбережения, если оно ограничивает работу приложения в фоновом режиме. TODO: вынести в отдельный сервис
    /// </summary>
    public static void CheckAndRequestBatteryOptimizations()
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity == null) return;

        var powerManager = (Android.OS.PowerManager)activity.GetSystemService(Android.Content.Context.PowerService);

        string packageName = activity.PackageName;

        if (powerManager != null && !powerManager.IsIgnoringBatteryOptimizations(packageName))
        {
            Android.Content.Intent intent = new Android.Content.Intent();

            intent.SetAction(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse($"package:{packageName}"));

            // Запускаем Intent из контекста текущего окна приложения
            activity.StartActivity(intent);
        }
#endif
    }
    /// <summary>
    /// Обработчик события добавления точки в трек
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLocationReceived(object? sender, LocationPointEventArgs e)
    {
        // Сервис уже вызвал это событие в UI-потоке (см. GpsLocationService)
        // Если нет — обернём здесь:
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentCoords = $"Текущие: {LocationUtils.FormatCoords(e.Location)} " +
                            $"(Точек: {e.PointCount}, отфильтровано: {e.DiscardedCount})";

            Altitude = e.Location.Altitude.HasValue
                ? $"Высота: {e.Location.Altitude.Value:F1} м"
                : "Высота: определение...";
        });
    }
}
