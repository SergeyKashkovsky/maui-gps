using BurnOffTheFat.Common.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GpsTrackerPrototype.ViewModels;

[QueryProperty(nameof(CurrentSpeedMode), "SpeedMode")]
public partial class SecondViewModel : ObservableObject
{
    [ObservableProperty]
    private SpeedMode _currentSpeedMode;

    [RelayCommand]
    private async Task StopAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
