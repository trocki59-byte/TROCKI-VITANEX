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

    // Yürüme sayar: Android'de arka plan hizmeti ekran kilitliyken de sayar.
    protected override async void OnStart()
    {
        base.OnStart();
        try { await Services.StepCounter.Resume(); } catch { }
        try { await Services.Payments.Reschedule(); } catch { }
    }

    protected override async void OnResume()
    {
        base.OnResume();
        try { await Services.StepCounter.Resume(); } catch { }
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        Services.StepCounter.Stop();
    }
}
