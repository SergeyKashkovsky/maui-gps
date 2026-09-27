using Microsoft.Maui.Devices.Sensors;
namespace MauiGps;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnCheckGpsClicked(object sender, EventArgs e)
    {
        // 1. Проверяем статус GPS (разрешения и включен ли датчик)
        bool isGpsEnabled = await CheckGpsStatusAsync();
        StatusLabel.Text = isGpsEnabled ? "Статус: GPS Доступен" : "Статус: GPS Отключен или нет разрешений";

        if (!isGpsEnabled) return;

        // 2. Получаем примерные координаты (быстро, по вышкам/Wi-Fi, экономит батарею)
        await GetCoarseLocationAsync();

        // 3. Получаем точные координаты (дольше, напрямую со спутников GPS)
        await GetFineLocationAsync();
    }
    private async Task<bool> CheckGpsStatusAsync()
    {
        try
        {
            // Проверяем, выдано ли разрешение
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted) return false;

            // Проверяем, включена ли геолокация в настройках устройства
            // (В .NET MAUI это можно косвенно понять, попытавшись сделать быстрый запрос)
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task GetCoarseLocationAsync()
    {
        try
        {
            // GeolocationAccuracy.Low или Medium подходят для примерных координат
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                CoarseCoordsLabel.Text = $"Примерные: {location.Latitude:F4}, {location.Longitude:F4} (Погрешность: {location.Accuracy}м)";
            }
        }
        catch (Exception ex)
        {
            CoarseCoordsLabel.Text = "Примерные: Ошибка получения";
        }
    }
    private async Task GetFineLocationAsync()
    {
        try
        {
            // GeolocationAccuracy.High или Best задействуют полноценный GPS для точных координат
            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(15));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                FineCoordsLabel.Text = $"Точные: {location.Latitude:F6}, {location.Longitude:F6} (Погрешность: {location.Accuracy}м)";
            }
        }
        catch (Exception ex)
        {
            FineCoordsLabel.Text = "Точные: Ошибка получения";
        }
    }

    /*private void OnCounterClicked(object? sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }*/
}
