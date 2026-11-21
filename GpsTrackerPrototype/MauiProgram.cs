using CommunityToolkit.Maui;
using GpsTrackerPrototype.ViewModels;
using GpsTrackerPrototype.Views;
using Microsoft.Extensions.Logging;

namespace GpsTrackerPrototype
{
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


#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
