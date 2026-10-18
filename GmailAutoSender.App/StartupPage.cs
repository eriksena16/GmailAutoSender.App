using Microsoft.Extensions.DependencyInjection;

namespace GmailAutoSender.App
{
    public class StartupPage : ContentPage
    {
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                var destination = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "credentials.json");

                if (!File.Exists(destination))
                {
                    await using var source =
                        await FileSystem.OpenAppPackageFileAsync(
                            "credentials.json");

                    await using var target =
                        File.Create(destination);

                    await source.CopyToAsync(target);
                }

                var services = Application.Current!.Handler!.MauiContext!.Services;

                var mainPage = services.GetRequiredService<MainPage>();

                Application.Current.Windows[0].Page = mainPage;
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro de inicialização",
                    ex.Message,
                    "OK");
            }
        }
    }

}
