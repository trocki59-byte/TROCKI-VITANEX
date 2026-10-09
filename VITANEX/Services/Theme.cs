namespace VITANEX.Services;

/// <summary>Açık / koyu tema yönetimi. "system" seçilirse cihazın temasını izler.</summary>
public static class Theme
{
    public static readonly string[] Modes = { "system", "light", "dark" };
    public static readonly string[] Names = { "Cihaz ayarına göre", "☀️ Açık", "🌙 Koyu" };

    public static string Mode
    {
        get => Preferences.Default.Get("theme", "system");
        private set => Preferences.Default.Set("theme", value);
    }

    public static bool IsDark { get; private set; }

    static readonly (string Key, string Light, string Dark)[] Palette =
    {
        ("PageBg",        "#F2F6FC",   "#0D1424"),
        ("CardBg",        "#FFFFFF",   "#18233A"),
        ("TextDark",      "#0E1B3D",   "#E8EEFA"),
        ("TextMuted",     "#5B6B8C",   "#9AA8C7"),
        ("Line",          "#E3EAF5",   "#2A3854"),
        ("PrimaryLight",  "#E6EEFC",   "#1E2D4D"),
        ("PrimaryDark",   "#0A2E8A",   "#8FB8FF"),
        ("NavBg",         "#FFFFFF",   "#121B2E"),
        ("TabUnselected", "#8A97B2",   "#6F7FA3"),
        ("HeroBox",       "#E6FFFFFF", "#D9121B2E"),
        ("DangerBg",      "#FDE7EA",   "#3A1D25"),
        ("Placeholder",   "#9AA7C2",   "#6F7FA3"),
        ("Surface2",      "#F4F7FC",   "#22304D"),
        ("ChipText",      "#0B4FD6",   "#8FB8FF"),
    };

    static bool _hooked;

    static Application _app;

    public static void Init(Application app)
    {
        _app = app;
        if (!_hooked)
        {
            app.RequestedThemeChanged += (s, e) => { if (Mode == "system") Apply(); };
            _hooked = true;
        }
        Apply();
    }

    public static void Set(string mode)
    {
        Mode = Modes.Contains(mode) ? mode : "system";
        Apply();
    }

    static void Apply()
    {
        var app = _app ?? Application.Current;
        if (app == null) return;

        app.UserAppTheme = Mode switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        IsDark = Mode == "dark" || (Mode == "system" && app.PlatformAppTheme == AppTheme.Dark);

        foreach (var (key, light, dark) in Palette)
            app.Resources[key] = Color.FromArgb(IsDark ? dark : light);
    }
}
