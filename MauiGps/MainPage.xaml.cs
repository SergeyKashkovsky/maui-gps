using MauiGps.ViewModel;

namespace MauiGps;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel vm)//GpsLocationService gpsLocationService)
    {
        InitializeComponent();
        // Привязываем BindingContext страницы к сервису
        BindingContext = vm;
    }
}
