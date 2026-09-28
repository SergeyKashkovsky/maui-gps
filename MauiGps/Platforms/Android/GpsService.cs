using Android.App;
using Android.OS;
using AndroidX.Core.App;
using Android.Content;
using Android.Content.PM;

namespace MauiGps;
/// <summary>
/// Сервис, создающий уведомление о том, что приложение выполняет работу
/// </summary>
[Service(ForegroundServiceType = ForegroundService.TypeLocation)]
public class GpsService : Service
{
    private const int NOTIFICATION_ID = 1001;
    private const string CHANNEL_ID = "gps_service_channel";

    public override IBinder? OnBind(Intent? intent) => null;

    /// <inheritdoc/>
    /// <remarks>Метод создания уведомления при старте сервиса</remarks>
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();

        var notification = new NotificationCompat.Builder(this, CHANNEL_ID)
            .SetContentTitle("Запись трека")?
            .SetContentText("Приложение записывает ваш GPS-маршрут...")?
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuCompass)? // Стандартная иконка компаса
            .SetOngoing(true)?
            .Build();

        // Запускаем службу в переднем плане
        StartForeground(NOTIFICATION_ID, notification);

        return StartCommandResult.Sticky;
    }
    /// <summary>
    /// Создание канала уведомления
    /// </summary>
    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(CHANNEL_ID, "GPS Tracking", NotificationImportance.Low);
            var manager = GetSystemService(NotificationService) as NotificationManager;
            manager?.CreateNotificationChannel(channel);
        }
    }
}