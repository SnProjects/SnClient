using System.Diagnostics;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SnClient.Pages;
using Xe.AcrylicView;

namespace SnClient;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseAcrylicView()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        
        Trace.WriteLine("MauiProgram: MauiApp created");

        return builder.Build();
    }
}