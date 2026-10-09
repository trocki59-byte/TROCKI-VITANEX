using System.Globalization;

namespace VITANEX;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var tr = new CultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentCulture = tr;
        CultureInfo.DefaultThreadCurrentUICulture = tr;
        CultureInfo.CurrentCulture = tr;
        CultureInfo.CurrentUICulture = tr;

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        return builder.Build();
    }
}
