using BurnOffTheFat.Common.Interfaces;
using BurnOffTheFat.Core.Data;
using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Services;
using CommunityToolkit.Maui;
using GpsTrackerPrototype.ViewModels;
using GpsTrackerPrototype.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            .AddTransient<TrainingViewModel>();
        
        // Путь в защищенном хранилище профиля Android
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "gps_tracker.db3");
        // Регистрируем фабрику контекстов БД
        builder.Services.AddDbContextFactory<TrackerDbContext>(options =>
            options.UseSqlite($"Filename={dbPath}"));
        // Регистрируем сервисы из Core.Shared и Core.Maui
        builder.Services.AddSingleton<ITrackRepository, SqliteTrackRepository>();
        builder.Services.AddSingleton<CurrentUserService>();
        builder.Services.AddSingleton<IGpsLocationService, GpsLocationService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
