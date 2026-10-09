namespace VITANEX.Pages;

/// <summary>Açılış ekranı: VİTANEX logosu ve "by TROÇKİ" imzası, ardından ana uygulama.</summary>
public partial class SplashPage : ContentPage
{
    bool _started;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;

        await LogoBlock.FadeTo(1, 450);
        await SignBlock.FadeTo(1, 350);
        await Task.Delay(900);

        if (Window != null)
            Window.Page = new AppShell();
    }
}
