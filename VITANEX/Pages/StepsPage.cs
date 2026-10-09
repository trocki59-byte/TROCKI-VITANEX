using VITANEX.Charts;
using VITANEX.Services;

namespace VITANEX.Pages;

/// <summary>Yürüme Sayar: bugünkü adım, hedef halkası, mesafe, kalori ve son 7 gün.</summary>
public class StepsPage : ContentPage
{
    readonly Label _now, _goal, _km, _kcal, _status, _week, _avg, _streak, _best;
    readonly DonutDrawable _ring = new() { RingColor = AppColors.Accent };
    readonly GraphicsView _ringView;
    readonly BarChartDrawable _chart = new() { ColorA = AppColors.Accent, EmptyText = "Henüz adım kaydı yok" };
    readonly GraphicsView _chartView;
    readonly Button _sensorBtn;

    public StepsPage()
    {
        Title = "Yürüme Sayar";

        _now = new Label { FontSize = 46, FontAttributes = FontAttributes.Bold, TextColor = AppColors.PrimaryDark, HorizontalOptions = LayoutOptions.Center };
        _goal = new Label { Style = Ui.Style("Muted"), HorizontalOptions = LayoutOptions.Center };
        _ringView = new GraphicsView { Drawable = _ring, HeightRequest = 170, WidthRequest = 170, HorizontalOptions = LayoutOptions.Center };
        _km = Big();
        _kcal = Big();
        _status = new Label { FontSize = 13, TextColor = AppColors.Muted, HorizontalTextAlignment = TextAlignment.Center, LineHeight = 1.25 };
        _sensorBtn = new Button();
        _sensorBtn.Clicked += OnSensor;

        var manual = new Button { Text = "✏️ Adım Sayısını Gir", Style = Ui.Style("SecondaryButton") };
        manual.Clicked += OnManual;

        var tiles = new Grid { ColumnSpacing = 10, Margin = new Thickness(0, 6, 0, 0) };
        Ui.Cols(tiles, -1, -1);
        tiles.Add(Tile("Mesafe", _km), 0, 0);
        tiles.Add(Tile("Yakılan", _kcal), 1, 0);

        var today = new Border
        {
            Style = Ui.Style("Card"),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "BUGÜN", FontSize = 12, FontAttributes = FontAttributes.Bold, CharacterSpacing = 3, TextColor = AppColors.Muted, HorizontalOptions = LayoutOptions.Center },
                    _now,
                    _goal,
                    _ringView,
                    tiles,
                    _sensorBtn,
                    _status,
                    manual
                }
            }
        };

        _chartView = new GraphicsView { Drawable = _chart, HeightRequest = 190 };
        _week = Small(); _avg = Small(); _streak = Small(); _best = Small();
        var stats = new Grid { ColumnSpacing = 10, RowSpacing = 10 };
        Ui.Cols(stats, -1, -1);
        stats.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        stats.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        stats.Add(Stat("Haftalık toplam", _week), 0, 0);
        stats.Add(Stat("Günlük ortalama", _avg), 1, 0);
        stats.Add(Stat("🔥 Hedef serisi", _streak), 0, 1);
        stats.Add(Stat("🏆 En iyi gün", _best), 1, 1);

        var history = new Border
        {
            Style = Ui.Style("Card"),
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children = { new Label { Text = "📊 Son 7 Gün", Style = Ui.Style("SectionTitle") }, _chartView, stats }
            }
        };

        var goalBtn = new Button { Text = "🎯 Günlük hedef", Style = Ui.Style("SecondaryButton") };
        goalBtn.Clicked += OnGoal;
        var strideBtn = new Button { Text = "📏 Adım uzunluğu", Style = Ui.Style("SecondaryButton") };
        strideBtn.Clicked += OnStride;
        var settingsRow = new Grid { ColumnSpacing = 10 };
        Ui.Cols(settingsRow, -1, -1);
        settingsRow.Add(goalBtn, 0, 0);
        settingsRow.Add(strideBtn, 1, 0);

        var info = new Label
        {
            Style = Ui.Style("Muted"), LineHeight = 1.3,
            Text = "ℹ️ VİTANEX telefonunuzun adım sensörünü kullanır. Sensör uygulama kapalıyken de saymaya devam eder; uygulamayı açtığınızda aradaki adımlar bugüne eklenir. Bu yüzden günün sonunda VİTANEX'i bir kez açmanız, adımların doğru güne yazılmasını sağlar. Sensörü olmayan cihazlarda adım sayısını elle girebilirsiniz."
        };

        var col1 = new VerticalStackLayout { Spacing = 12, Children = { today } };
        var col2 = new VerticalStackLayout { Spacing = 12, Children = { history, settingsRow, info } };
        var host = new Grid { ColumnSpacing = 16, Padding = new Thickness(16, 16, 16, 40), MaximumWidthRequest = 1100 };
        host.Add(col1, 0, 0);
        host.Add(col2, 0, 1);
        host.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        host.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        host.RowSpacing = 12;

        // Tablette iki sütun
        SizeChanged += (s, e) =>
        {
            bool wide = Width >= 760;
            host.ColumnDefinitions.Clear();
            host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            if (wide) host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetRow(col2, wide ? 0 : 1);
            Grid.SetColumn(col2, wide ? 1 : 0);
        };

        Content = new ScrollView { Content = host };
    }

    static Label Big() => new() { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = AppColors.Text, HorizontalOptions = LayoutOptions.Center };
    static Label Small() => new() { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = AppColors.Text, HorizontalOptions = LayoutOptions.Center };

    static View Tile(string title, Label value) => new Border
    {
        StrokeThickness = 0, BackgroundColor = AppColors.PrimaryLight, Padding = new Thickness(8, 10),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
        Content = new VerticalStackLayout
        {
            Children = { new Label { Text = title, Style = Ui.Style("Muted"), HorizontalOptions = LayoutOptions.Center }, value }
        }
    };

    static View Stat(string title, Label value) => new VerticalStackLayout
    {
        Children = { new Label { Text = title, Style = Ui.Style("Muted"), HorizontalOptions = LayoutOptions.Center }, value }
    };

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StepCounter.Changed += OnChanged;
        await StepCounter.Resume();
        await LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StepCounter.Changed -= OnChanged;
    }

    async void OnChanged() => await LoadAsync();

    async Task LoadAsync()
    {
        try
        {
            int goal = AppSettings.StepGoal;
            int n = await StepCounter.Today();
            _now.Text = n.ToString("N0", Fmt.TR);
            _goal.Text = $"adım  ·  hedef {goal.ToString("N0", Fmt.TR)}";
            _ring.Percent = goal > 0 ? Math.Min(100, n * 100.0 / goal) : 0;
            _ring.RingColor = n >= goal ? AppColors.Green : AppColors.Accent;
            _ringView.Invalidate();
            _km.Text = StepCounter.Km(n).ToString("0.00", Fmt.TR) + " km";
            _kcal.Text = (await StepCounter.Kcal(n)) + " kcal";

            // Sensör durumu
            if (!StepCounter.Supported)
            {
                _sensorBtn.IsVisible = false;
                _status.Text = "Bu cihazda adım sensörü bulunamadı. Adımlarınızı aşağıdan elle girebilirsiniz.";
            }
            else if (StepCounter.Enabled && StepCounter.Running)
            {
                _sensorBtn.IsVisible = true;
                _sensorBtn.Text = "⏹ Otomatik Sayımı Kapat";
                _sensorBtn.BackgroundColor = AppColors.Red;
                _status.Text = "👣 Otomatik sayım açık. Telefonunuzu yanınızda taşımanız yeterli.";
            }
            else
            {
                _sensorBtn.IsVisible = true;
                _sensorBtn.Text = "▶ Otomatik Sayımı Başlat";
                _sensorBtn.BackgroundColor = AppColors.Primary;
                _status.Text = StepCounter.Enabled
                    ? "İzin verilmediği için sayım durdu. Başlat'a dokunup \"Fiziksel etkinlik\" iznine izin verin."
                    : "Başlattığınızda telefonun adım sensörü kullanılır (\"Fiziksel etkinlik\" izni istenir).";
            }

            // Son 7 gün
            var days = await StepCounter.LastDays(7);
            _chart.Labels = days.Select(d => d.Day.ToString("ddd", Fmt.TR)).ToList();
            _chart.SeriesA = days.Select(d => (double)d.Steps).ToList();
            _chartView.Invalidate();

            int week = days.Sum(d => d.Steps), active = days.Count(d => d.Steps > 0);
            _week.Text = week.ToString("N0", Fmt.TR);
            _avg.Text = active > 0 ? (week / active).ToString("N0", Fmt.TR) : "—";

            var last60 = await StepCounter.LastDays(60);
            int streak = 0;
            int idx = last60.Count - 1;
            if (last60[idx].Steps < goal) idx--;        // bugün henüz tamamlanmadıysa dünden say
            for (; idx >= 0 && last60[idx].Steps >= goal; idx--) streak++;
            _streak.Text = $"{streak} gün";

            var c = await Db.Get();
            var best = await c.Table<Models.StepDay>().OrderByDescending(x => x.Steps).FirstOrDefaultAsync();
            _best.Text = (best?.Steps ?? 0).ToString("N0", Fmt.TR);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Hata", ex.Message, "Tamam");
        }
    }

    async void OnSensor(object sender, EventArgs e)
    {
        if (StepCounter.Enabled && StepCounter.Running)
        {
            if (await Ui.Confirm("Otomatik sayım", "Adım sensörü kapatılsın mı? Kayıtlı adımlarınız silinmez.", "Kapat"))
                StepCounter.Disable();
        }
        else if (!await StepCounter.Enable())
        {
            await DisplayAlertAsync("İzin gerekli",
                "Adım saymak için \"Fiziksel etkinlik\" iznine izin verin.\n\nDaha önce reddettiyseniz: Ayarlar → Uygulamalar → TROÇKİ VİTANEX → İzinler → Fiziksel etkinlik → İzin ver.",
                "Tamam");
        }
        else
        {
            await DisplayAlertAsync("Başladı 👣", "Otomatik adım sayımı açıldı. Bundan sonraki adımlarınız sayılacak.", "Tamam");
        }
        await LoadAsync();
    }

    async void OnManual(object sender, EventArgs e)
    {
        var day = await DisplayActionSheetAsync("Hangi gün?", "Vazgeç", null, "Bugün", "Dün", "2 gün önce");
        DateTime d = day switch
        {
            "Bugün" => DateTime.Today,
            "Dün" => DateTime.Today.AddDays(-1),
            "2 gün önce" => DateTime.Today.AddDays(-2),
            _ => DateTime.MinValue
        };
        if (d == DateTime.MinValue) return;

        var cur = (await StepCounter.Day(d)).Steps;
        var txt = await DisplayPromptAsync("Adım Sayısı", $"{d.ToString("d MMMM dddd", Fmt.TR)} günü toplam kaç adım attınız?\n(Telefonun Sağlık / Fit uygulamasından bakabilirsiniz.)",
            "Kaydet", "Vazgeç", "örn. 7500", 6, Keyboard.Numeric, cur > 0 ? cur.ToString() : "");
        var v = Fmt.ParseInt(txt);
        if (!v.HasValue) return;
        if (v < 0 || v > 100000) { await DisplayAlertAsync("Geçersiz", "0 ile 100.000 arasında bir sayı girin.", "Tamam"); return; }
        await StepCounter.SetDay(d, v.Value);
        await LoadAsync();
    }

    async void OnGoal(object sender, EventArgs e)
    {
        var txt = await DisplayPromptAsync("Günlük hedef", "Günde kaç adım hedefliyorsunuz?", "Kaydet", "Vazgeç",
            "8000", 6, Keyboard.Numeric, AppSettings.StepGoal.ToString());
        var v = Fmt.ParseInt(txt);
        if (!v.HasValue) return;
        AppSettings.StepGoal = Math.Clamp(v.Value, 500, 100000);
        await LoadAsync();
    }

    async void OnStride(object sender, EventArgs e)
    {
        var txt = await DisplayPromptAsync("Adım uzunluğu", "Ortalama adım uzunluğunuz kaç cm? (genelde 65–80)", "Kaydet", "Vazgeç",
            "72", 3, Keyboard.Numeric, AppSettings.StrideCm.ToString());
        var v = Fmt.ParseInt(txt);
        if (!v.HasValue) return;
        AppSettings.StrideCm = Math.Clamp(v.Value, 40, 120);
        await LoadAsync();
    }
}
