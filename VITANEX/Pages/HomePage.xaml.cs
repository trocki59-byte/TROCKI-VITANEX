using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class HomePage : ContentPage
{
    int _cols = -1;
    bool? _wide;
    List<Reminder> _reminders = new();

    public HomePage()
    {
        InitializeComponent();
        ApplyLayout(400);
        SizeChanged += (s, e) => ApplyLayout(Width);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    // ───────── Telefon / tablet yerleşimi ─────────
    void ApplyLayout(double w)
    {
        if (w <= 0) return;
        bool wide = w >= 900;            // tablet yatay
        int cols = w >= 600 ? 6 : 4;     // tablet: 6 sütun modül
        if (_wide == wide && _cols == cols) return;

        _wide = wide;
        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (wide)
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));
            for (int i = 0; i < 5; i++) RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Place(HeaderView, 0, 0, 1, 2);
            Place(HeroView, 1, 0);
            Place(SummaryView, 2, 0);
            Place(ModulesGrid, 3, 0);
            Place(RemindersView, 1, 1, 3, 1);
            Place(FooterView, 4, 0, 1, 2);
        }
        else
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            for (int i = 0; i < 6; i++) RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Place(HeaderView, 0, 0);
            Place(HeroView, 1, 0);
            Place(SummaryView, 2, 0);
            Place(ModulesGrid, 3, 0);
            Place(RemindersView, 4, 0);
            Place(FooterView, 5, 0);
        }

        HeroView.HeightRequest = w >= 600 ? 210 : 176;

        if (_cols != cols)
        {
            _cols = cols;
            BuildModules(cols);
        }
        BuildReminders();
    }

    static void Place(View v, int row, int col, int rowSpan = 1, int colSpan = 1)
    {
        Grid.SetRow(v, row);
        Grid.SetColumn(v, col);
        Grid.SetRowSpan(v, rowSpan);
        Grid.SetColumnSpan(v, colSpan);
    }

    void BuildModules(int cols)
    {
        ModulesGrid.Children.Clear();
        ModulesGrid.ColumnDefinitions.Clear();
        ModulesGrid.RowDefinitions.Clear();
        for (int c = 0; c < cols; c++) ModulesGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        int rows = (Nav.Modules.Length + cols - 1) / cols;
        for (int r = 0; r < rows; r++) ModulesGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int i = 0; i < Nav.Modules.Length; i++)
            ModulesGrid.Add(MakeTile(Nav.Modules[i]), i % cols, i / cols);
    }

    static View MakeTile(Nav.ModuleDef m)
    {
        var tile = new Border
        {
            Style = Ui.Style("Card"),
            Padding = new Thickness(4, 10),
            HeightRequest = 94,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = m.Emoji, FontSize = 28, HorizontalOptions = LayoutOptions.Center },
                    new Label
                    {
                        Text = m.Title, FontSize = 12.5, TextColor = AppColors.Text,
                        HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center,
                        MaxLines = 2, LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) => await Nav.Open(m.Key);
        tile.GestureRecognizers.Add(tap);
        return tile;
    }

    // ───────── Veriler ─────────
    static string Greeting(int h) => h switch
    {
        >= 5 and < 12 => "Günaydın",
        >= 12 and < 18 => "İyi günler",
        >= 18 and < 23 => "İyi akşamlar",
        _ => "İyi geceler"
    };

    async Task LoadAsync()
    {
        var now = DateTime.Now;
        MenuIcon.Source = Theme.IsDark ? "icon_menu_light.png" : "icon_menu.png";
        BellIcon.Source = Theme.IsDark ? "icon_bell_light.png" : "icon_bell.png";
        GreetingLabel.Text = $"{Greeting(now.Hour)} {AppSettings.UserName}";
        DateLabel.Text = now.ToString("d MMMM yyyy", Fmt.TR);
        DayLabel.Text = now.ToString("dddd", Fmt.TR);

        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Sağlık
            var hrs = await Db.Between<HealthRecord>(today, tomorrow);
            var water = await Db.Between<WaterLog>(today, tomorrow);
            double score = Stats.HealthScore(hrs, water.Sum(x => x.Ml));
            HealthValue.Text = $"% {score:0}";
            HealthBar.Progress = score / 100.0;

            // Finans (bu ay)
            var ms = new DateTime(today.Year, today.Month, 1);
            var fin = await Db.Between<FinanceRecord>(ms, ms.AddMonths(1));
            decimal inc = fin.Where(f => f.IsIncome).Sum(f => f.Amount);
            decimal exp = fin.Where(f => !f.IsIncome).Sum(f => f.Amount);
            decimal net = inc - exp;
            FinanceValue.Text = Fmt.Money(net, 0);
            FinanceValue.TextColor = net < 0 ? AppColors.Red : AppColors.Primary;
            FinanceBar.Progress = inc > 0 ? Math.Clamp((double)(net / inc), 0, 1) : 0;

            // Görevler (bugün)
            var tasks = await Db.Between<TaskItem>(today, tomorrow);
            int done = tasks.Count(t => t.Status == 2);
            TaskValue.Text = $"{done} / {tasks.Count}";
            TaskBar.Progress = tasks.Count == 0 ? 0 : (double)done / tasks.Count;

            // Hatırlatıcılar
            var c = await Db.Get();
            _reminders = (await c.Table<Reminder>().ToListAsync())
                .Where(r => r.Active).OrderBy(r => r.Time).ToList();
            BuildReminders();

            string hm = now.ToString("HH:mm");
            int pending = _reminders.Count(r => !r.DoneToday && string.CompareOrdinal(r.Time, hm) <= 0);
            Badge.IsVisible = pending > 0;
            BadgeLabel.Text = pending.ToString();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Hata", ex.Message, "Tamam");
        }
    }

    void BuildReminders()
    {
        if (RemindersList == null) return;
        RemindersList.Children.Clear();
        NoReminders.IsVisible = _reminders.Count == 0;

        foreach (var r in _reminders.Take(_wide == true ? 12 : 6))
        {
            var row = new Grid { ColumnSpacing = 8 };
            Ui.Cols(row, 30, 50, 30, -1);

            var check = new Border
            {
                WidthRequest = 24, HeightRequest = 24,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                StrokeThickness = r.DoneToday ? 0 : 2,
                Stroke = new SolidColorBrush(AppColors.Primary),
                BackgroundColor = r.DoneToday ? AppColors.Green : AppColors.Card,
                VerticalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = r.DoneToday ? "✓" : "", TextColor = Colors.White, FontSize = 13, FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
                }
            };
            var tap = new TapGestureRecognizer();
            var item = r;
            tap.Tapped += async (s, e) =>
            {
                item.LastDone = item.DoneToday ? null : DateTime.Now;
                await Db.Save(item);
                await LoadAsync();
            };
            check.GestureRecognizers.Add(tap);

            row.Add(check, 0, 0);
            row.Add(new Label { Text = r.Time, FontSize = 14, VerticalOptions = LayoutOptions.Center, TextColor = AppColors.Text }, 1, 0);
            row.Add(new Label { Text = r.Icon, FontSize = 17, VerticalOptions = LayoutOptions.Center }, 2, 0);
            row.Add(new Label
            {
                Text = r.Title, FontSize = 14, VerticalOptions = LayoutOptions.Center,
                TextColor = r.DoneToday ? AppColors.Muted : AppColors.Text,
                TextDecorations = r.DoneToday ? TextDecorations.Strikethrough : TextDecorations.None
            }, 3, 0);

            RemindersList.Children.Add(row);
        }
    }

    // ───────── Olaylar ─────────
    async void OnHealthCard(object sender, TappedEventArgs e) => await Nav.Open("health");
    async void OnFinanceCard(object sender, TappedEventArgs e) => await Nav.Open("finance");
    async void OnTaskCard(object sender, TappedEventArgs e) => await Nav.Open("tasks");
    async void OnAllReminders(object sender, TappedEventArgs e) => await Nav.Open("reminders");

    async void OnMenuTapped(object sender, TappedEventArgs e)
    {
        var choice = await DisplayActionSheetAsync("TROÇKİ VİTANEX", "Kapat", null,
            "Hatırlatıcılar", "Beslenme", "Eğitim", "Kişisel Gelişim", "Sosyal Yaşam", "İş / Kariyer",
            "Yedekleme", "Ayarlar", Theme.IsDark ? "☀️ Açık tema" : "🌙 Koyu tema", "Hakkında");
        switch (choice)
        {
            case "Hatırlatıcılar": await Nav.Open("reminders"); break;
            case "Beslenme": await Nav.Open("nutrition"); break;
            case "Eğitim": await Nav.Open("education"); break;
            case "Kişisel Gelişim": await Nav.Open("development"); break;
            case "Sosyal Yaşam": await Nav.Open("social"); break;
            case "İş / Kariyer": await Nav.Open("career"); break;
            case "Yedekleme": await Nav.Open("backup"); break;
            case "Ayarlar": await Nav.Open("settings"); break;
            case "🌙 Koyu tema": Theme.Set("dark"); await LoadAsync(); break;
            case "☀️ Açık tema": Theme.Set("light"); await LoadAsync(); break;
            case "Hakkında":
                await DisplayAlertAsync("TROÇKİ VİTANEX", $"Kişisel Yaşam Yönetim Sistemi\nSürüm {AppInfo.Current.VersionString}\n\nHayatını Sen Yönet — Daha sağlıklı, daha dengeli, daha üretken bir yaşam.\n\nGeliştirici: TROÇKİ\nDüşün • Keşfet • Üret • Paylaş", "Tamam");
                break;
        }
    }

    async void OnBellTapped(object sender, TappedEventArgs e)
    {
        string hm = DateTime.Now.ToString("HH:mm");
        var pending = _reminders.Where(r => !r.DoneToday && string.CompareOrdinal(r.Time, hm) <= 0).ToList();
        var upcoming = _reminders.Where(r => !r.DoneToday && string.CompareOrdinal(r.Time, hm) > 0).ToList();
        var text = pending.Count == 0 ? "Zamanı gelmiş bekleyen hatırlatıcı yok. 👍"
            : "Bekleyenler:\n" + string.Join("\n", pending.Select(r => $"{r.Time}  {r.Icon} {r.Title}"));
        if (upcoming.Count > 0)
            text += "\n\nSıradakiler:\n" + string.Join("\n", upcoming.Select(r => $"{r.Time}  {r.Icon} {r.Title}"));
        await DisplayAlertAsync("Bildirimler", text, "Tamam");
    }

    async void OnFabClicked(object sender, EventArgs e)
    {
        var choice = await DisplayActionSheetAsync("Hızlı Ekle", "Vazgeç", null,
            "❤️ Sağlık Kaydı", "💧 Su (+200 ml)", "💰 Gelir / Gider", "✅ Görev", "🍽️ Öğün", "⏰ Hatırlatıcı");
        switch (choice)
        {
            case "❤️ Sağlık Kaydı": await Nav.Push(new HealthEntryPage()); break;
            case "💧 Su (+200 ml)":
                await Db.Save(new WaterLog { Date = DateTime.Now, Ml = 200 });
                await LoadAsync();
                break;
            case "💰 Gelir / Gider": await Nav.Push(new FinanceEntryPage()); break;
            case "✅ Görev": await Nav.Push(new TaskEntryPage()); break;
            case "🍽️ Öğün": await Nav.Push(new MealEntryPage()); break;
            case "⏰ Hatırlatıcı": await Nav.Push(new ReminderEntryPage()); break;
        }
    }
}
