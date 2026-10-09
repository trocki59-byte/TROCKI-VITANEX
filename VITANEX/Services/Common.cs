using System.Globalization;

namespace VITANEX.Services;

public static class AppColors
{
    public static readonly Color Primary = Color.FromArgb("#0B4FD6");
    public static Color PrimaryDark => Theme.IsDark ? Color.FromArgb("#8FB8FF") : Color.FromArgb("#0A2E8A");
    public static readonly Color Accent = Color.FromArgb("#1E88FF");
    public static Color PrimaryLight => Theme.IsDark ? Color.FromArgb("#1E2D4D") : Color.FromArgb("#E6EEFC");
    public static Color Text => Theme.IsDark ? Color.FromArgb("#E8EEFA") : Color.FromArgb("#0E1B3D");
    public static Color Muted => Theme.IsDark ? Color.FromArgb("#8D9BBB") : Color.FromArgb("#8A97B2");
    public static Color Card => Theme.IsDark ? Color.FromArgb("#18233A") : Colors.White;
    public static Color Surface2 => Theme.IsDark ? Color.FromArgb("#22304D") : Color.FromArgb("#F4F7FC");
    public static Color Line => Theme.IsDark ? Color.FromArgb("#2A3854") : Color.FromArgb("#E3EAF5");
    public static readonly Color Green = Color.FromArgb("#22B573");
    public static readonly Color Orange = Color.FromArgb("#FF8A1F");
    public static readonly Color Red = Color.FromArgb("#F0384B");
    public static readonly Color Purple = Color.FromArgb("#7B4DFF");
    public static readonly Color Pink = Color.FromArgb("#E8457C");
    public static readonly Color Yellow = Color.FromArgb("#FFB300");
}

/// <summary>Kullanıcı tercihleri (Preferences içinde saklanır).</summary>
public static class AppSettings
{
    public static string UserName
    {
        get => Preferences.Default.Get("user_name", "Troçki");
        set => Preferences.Default.Set("user_name", value);
    }

    public static string Currency
    {
        get => Preferences.Default.Get("currency", "₺");
        set => Preferences.Default.Set("currency", value);
    }

    public static int WaterGoal
    {
        get => Preferences.Default.Get("goal_water", 2000);
        set => Preferences.Default.Set("goal_water", value);
    }

    public static double SleepGoal
    {
        get => Preferences.Default.Get("goal_sleep", 8.0);
        set => Preferences.Default.Set("goal_sleep", value);
    }

    public static int ExerciseGoal
    {
        get => Preferences.Default.Get("goal_exercise", 30);
        set => Preferences.Default.Set("goal_exercise", value);
    }

    public static int CalorieGoal
    {
        get => Preferences.Default.Get("goal_calorie", 2000);
        set => Preferences.Default.Set("goal_calorie", value);
    }

    public static int StepGoal
    {
        get => Preferences.Default.Get("goal_steps", 8000);
        set => Preferences.Default.Set("goal_steps", value);
    }

    /// <summary>Adım uzunluğu (cm).</summary>
    public static int StrideCm
    {
        get => Preferences.Default.Get("stride_cm", 72);
        set => Preferences.Default.Set("stride_cm", value);
    }
}

/// <summary>Biçimlendirme ve sayı okuma yardımcıları.</summary>
public static class Fmt
{
    public static readonly CultureInfo TR = new("tr-TR");

    public static string Money(decimal v, int decimals = 2) =>
        $"{AppSettings.Currency} {v.ToString(decimals == 0 ? "N0" : "N2", TR)}";

    static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Replace(" ", "").Replace("₺", "");
        if (s.Contains(',') && s.Contains('.')) s = s.Replace(".", "");   // 12.450,50
        return s.Replace(',', '.');
    }

    public static decimal? ParseDecimal(string s)
    {
        s = Normalize(s);
        return s != null && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static double? ParseDouble(string s)
    {
        s = Normalize(s);
        return s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static int? ParseInt(string s)
    {
        var d = ParseDouble(s);
        return d.HasValue ? (int)Math.Round(d.Value) : null;
    }

    public static string Num(double? v, string format = "0.#") => v.HasValue ? v.Value.ToString(format, TR) : "";
}

public static class Ui
{
    public static Page CurrentPage => Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    public static Task Alert(string title, string message) =>
        CurrentPage.DisplayAlertAsync(title, message, "Tamam");

    public static Task<bool> Confirm(string title, string message, string yes = "Evet") =>
        CurrentPage.DisplayAlertAsync(title, message, yes, "Vazgeç");

    public static Style Style(string key) =>
        Application.Current.Resources.TryGetValue(key, out var v) ? v as Style : null;

    public static void SetChip(Button b, bool selected)
    {
        b.BackgroundColor = selected ? AppColors.Primary : AppColors.PrimaryLight;
        b.TextColor = selected ? Colors.White : (Theme.IsDark ? AppColors.PrimaryDark : AppColors.Primary);
    }

    /// <summary>Grid için sütun tanımı: -1 = yıldız (*), 0 = Auto, diğerleri sabit genişlik.</summary>
    public static void Cols(Grid g, params double[] widths)
    {
        g.ColumnDefinitions.Clear();
        foreach (var w in widths)
            g.ColumnDefinitions.Add(new ColumnDefinition(
                w < 0 ? GridLength.Star : w == 0 ? GridLength.Auto : new GridLength(w)));
    }
}

/// <summary>
/// Telefonda kartları tek sütunda, tablette (genişlik >= 760) iki sütunda gösterir.
/// </summary>
public class TwoColumn
{
    readonly Grid _host;
    readonly VerticalStackLayout _a, _b;
    readonly List<View> _cards;
    bool? _wide;

    public TwoColumn(Grid host, VerticalStackLayout colA, VerticalStackLayout colB)
    {
        _host = host;
        _a = colA;
        _b = colB;
        _cards = colA.Children.OfType<View>().ToList();
    }

    public void Apply(double width)
    {
        if (width <= 0) return;
        bool wide = width >= 760;
        if (_wide == wide) return;
        _wide = wide;

        _a.Children.Clear();
        _b.Children.Clear();
        _host.ColumnDefinitions.Clear();
        _host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        if (wide)
        {
            _host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            for (int i = 0; i < _cards.Count; i++)
                (i % 2 == 0 ? _a : _b).Children.Add(_cards[i]);
            _b.IsVisible = true;
        }
        else
        {
            foreach (var c in _cards) _a.Children.Add(c);
            _b.IsVisible = false;
        }
    }
}
