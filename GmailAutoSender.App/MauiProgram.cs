
using Microsoft.Extensions.Logging;
using GmailAutoSender.Infrastructure;

namespace GmailAutoSender.App;

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
            });

        var credentialsPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "credentials.json");

        var tokenDirectory = Path.Combine(
            FileSystem.AppDataDirectory,
            "OAuth");

        builder.Services.AddInfrastructure(
            credentialsPath,
            tokenDirectory);

        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
