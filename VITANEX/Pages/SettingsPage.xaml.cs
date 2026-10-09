using VITANEX.Services;

namespace VITANEX.Pages;

public partial class SettingsPage : ContentPage
{
    static readonly string[] Currencies = { "₺", "$", "€", "£" };

    public SettingsPage()
    {
        InitializeComponent();
        CurrencyField.ItemsSource = Currencies;
        ThemeField.ItemsSource = Theme.Names;
    }

    bool _loading;

    void OnThemeChanged(object sender, EventArgs e)
    {
        if (_loading || ThemeField.SelectedIndex < 0) return;
        Theme.Set(Theme.Modes[ThemeField.SelectedIndex]);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NameField.Text = AppSettings.UserName;
        CurrencyField.SelectedItem = Currencies.Contains(AppSettings.Currency) ? AppSettings.Currency : "₺";
        _loading = true;
        ThemeField.SelectedIndex = Math.Max(0, Array.IndexOf(Theme.Modes, Theme.Mode));
        _loading = false;
        WaterField.Text = AppSettings.WaterGoal.ToString();
        SleepField.Text = AppSettings.SleepGoal.ToString("0.#", Fmt.TR);
        ExerciseField.Text = AppSettings.ExerciseGoal.ToString();
        CalorieField.Text = AppSettings.CalorieGoal.ToString();

        var db = new FileInfo(Db.DbPath);
        AppInfoLabel.Text =
            $"TROÇKİ VİTANEX — Kişisel Yaşam Yönetim Sistemi\n" +
            $"Geliştirici: TROÇKİ · Düşün • Keşfet • Üret • Paylaş\n" +
            $"Sürüm {AppInfo.Current.VersionString} (yapı {AppInfo.Current.BuildString})\n" +
            $"Cihaz: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} · Android {DeviceInfo.Current.VersionString} · {DeviceInfo.Current.Idiom}\n" +
            $"Veritabanı: {(db.Exists ? $"{db.Length / 1024.0:0.#} KB" : "—")}";
    }

    async void OnSave(object sender, EventArgs e)
    {
        AppSettings.UserName = string.IsNullOrWhiteSpace(NameField.Text) ? "Troçki" : NameField.Text.Trim();
        AppSettings.Currency = CurrencyField.SelectedItem as string ?? "₺";
        AppSettings.WaterGoal = Math.Clamp(Fmt.ParseInt(WaterField.Text) ?? 2000, 250, 10000);
        AppSettings.SleepGoal = Math.Clamp(Fmt.ParseDouble(SleepField.Text) ?? 8, 3, 14);
        AppSettings.ExerciseGoal = Math.Clamp(Fmt.ParseInt(ExerciseField.Text) ?? 30, 5, 600);
        AppSettings.CalorieGoal = Math.Clamp(Fmt.ParseInt(CalorieField.Text) ?? 2000, 800, 6000);
        await DisplayAlertAsync("Kaydedildi", "Ayarlarınız kaydedildi.", "Tamam");
    }

    async void OnReminders(object s, EventArgs e) => await Nav.Open("reminders");
    async void OnBackup(object s, EventArgs e) => await Nav.Open("backup");

    async void OnWipe(object sender, EventArgs e)
    {
        if (!await Ui.Confirm("Dikkat", "Tüm kayıtlar kalıcı olarak silinecek. Önce yedek almanız önerilir. Devam edilsin mi?", "Devam"))
            return;
        var confirm = await DisplayPromptAsync("Son onay", "Silmek için SİL yazın:", "Sil", "Vazgeç");
        if (!string.Equals(confirm?.Trim(), "SİL", StringComparison.CurrentCultureIgnoreCase)
            && !string.Equals(confirm?.Trim(), "SIL", StringComparison.OrdinalIgnoreCase))
            return;

        await Db.Close();
        foreach (var ext in new[] { "", "-wal", "-shm", "-journal" })
            if (File.Exists(Db.DbPath + ext)) File.Delete(Db.DbPath + ext);
        await Db.Get();
        await DisplayAlertAsync("Tamamlandı", "Tüm veriler silindi.", "Tamam");
        OnAppearing();
    }
}
