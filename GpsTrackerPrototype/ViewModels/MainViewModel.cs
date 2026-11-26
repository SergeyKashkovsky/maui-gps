using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Interfaces;
using BurnOffTheFat.Common.Models.Dto;
using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Models;
using BurnOffTheFat.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace GpsTrackerPrototype.ViewModels;

/// <summary>
/// Модель представления главного окна
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly CurrentUserService _userService;
    private readonly TrackRecordingCoordinator _coordinator;
    private readonly IGpsLocationService _gpsService;
    private readonly ITrackRepository _trackRepository;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSetDefaultModeVisible))]
    private UserProfile? _currentProfile;

    public MainViewModel(CurrentUserService userService,
        TrackRecordingCoordinator coordinator,
        IGpsLocationService gpsService,
        ITrackRepository trackRepository)
    {
        _userService = userService;
        _coordinator = coordinator;
        _gpsService = gpsService;
        _trackRepository = trackRepository;
    }

    /// <summary>
    /// Коллекция режимов тренировки для отображения в выпадающем списке
    /// </summary>
    public IReadOnlyList<SpeedMode> SpeedModes { get; } =
        Enum.GetValues<SpeedMode>();
    /// <summary>
    /// Список моделей для списка тренировок
    /// </summary>
    public ObservableCollection<TrainingCardModel> Trainings { get; } = new();
    /// <summary>
    /// Выбранный режим скорости
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSetDefaultModeVisible))]
    private SpeedMode _selectedSpeedMode = SpeedMode.Walking;

    /// <summary>
    /// Определяет, нужно ли показывать кнопку "Установить по умолчанию"
    /// </summary>
    public bool IsSetDefaultModeVisible =>
        CurrentProfile != null && SelectedSpeedMode != CurrentProfile.FastTreiningMode;


    /// <summary>
    /// Вызывается при появлении MainPage (например, в OnAppearing)
    /// </summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (CurrentProfile == null)
        {
            CurrentProfile = await _userService.InitializeAsync();
            SelectedSpeedMode = CurrentProfile.FastTreiningMode;
            OnPropertyChanged(nameof(SelectedSpeedMode));
            _gpsService.SpeedMode = SelectedSpeedMode;

            // Проверяем аварийный трек только при старте приложения
            var recoveredTrack = await _coordinator.RecoverUnfinishedTrackAsync(CurrentProfile.Id);
            if (recoveredTrack != null)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Трек восстановлен",
                    $"Предыдущая запись была завершена аварийно. Сохранено {recoveredTrack.TotalDistanceMeters:F0} м.",
                    "ОК");
            }
#if ANDROID
            AndriodPermissionService.CheckAndRequestBatteryOptimizations();
#endif
        }
        await LoadTrainingsAsync();
    }
    /// <summary>
    /// Загрузка списка тренировок
    /// </summary>
    /// <returns></returns>
    private async Task LoadTrainingsAsync()
    {
        if (CurrentProfile == null) return;

        var itemsFromDb = await _trackRepository.GetUserTracksAsync(CurrentProfile.Id);

        Trainings.Clear();
        foreach (var item in itemsFromDb)
        {
            Trainings.Add(TrainingCardModel.FromTrack(item));
        }
    }

    /// <summary>
    /// Обработчик выбора режима скорости
    /// </summary>
    /// <param name="value"></param>
    async partial void OnSelectedSpeedModeChanged(SpeedMode value)
    {
        _gpsService.SpeedMode = value;
    }

    /// <summary>
    /// Сохраняет выбранный режим как режим по умолчанию в профиле пользователя
    /// </summary>
    [RelayCommand]
    private async Task SetDefaultSpeedModeAsync()
    {
        if (CurrentProfile == null)
            return;

        // Обновляем в БД через ваш сервис
        await _userService.UpdateFastTrainingModeAsync(SelectedSpeedMode);

        // Обновляем локальное свойство профиля, чтобы кнопка скрылась
        CurrentProfile.FastTreiningMode = SelectedSpeedMode;
        OnPropertyChanged(nameof(IsSetDefaultModeVisible));
    }

    /// <summary>
    /// Обработчик нажатия на кнопку запуска тернировки
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task StartAsync()
    {
        CurrentProfile ??= await _userService.InitializeAsync();
        await _gpsService.StartListenAsync();
        // Передаём выбранный режим на вторую страницу через параметр
        await Shell.Current.GoToAsync(nameof(Views.TrainingPage),
            new Dictionary<string, object>
            {
                ["SpeedMode"] = SelectedSpeedMode
            });
    }
}
