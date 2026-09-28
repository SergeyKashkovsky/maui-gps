using CommunityToolkit.Maui.Storage;
using System.Globalization;
using System.Text;

namespace MauiGps;

public partial class MainPage : ContentPage
{
    int count = 0;
    private bool _isRecording = false;
    private List<Location> _trackPoints = new();
    private int _discardedCount = 0;

    public MainPage()
    {
        InitializeComponent();
    }
    /// <summary>
    /// Событие нажатия на кнопку Старт/Стоп
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnToggleTrackClicked(object sender, EventArgs e)
    {
        if (!_isRecording)
        {
            // Начинаем запись
            var hasPermission = await MainPage.CheckGpsPermissionAsync();
            if (!hasPermission)
            {
                await DisplayAlertAsync("Ошибка", "Нет разрешений на использование GPS", "OK");
                return;
            }

#if ANDROID
            var bgStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            if (bgStatus != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.LocationAlways>();
            }
#endif

            _trackPoints.Clear();
            _isRecording = true;
            _discardedCount = 0;
            TrackButton.Text = "Остановить и сохранить трек";
            TrackButton.BackgroundColor = Colors.Red;
            StatusLabel.Text = "Статус: Запись трека...";

            // запуск фоновой службы на android
#if ANDROID
            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(GpsService));
            Android.App.Application.Context.StartForegroundService(intent);
#endif

            // Подписываемся на обновление координат в реальном времени
            Geolocation.Default.LocationChanged += OnLocationChanged;

            // Запускаем прослушивание (High для высокой точности трека)
            var request = new GeolocationListeningRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(2));
            await Geolocation.Default.StartListeningForegroundAsync(request);
        }
        else
        {
            // Останавливаем запись
            _isRecording = false;
            TrackButton.Text = "Начать запись трека";
            TrackButton.BackgroundColor = Colors.Green;
            StatusLabel.Text = "Статус: Сохранение...";

            Geolocation.Default.LocationChanged -= OnLocationChanged;
            Geolocation.Default.StopListeningForeground();

            // остановка фоновой службы на android
#if ANDROID
            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(GpsService));
            Android.App.Application.Context.StopService(intent);
#endif

            // Сохраняем файл gpx
            if (_trackPoints.Count > 0)
            {
                var filePath = await MainPage.SaveGpxFileAsync(_trackPoints);
                await DisplayAlertAsync("Успех", $"Трек сохранен!\nКоличество точек: {_trackPoints.Count}\nФайл: {filePath}", "OK");
            }
            else
            {
                await DisplayAlertAsync("Внимание", "Не удалось записать ни одной точки. Файл не создан.", "OK");
            }
            StatusLabel.Text = "Статус: Готов к записи";
        }
    }
    /// <summary>
    /// Реакция на измнение местоположения
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        // Если точность хуже 35 метров, GPS-приемник «потерялся», точку в трек не берем.
        if (e.Location == null ||
            (e.Location.Accuracy.HasValue && e.Location.Accuracy.Value > 35))
            return;
        
        var newLocation = e.Location;

        // Проверяем: если это не первая точка, сравниваем её с предыдущей
        if (_trackPoints.Count > 0)
        {
            var lastPoint = _trackPoints[^1];

            // Вычисляем расстояние между прошлой и новой точкой (в километрах)
            // и переводим в метры
            var distanceInMeters = Location.CalculateDistance(lastPoint, newLocation, DistanceUnits.Kilometers) * 1000;

            // Вычисляем разницу во времени между точками (в секундах)
            var timeDeltaInSeconds = (newLocation.Timestamp - lastPoint.Timestamp).TotalSeconds;

            // Если расстояние меньше 3 метров, считаем, что пользователь стоит на месте.
            // Игнорируем микро-колебания, чтобы трек не превращался в "паутину" на стоянках.
            if (distanceInMeters < 3.0)
            {
                _discardedCount++;
                return;
            }

            // фильтр отскоков (Реалистичная скорость):
            if (timeDeltaInSeconds > 0)
            {
                // Вычисляем скорость перемещения между этими двумя точками (в м/с)
                var calculatedSpeedMetresPerSecond = distanceInMeters / timeDeltaInSeconds;

                // Переводим в км/ч для удобства понимания (1 м/с = 3.6 км/ч)
                var calculatedSpeedKmH = calculatedSpeedMetresPerSecond * 3.6;

                // Безопасно считываем значение из интерфейса. 
                // Если поле пустое или там некорректные символы, используем 45.0 км/ч по умолчанию.
                if (!double.TryParse(MaxSpeedEntry.Text, NumberStyles.Any,
                                      CultureInfo.InvariantCulture, out double maxPossibleSpeed))
                    maxPossibleSpeed = 45.0;

                if (calculatedSpeedKmH > maxPossibleSpeed)
                {
                    // Точка совершила нереалистичный прыжок во времени и пространстве — это отскок!
                    System.Diagnostics.Debug.WriteLine($"[GPS Filter] Отскок заблокирован! Скорость: {calculatedSpeedKmH:F1} км/ч");
                    _discardedCount++;
                    return;
                }
            }
        }

        _trackPoints.Add(e.Location);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentCoordsLabel.Text = $"Текущие: {e.Location.Latitude:F6}, {e.Location.Longitude:F6} (Точек: {_trackPoints.Count}, отфильтровано: {_discardedCount})";
            // Проверяем, вернул ли GPS-датчик высоту
            if (e.Location.Altitude.HasValue)
            {
                AltitudeLabel.Text = $"Высота: {e.Location.Altitude.Value:F1} м";
            }
            else
            {
                AltitudeLabel.Text = "Высота: определение...";
            }
        });
    }
    /// <summary>
    /// Проверка прав на получение информации GPS
    /// </summary>
    /// <returns></returns>
    private static async Task<bool> CheckGpsPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }
        return status == PermissionStatus.Granted;
    }
    /// <summary>
    /// Сохранение файла в память
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    private static async Task<string> SaveGpxFileAsync(List<Location> points)
    {
        var gpxBuilder = new StringBuilder();

        // 1. Формируем структуру строго по стандарту, как в рабочем файле Locus Map
        gpxBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
        gpxBuilder.AppendLine("<gpx version=\"1.1\" creator=\"MauiGpsTracker\" xmlns=\"http://topografix.com\" xmlns:xsi=\"http://w3.org\" xsi:schemaLocation=\"http://topografix.com http://topografix.com/gpx.xsd\">");
        gpxBuilder.AppendLine("  <metadata>");
        gpxBuilder.AppendLine($"    <time>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</time>");
        gpxBuilder.AppendLine("  </metadata>");
        gpxBuilder.AppendLine("  <trk>");
        gpxBuilder.AppendLine($"    <name>Трек {DateTime.Now:yyyy-MM-dd HH:mm}</name>");
        gpxBuilder.AppendLine("    <trkseg>");

        foreach (var p in points)
        {
            var lat = p.Latitude.ToString(CultureInfo.InvariantCulture);
            var lon = p.Longitude.ToString(CultureInfo.InvariantCulture);
            var time = p.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");

            gpxBuilder.AppendLine($"      <trkpt lat=\"{lat}\" lon=\"{lon}\">");
            if (p.Altitude.HasValue)
            {
                var ele = p.Altitude.Value.ToString(CultureInfo.InvariantCulture);
                gpxBuilder.AppendLine($"        <ele>{ele}</ele>");
            }
            gpxBuilder.AppendLine($"        <time>{time}</time>");
            if (p.Accuracy.HasValue)
            {
                // Переводим метры в коэффициент HDOP (минимум 1.0)
                var hdopCalc = Math.Max(1.0, p.Accuracy.Value / 5.0);
                var hdop = hdopCalc.ToString("F2", CultureInfo.InvariantCulture);
                gpxBuilder.AppendLine($"        <hdop>{hdop}</hdop>");
            }
            gpxBuilder.AppendLine("      </trkpt>");
        }

        gpxBuilder.AppendLine("    </trkseg>");
        gpxBuilder.AppendLine("  </trk>");
        gpxBuilder.AppendLine("</gpx>");

        // Переводим строку XML в поток байт
        var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        // Переводим текст в массив байт с чистой кодировкой
        byte[] fileBytes = utf8WithoutBom.GetBytes(gpxBuilder.ToString());
        using var stream = new MemoryStream(fileBytes);

        string fileName = $"track_{DateTime.Now:yyyyMMdd_HHmmss}.gpx";

        try
        {
            // Открываем стандартный Android-диалог "Сохранить как..."
            // Пользователь сможет выбрать папку "Загрузки", "Документы" или любую другую на свое усмотрение
            var fileSaverResult = await FileSaver.Default.SaveAsync(fileName, stream, cancellationToken: default);

            if (fileSaverResult.IsSuccessful)
            {
                // Возвращаем путь, который выбрал пользователь
                return fileSaverResult.FilePath;
            }
            else
            {
                return "Сохранение отменено пользователем";
            }
        }
        catch (Exception ex)
        {
            return $"Ошибка при сохранении: {ex.Message}";
        }
    }
    /// <summary>
    /// Нажатие кнопки Обновить данные GPS
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnCheckGpsClicked(object sender, EventArgs e)
    {
        // 1. Проверяем статус GPS (разрешения и включен ли датчик)
        var isGpsEnabled = await CheckGpsStatusAsync();
        StatusLabel.Text = isGpsEnabled ? "Статус: GPS Доступен" : "Статус: GPS Отключен или нет разрешений";

        if (!isGpsEnabled) return;

        // 2. Получаем примерные координаты (быстро, по вышкам/Wi-Fi, экономит батарею)
        await GetCoarseLocationAsync();

        // 3. Получаем точные координаты (дольше, напрямую со спутников GPS)
        await GetFineLocationAsync();
    }
    /// <summary>
    /// Проверка статуса GPS
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// Получение приблизительных координат
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// Получение точных координат
    /// </summary>
    /// <returns></returns>
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
    
}
