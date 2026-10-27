using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Utils;
using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Services;
using BurnOffTheFat.Core.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MauiGps.ViewModel;

public partial class MainViewModel : ObservableObject
{
    private readonly IGpsLocationService _gps;
    private readonly ITrackFileService _gpxFileService;
    private readonly ILogger<MainViewModel> _logger;
    public MainViewModel(IGpsLocationService gps, ITrackFileService fileService, ILogger<MainViewModel> logger)
    {
        _gps = gps;
        _gpxFileService = fileService;
        _logger = logger;

        _gps.LocationReceived += OnLocationReceived;
    }
    [ObservableProperty]
    private string _gpsAccuracyText = "Погрешность GPS: Определение...";

    [ObservableProperty]
    private string _status = "Статус: Готов к записи";

    [ObservableProperty]
    private string _currentCoords = "Текущие координаты отобразятся при записи";

    [ObservableProperty]
    private string _altitude = "Высота отобразится при записи";

    [ObservableProperty]
    private bool _isListening;
    
    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private string _currentSpeedKmHText;

    [ObservableProperty]
    private string _totalDistanceMText;
    [ObservableProperty]
    private string _totalAscentMText;
    [ObservableProperty]
    private string _totalDescentMText;
    /// <summary>
    /// Коллекция режимов тренировки для отображения в выпадающем списке
    /// </summary>
    public IReadOnlyList<SpeedMode> SpeedModes { get; } =
        Enum.GetValues<SpeedMode>();

    [ObservableProperty]
    private SpeedMode _selectedSpeedMode = SpeedMode.Walking;

    /// <summary>
    /// Обработчик выбора режима скорости
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedSpeedModeChanged(SpeedMode value)
        => _gps.SpeedMode = value;

    /// <summary>
    /// Название кнопки остановки/запуска тренировки
    /// </summary>
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

    /// <summary>
    /// Запустить или остановить запись трека
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ToggleTrackAsync()
    {
        if (!IsRecording)
        {

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
            var fileName = $"track_{DateTime.Now:yyyyMMdd_HHmmss}";
            var gpxFileName = $"{fileName}.gpx";
            var fileResult = await _gpxFileService.CreateGpxFromTextAsync(gpxFileName, gpx);
            if (fileResult.Success)
                await ShowAlertAsync("Успех",
                    $"Трек сохранен!\nКоличество точек: {_gps.PointCount}\nФайл: {fileResult.FilePath}");
            else
                await ShowAlertAsync("Внимание", fileResult.Message);
            IsRecording = false;
            Status = "Статус: Готов к записи";

            if (_gps.SourcePoints.Count == 0) return;
            var filePath = FileSystemUtils.GetApllicationPublicDirectory();
            var jsonFileName = Path.Combine(filePath, $"{fileName}.json");
            fileResult = await _gpxFileService.CreateFileFromTextAsync(jsonFileName, JsonSerializer.Serialize(_gps.SourcePoints), CancellationToken.None);
            if (fileResult.Success)
                _logger.LogDebug($"Исходный трек сохранен! Количество точек: {_gps.SourcePoints}. Файл: {fileResult.FilePath}");
            else
                _logger.LogWarning("Ошибка сохранения исходного трека: {mess}", fileResult.Message);

        }
    }
    /// <summary>
    /// Загрузка трека из файла
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task LoadTrackAsync()
    {
        try
        {
            // 1. Настраиваем фильтр для расширения .gpx
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                { DevicePlatform.Android, new[] { "application/gpx+xml", "application/xml", "*/*" } },
                { DevicePlatform.iOS, new[] { "com.topografix.gpx" } },
                { DevicePlatform.WinUI, new[] { ".gpx" } }
                });

            var options = new PickOptions
            {
                PickerTitle = "Выберите файл GPX трека",
                FileTypes = customFileType
            };

            // 2. Открываем системное окно выбора файла
            var result = await FilePicker.Default.PickAsync(options);
            if (result == null) return; // Пользователь отменил выбор

            // Проверяем расширение файла на всякий случай (особенно важно для Android при выборе */*)
            if (!result.FileName.EndsWith(".gpx", StringComparison.OrdinalIgnoreCase))
            {
                await ShowAlertAsync("Внимание", "Пожалуйста, выберите файл с расширением .gpx");
                return;
            }

            // 3. Открываем поток файла и передаем в наш сервис
            using var stream = await result.OpenReadAsync();

            // Вызываем чтение (убедитесь, что _gpxFileService внедрен через конструктор вашей ViewModel)
            List<Location> loadedPoints = await _gpxFileService.ReadGpxFile(stream);

            if (loadedPoints.Count == 0)
            {
                await ShowAlertAsync("Внимание", "Выбранный файл пуст или имеет некорректный формат.");
                return;
            }

            // 4. Имитируем отображение данных, как будто трек только что записан
            var lastPoint = loadedPoints[^1]; // Берем последнюю точку трека

            Status = $"Статус: Трек успешно загружен";

            // Форматируем вывод координат через ваш LocationUtils
            CurrentCoords = $"Текущие: {LocationUtils.FormatCoords(lastPoint)} (Точек загружено: {loadedPoints.Count})";

            Altitude = lastPoint.Altitude.HasValue
                ? $"Высота: {lastPoint.Altitude.Value:F1} м"
                : "Высота: нет данных в файле";

        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Ошибка", $"Не удалось загрузить файл: {ex.Message}");
        }
    }
    /// <summary>
    /// Инициализация модели (запуск слушателоя GPS)
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task InitializeAsync()
    {
        Status = "Статус: Поиск спутников...";
#if ANDROID
        AndriodPermissionService.CheckAndRequestBatteryOptimizations();
#endif
        await _gps.StartListenAsync();
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
    /// Обработчик события добавления точки в трек
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLocationReceived(object? sender, LocationPointEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentCoords = $"Текущие: {LocationUtils.FormatCoords(e.Location)} " +
                            $"(Точек: {e.PointCount}, отфильтровано: {e.DiscardedCount})";

            Altitude = e.Location.Altitude.HasValue
                ? $"Высота: {e.Location.Altitude.Value:F1} м"
                : "Высота: определение...";
            GpsAccuracyText = $"Погрешность GPS: {e.Location.Accuracy:F0} м";
            var currentSpeedKmH = (e.Location.Speed.HasValue ? e.Location.Speed.Value : 0)*3.6;
            var currentPaseHKmText = PaceFormatter.Format(currentSpeedKmH);
            CurrentSpeedKmHText = $"Текущая скорость {currentSpeedKmH:F2} км/ч, темп: {currentPaseHKmText}";
            TotalDistanceMText = $"Пройденный путь: {e.TotalDistanceM}м";
            TotalAscentMText = $"Общий набор высоты: {e.TotalAscentM}м";
            TotalDescentMText = $"Общий спуск: {e.TotalDescentM}м";
        });
    }
}
