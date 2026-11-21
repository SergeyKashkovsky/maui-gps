using BurnOffTheFat.Common.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GpsTrackerPrototype.ViewModels;

/// <summary>
/// Модель представления главного окна
/// </summary>
public partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// Коллекция режимов тренировки для отображения в выпадающем списке
    /// </summary>
    public IReadOnlyList<SpeedMode> SpeedModes { get; } =
        Enum.GetValues<SpeedMode>();
    /// <summary>
    /// Выбранный режим скорости
    /// </summary>
    [ObservableProperty]
    private SpeedMode _selectedSpeedMode = SpeedMode.Walking;

    /// <summary>
    /// Обработчик выбора режима скорости
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedSpeedModeChanged(SpeedMode value)
    {
        //TODO: обработка режима
    }

    /// <summary>
    /// Обработчик нажатия на кнопку запуска тернировки
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task StartAsync()
    {
        // Передаём выбранный режим на вторую страницу через параметр
        await Shell.Current.GoToAsync(nameof(Views.TrainingPage),
            new Dictionary<string, object>
            {
                ["SpeedMode"] = SelectedSpeedMode
            });
    }
}
