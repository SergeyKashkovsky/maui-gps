using Android.App;
using Android.OS;
using AndroidX.Core.App;
using Android.Content;
using Android.Content.PM;

namespace BurnOffTheFat;
/// <summary>
/// Сервис, создающий уведомление о том, что приложение выполняет работу
/// </summary>
[Service(Enabled = true, Name = "burnoffthefat.GpsService", ForegroundServiceType = ForegroundService.TypeLocation)]
public class GpsService : Service
{
    private const int NOTIFICATION_ID = 1001;
    private const string CHANNEL_ID = "gps_service_channel";

    public GpsService() : base() { }

    public GpsService(IntPtr handle, global::Android.Runtime.JniHandleOwnership transfer) : base(handle, transfer) { }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        try
        {
            CreateNotificationChannel();

            var notification = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle("Запись трека")
                .SetContentText("Приложение записывает ваш GPS-маршрут...")
                .SetSmallIcon(global::Android.Resource.Drawable.IcMenuCompass) // Проверьте, чтобы иконка существовала
                .SetOngoing(true)
                .Build();

            StartForeground(NOTIFICATION_ID, notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка запуска Foreground службы в OnCreate: {ex}");
        }
    }

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        return StartCommandResult.Sticky;
    }

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