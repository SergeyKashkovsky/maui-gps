using BurnOffTheFat.Common.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GpsTrackerPrototype.ViewModels;

/// <summary>
/// МОдель представления окна тренировки
/// </summary>
[QueryProperty(nameof(CurrentSpeedMode), "SpeedMode")]
public partial class TrainingViewModel : ObservableObject
{
    /// <summary>
    /// Текущий режим тренировки
    /// </summary>
    [ObservableProperty]
    private SpeedMode _currentSpeedMode;

    /// <summary>
    /// Обработка нажатия на кнопку остановки тренировки
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task StopAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
