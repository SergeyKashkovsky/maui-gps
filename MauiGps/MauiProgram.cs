using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Services;
using CommunityToolkit.Maui;
using MauiGps.ViewModel;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace MauiGps;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        })
            .UseMauiCommunityToolkit()
            .Services
                .AddSingleton<IGpsLocationService, GpsLocationService>()
                .AddSingleton<IGpxFileService, GpxFileService>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<MainPage>();
        string logDirectory = FileSystem.Current.AppDataDirectory;

#if ANDROID
        // Для Android берем специальную папку Documents внутри Android/data/..., доступную по USB
        var context = Android.App.Application.Context;
        var externalFilesDir = context.GetExternalFilesDir(Android.OS.Environment.DirectoryDocuments);
        if (externalFilesDir != null)
        {
            logDirectory = externalFilesDir.AbsolutePath;
        }
#endif

        // 2. Передаем путь в глобальный контекст NLog
        GlobalDiagnosticsContext.Set("LogDir", logDirectory);

        // 3. Инициализируем конфигурацию NLog
        ConfigureNLog();

        // 4. Подключаем NLog к стандартной системе логирования MAUI
        builder.Logging.ClearProviders(); // Опционально: очищаем базовые провайдеры
        builder.Logging.AddNLog();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    /// <summary>
    /// Настройка журналирования
    /// </summary>
    private static void ConfigureNLog()
    {
        var config = new LoggingConfiguration();

        // Создаем целевую папку и файл "logs/app-log.txt" внутри нашей USB-доступной директории
        var fileTarget = new FileTarget("usbFileTarget")
        {
            FileName = "${gdc:item=LogDir}/logs/app-log.txt",
            Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}",
            ArchiveEvery = FileArchivePeriod.Day,
            MaxArchiveFiles = 7,
            KeepFileOpen = false // Позволяет ОС Windows легче читать файл "на лету" при подключении
        };

        // Записываем всё от уровня Trace/Debug и выше
        config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, fileTarget);
        LogManager.Configuration = config;
    }
}