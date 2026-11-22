using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GpsTrackerPrototype.ViewModels;

/// <summary>
/// МОдель представления окна тренировки
/// </summary>
[QueryProperty(nameof(CurrentSpeedMode), "SpeedMode")]
public partial class TrainingViewModel : ObservableObject, IQueryAttributable, IDisposable
{
    private readonly CurrentUserService _userService;
    private readonly TrackRecordingCoordinator _coordinator;
    private readonly IGpsLocationService _gpsService;
    private IDispatcherTimer? _uiTimer;
    private DateTime _startTime;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private double _distanceMeters;

    [ObservableProperty]
    private double _currentSpeedKmH;

    [ObservableProperty]
    private double _totalAscentM;
    [ObservableProperty]
    private double _totalDescentM;

    [ObservableProperty]
    private TimeSpan _elapsedTime;
    /// <summary>
    /// Текущий режим тренировки
    /// </summary>
    [ObservableProperty]
    private SpeedMode _currentSpeedMode;

    public TrainingViewModel(
        CurrentUserService userService,
        TrackRecordingCoordinator coordinator,
        IGpsLocationService gpsService)
    {
        _userService = userService;
        _coordinator = coordinator;
        _gpsService = gpsService;

        // Подписываемся на обновление телеметрии в UI
        _gpsService.LocationReceived += OnLocationReceived;
    }

    /// <summary>
    /// Вызывается автоматически при навигации через Shell.Current.GoToAsync
    /// </summary>
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("SpeedMode", out var modeObj) && modeObj is SpeedMode mode)
        {
            CurrentSpeedMode = mode;
        }

        await StartWorkoutSessionAsync();
    }

    /// <summary>
    /// Запуск сессии тренировки
    /// </summary>
    /// <returns></returns>
    private async Task StartWorkoutSessionAsync()
    {
        if (_coordinator.IsRecording) return;

        var user = _userService.CurrentUser ?? await _userService.InitializeAsync();

        string title = $"Поход {DateTime.Now:dd.MM.yyyy HH:mm}";
        bool started = await _coordinator.StartWorkoutAsync(user, title);

        if (!started)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка GPS", "Не удалось запустить отслеживание местоположения.", "ОК");
            await Shell.Current.GoToAsync("..");
            return;
        }

        IsRecording = true;
        _startTime = DateTime.UtcNow;
        StartUiTimer();
    }

    /// <summary>
    /// Запуск таймера для отображения
    /// </summary>
    private void StartUiTimer()
    {
        _uiTimer = Application.Current?.Dispatcher.CreateTimer();
        if (_uiTimer != null)
        {
            _uiTimer.Interval = TimeSpan.FromSeconds(1);
            _uiTimer.Tick += (s, e) => ElapsedTime = DateTime.UtcNow - _startTime;
            _uiTimer.Start();
        }
    }

    private void OnLocationReceived(object? sender, LocationPointEventArgs e)
    {
        // Обновляем свойства на главном потоке для привязок в XAML
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DistanceMeters = _gpsService.TotalDistance;
            CurrentSpeedKmH = _gpsService.CurrentSpeedKmH;
            TotalAscentM = _gpsService.TotalAscentM;
        });
    }

    /// <summary>
    /// Обработка нажатия на кнопку остановки тренировки
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    public async Task StopAsync()
    {
        if (!IsRecording) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Завершение",
            "Завершить и сохранить текущую тренировку?",
            "Да", "Отмена");

        if (!confirm) return;

        _uiTimer?.Stop();
        await _coordinator.StopWorkoutAsync();
        IsRecording = false;

        // Возвращаемся на главный экран
        await Shell.Current.GoToAsync("..");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _uiTimer?.Stop();
        _gpsService.LocationReceived -= OnLocationReceived;
    }
}
