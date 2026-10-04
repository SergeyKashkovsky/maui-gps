using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Services;
using BurnOffTheFat.Core.Utils;

namespace MauiGps;

public partial class MainPage : ContentPage
{
    private readonly GpsLocationService _gpsService;

    public MainPage(GpsLocationService gpsLocationService)
    {
        InitializeComponent();
        _gpsService = gpsLocationService;
        _gpsService.TrackSaved += OnTrackSaved;

        // Привязываем BindingContext страницы к сервису
        BindingContext = _gpsService;
    }
    /// <summary>
    /// Событие нажатия на кнопку Старт/Стоп
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnToggleTrackClicked(object sender, EventArgs e)
    {
        if (!_gpsService.IsRecording)
        {
            // Запрашиваем переключение режима энергосбережения 
            CheckAndRequestBatteryOptimizations();
            // запуск фоновой службы на android
#if ANDROID
            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(GpsService));
            Android.App.Application.Context.StartForegroundService(intent);
#endif
            var started = await _gpsService.StartRecordingAsync();
            if (!started)
            {
                await DisplayAlertAsync("Ошибка",
                    "Не удалось начать запись. Проверьте разрешения на GPS.", "OK");
            }
        }
        else
        {
            await _gpsService.StopRecordingAsync();
#if ANDROID
            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(GpsService));
            Android.App.Application.Context.StopService(intent);
#endif
        }
    }
    
    /// <summary>
    /// Нажатие кнопки Обновить данные GPS
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnCheckGpsClicked(object sender, EventArgs e)
    {
        var isGpsEnabled = await GpsLocationService.CheckGpsPermissionAsync();
        if (!isGpsEnabled)
        {
            CoarseCoordsLabel.Text = "Примерные: Нет разрешений";
            FineCoordsLabel.Text = "Точные: Нет разрешений";
            return;
        }

        var (coarse, fine) = await _gpsService.GetTestLocationsAsync();

        CoarseCoordsLabel.Text = coarse != null
            ? $"Примерные: {LocationUtils.FormatCoords(coarse, 4)} (Погрешность: {coarse.Accuracy:F0}м)"
            : "Примерные: Ошибка получения";

        FineCoordsLabel.Text = fine != null
            ? $"Точные: {LocationUtils.FormatCoords(fine)} (Погрешность: {fine.Accuracy:F0}м)"
            : "Точные: Ошибка получения";
    }

    /// <summary>
    /// Реакция на завершение сохранения трека.
    /// </summary>
    private async void OnTrackSaved(object? sender, TrackSavedEventArgs e)
    {
        if (e.Success)
        {
            await DisplayAlertAsync("Успех",
                $"Трек сохранен!\nКоличество точек: {e.PointCount}\nФайл: {e.FilePath}",
                "OK");
        }
        else
        {
            await DisplayAlertAsync("Внимание", e.Message, "OK");
        }
    }

    /// <summary>
    /// Запустить диалог переключения режима энергосбережения, если оно ограничивает работу приложения в фоновом режиме
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

}
