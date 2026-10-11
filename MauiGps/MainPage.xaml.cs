using MauiGps.ViewModel;

namespace MauiGps;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel vm)//GpsLocationService gpsLocationService)
    {
        InitializeComponent();
        // Привязываем BindingContext страницы к сервису
        BindingContext = vm;
        _viewModel = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Безопасно запускаем инициализацию и старт прослушивания GPS
        if (_viewModel.InitializeCommand.CanExecute(null))
        {
            await _viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
