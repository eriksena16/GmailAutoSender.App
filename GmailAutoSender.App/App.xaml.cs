//namespace GmailAutoSender.App
//{
//    public partial class App : Application
//    {
//        public App()
//        {
//            InitializeComponent();

//            MainPage = new AppShell();
//        }

//        protected override Window CreateWindow(
//        IActivationState? activationState)
//        {
//            return new Window(new StartupPage());
//        }
//    }

//}
//namespace GmailAutoSender.App;

//public partial class App : Application
//{
//    public App()
//    {
//        InitializeComponent();
//    }

//    protected override Window CreateWindow(
//        IActivationState? activationState)
//    {
//        return new Window(new ContentPage
//        {
//            Content = new Label
//            {
//                Text = "Aplicativo iniciado com sucesso",
//                HorizontalOptions = LayoutOptions.Center,
//                VerticalOptions = LayoutOptions.Center
//            }
//        });
//    }
//}

namespace GmailAutoSender.App;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        return new Window(_mainPage);
    }
}