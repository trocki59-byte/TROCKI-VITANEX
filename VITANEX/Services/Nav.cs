using VITANEX.Pages;

namespace VITANEX.Services;

/// <summary>Modüller arası gezinme.</summary>
public static class Nav
{
    public record ModuleDef(string Key, string Emoji, string Title);

    public static readonly ModuleDef[] Modules =
    {
        new("health", "❤️", "Sağlık"),
        new("nutrition", "🍽️", "Beslenme"),
        new("finance", "💰", "Finans"),
        new("tasks", "✅", "Görevler"),
        new("education", "📘", "Eğitim"),
        new("stats", "📊", "İstatistikler"),
        new("social", "👥", "Sosyal Yaşam"),
        new("development", "🌱", "Kişisel Gelişim"),
        new("career", "💼", "İş / Kariyer"),
        new("reports", "📄", "Raporlar"),
        new("backup", "☁️", "Yedekleme"),
        new("settings", "⚙️", "Ayarlar"),
    };

    static bool _busy;

    public static async Task Push(Page page)
    {
        if (_busy) return;
        _busy = true;
        try { await Shell.Current.Navigation.PushAsync(page); }
        finally { _busy = false; }
    }

    public static async Task Open(string key)
    {
        switch (key)
        {
            case "health": await Push(new HealthPage()); break;
            case "nutrition": await Push(new NutritionPage()); break;
            case "finance": await Push(new FinancePage()); break;
            case "tasks": await Shell.Current.GoToAsync("//tasks"); break;
            case "stats": await Shell.Current.GoToAsync("//stats"); break;
            case "reports": await Shell.Current.GoToAsync("//reports"); break;
            case "education":
            case "development":
            case "social":
            case "career": await Push(new ModulePage(key)); break;
            case "backup": await Push(new BackupPage()); break;
            case "settings": await Push(new SettingsPage()); break;
            case "reminders": await Push(new RemindersPage()); break;
        }
    }
}
