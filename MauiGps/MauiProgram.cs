using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Services;
using CommunityToolkit.Maui;
using MauiGps.ViewModel;
using Microsoft.Extensions.Logging;

namespace MauiGps
{
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

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}