using GpsTrackerPrototype.ViewModels;

namespace GpsTrackerPrototype;

/// <summary>
/// Главное окно
/// </summary>
public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

}
