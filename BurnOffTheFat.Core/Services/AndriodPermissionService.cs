namespace BurnOffTheFat.Core.Services;
/// <summary>
/// Сервис для выдачи разрешений на использование ресурсов Android и управления фоновой службой
/// </summary>
public static class AndriodPermissionService
{
#if ANDROID
    /// <summary>
    /// Запрос работы приложения как фоновой службы
    /// </summary>
    /// <returns></returns>
    public static async Task RequestBackgroundLocationPermissionAsync()
    {
        var bgStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (bgStatus != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.LocationAlways>();
    }


    /// <summary>
    /// Запустить диалог переключения режима энергосбережения, если оно ограничивает работу приложения в фоновом режиме.
    /// </summary>
    public static void CheckAndRequestBatteryOptimizations()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return;

        var powerManager = (Android.OS.PowerManager)activity.GetSystemService(Android.Content.Context.PowerService);

        string packageName = activity.PackageName;

        if (powerManager != null && !powerManager.IsIgnoringBatteryOptimizations(packageName))
        {
            Android.Content.Intent intent = new Android.Content.Intent();

            intent.SetAction(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse($"package:{packageName}"));

            // Запускаем Intent из контекста текущего окна приложения
            activity.StartActivity(intent);
        }
    }
    /// <summary>
    /// Запуск фоновой службы уведомлений
    /// </summary>
    public static void StartAndroidGpsService()
    {
        var context = Platform.AppContext;
        var intent = new Android.Content.Intent();
        intent.SetClassName(context.PackageName, "burnoffthefat.GpsService");

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }
    /// <summary>
    /// Остановка фоновой службы уведомлений
    /// </summary>
    public static void StopAndroidGpsService()
    {
        var context = Platform.AppContext;
        var intent = new Android.Content.Intent();
        intent.SetClassName(context.PackageName, "burnoffthefat.GpsService");
        Android.App.Application.Context.StopService(intent);
    }
#endif

}
