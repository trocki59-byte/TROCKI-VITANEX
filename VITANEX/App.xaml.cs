namespace VITANEX;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Services.Theme.Init(this);
    }

    protected override Window CreateWindow(IActivationState activationState) =>
        new Window(new Pages.SplashPage()) { Title = "TROÇKİ VİTANEX" };
}
