using BurnOffTheFat.Common.Interfaces;
using BurnOffTheFat.Core.Data;
using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Services;
using BurnOffTheFat.Core.Utils;
using CommunityToolkit.Maui;
using GpsTrackerPrototype.ViewModels;
using GpsTrackerPrototype.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace GpsTrackerPrototype;

/// <summary>
/// Точка входа в программу
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseMauiCommunityToolkit()
            .Services
                .AddTransient<MainViewModel>()
                .AddTransient<MainPage>()
                .AddTransient<TrainingPage>()
                .AddTransient<TrainingViewModel>()
                .AddSingleton<ITrackFileService, TrackFileService>();
        
        // Путь в защищенном хранилище профиля Android
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "gps_tracker.db3");
        // Регистрируем фабрику контекстов БД
        builder.Services.AddDbContextFactory<TrackerDbContext>(options =>
            options.UseSqlite($"Filename={dbPath}"));
        // Регистрируем сервисы из Core.Shared и Core.Maui
        builder.Services.AddSingleton<ITrackRepository, SqliteTrackRepository>();
        builder.Services.AddSingleton<IUserProfileRepository, SqliteUserProfileRepository>();
        builder.Services.AddSingleton<CurrentUserService>();
        builder.Services.AddSingleton<IGpsLocationService, GpsLocationService>();
        builder.Services.AddSingleton<TrackRecordingCoordinator>();

        var logDirectory = FileSystemUtils.GetApllicationPublicDirectory();
        GlobalDiagnosticsContext.Set("LogDir", logDirectory);
        ConfigureNLog();
        builder.Logging.ClearProviders();
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
