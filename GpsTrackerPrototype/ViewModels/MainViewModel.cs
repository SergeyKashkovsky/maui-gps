using BurnOffTheFat.Common.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GpsTrackerPrototype.ViewModels;

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
        
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        // Передаём выбранный режим на вторую страницу через параметр
        await Shell.Current.GoToAsync(nameof(Views.SecondPage),
            new Dictionary<string, object>
            {
                ["SpeedMode"] = SelectedSpeedMode
            });
    }
}
