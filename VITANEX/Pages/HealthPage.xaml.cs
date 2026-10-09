using VITANEX.Charts;
using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class HealthPage : ContentPage
{
    readonly LineChartDrawable _weight = new() { LineColor = AppColors.Primary, EmptyText = "Kilo kaydı yok" };
    readonly LineChartDrawable _bp = new() { LineColor = AppColors.Red, EmptyText = "Tansiyon kaydı yok" };

    readonly TwoColumn _layout;

    public HealthPage()
    {
        InitializeComponent();
        _layout = new TwoColumn(Host, ColA, ColB);
        WeightChart.Drawable = _weight;
        BpChart.Drawable = _bp;
        SizeChanged += (s, e) => _layout.Apply(Width);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        var c = await Db.Get();
        var today = DateTime.Today;
        var todayRecs = await Db.Between<HealthRecord>(today, today.AddDays(1));
        var water = await Db.Between<WaterLog>(today, today.AddDays(1));
        int ml = water.Sum(w => w.Ml);

        ScoreLabel.Text = $"Skor %{Stats.HealthScore(todayRecs, ml):0}";
        WaterLabel.Text = $"💧 Su: {ml} / {AppSettings.WaterGoal} ml";
        WaterBar.Progress = Math.Min(1, ml / (double)Math.Max(1, AppSettings.WaterGoal));
        var sleep = todayRecs.Where(r => r.SleepHours.HasValue).Select(r => r.SleepHours.Value).DefaultIfEmpty(0).Max();
        var ex = todayRecs.Sum(r => r.ExerciseMin ?? 0);
        SleepExLabel.Text = $"😴 Uyku: {sleep.ToString("0.#", Fmt.TR)} / {AppSettings.SleepGoal.ToString("0.#", Fmt.TR)} sa     🏃 Egzersiz: {ex} / {AppSettings.ExerciseGoal} dk";

        var all = await c.QueryAsync<HealthRecord>("SELECT * FROM HealthRecord ORDER BY Date DESC LIMIT 200");
        BuildVitals(all);

        var last30 = all.Take(30).Reverse().ToList();
        var w = last30.Where(r => r.Weight.HasValue).ToList();
        _weight.Labels = w.Select(r => r.Date.ToString("dd.MM")).ToList();
        _weight.Values = w.Select(r => r.Weight).ToList();
        var bp = last30.Where(r => r.Systolic.HasValue).ToList();
        _bp.Labels = bp.Select(r => r.Date.ToString("dd.MM")).ToList();
        _bp.Values = bp.Select(r => (double?)r.Systolic.Value).ToList();
        WeightChart.Invalidate();
        BpChart.Invalidate();

        var meds = await c.Table<Medication>().ToListAsync();
        BindableLayout.SetItemsSource(MedList, meds);
        NoMeds.IsVisible = meds.Count == 0;

        var list = all.Take(50).ToList();
        BindableLayout.SetItemsSource(RecordList, list);
        NoRecords.IsVisible = list.Count == 0;
    }

    void BuildVitals(List<HealthRecord> all)
    {
        string Last(Func<HealthRecord, bool> has, Func<HealthRecord, string> fmt)
        {
            var r = all.FirstOrDefault(has);
            return r == null ? "—" : fmt(r);
        }

        var items = new (string icon, string title, string value)[]
        {
            ("⚖️", "Kilo", Last(r => r.Weight.HasValue, r => $"{Fmt.Num(r.Weight)} kg")),
            ("🩺", "Tansiyon", Last(r => r.Systolic.HasValue, r => $"{r.Systolic}/{r.Diastolic}")),
            ("💓", "Nabız", Last(r => r.Pulse.HasValue, r => $"{r.Pulse} bpm")),
            ("🫁", "SpO₂", Last(r => r.SpO2.HasValue, r => $"%{r.SpO2}")),
            ("😴", "Uyku", Last(r => r.SleepHours.HasValue, r => $"{Fmt.Num(r.SleepHours)} sa")),
            ("🏃", "Egzersiz", Last(r => r.ExerciseMin.HasValue, r => $"{r.ExerciseMin} dk")),
        };

        VitalsGrid.Children.Clear();
        for (int i = 0; i < items.Length; i++)
        {
            var (icon, title, value) = items[i];
            var box = new Border
            {
                BackgroundColor = AppColors.PrimaryLight,
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(6, 8),
                Content = new VerticalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label { Text = $"{icon} {title}", FontSize = 12, TextColor = AppColors.Muted, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = value, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = AppColors.PrimaryDark, HorizontalOptions = LayoutOptions.Center }
                    }
                }
            };
            VitalsGrid.Add(box, i % 3, i / 3);
        }
    }

    async void OnWater200(object s, EventArgs e) { await Db.Save(new WaterLog { Date = DateTime.Now, Ml = 200 }); await LoadAsync(); }
    async void OnWater500(object s, EventArgs e) { await Db.Save(new WaterLog { Date = DateTime.Now, Ml = 500 }); await LoadAsync(); }

    async void OnWaterUndo(object s, EventArgs e)
    {
        var today = DateTime.Today;
        var last = (await Db.Between<WaterLog>(today, today.AddDays(1))).OrderByDescending(w => w.Date).FirstOrDefault();
        if (last != null) { await Db.Delete(last); await LoadAsync(); }
    }

    async void OnAddRecord(object s, EventArgs e) => await Nav.Push(new HealthEntryPage());

    async void OnRecordTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not HealthRecord r) return;
        var choice = await DisplayActionSheetAsync(r.DateText, "Vazgeç", "Sil", "Düzenle");
        if (choice == "Düzenle") await Nav.Push(new HealthEntryPage(r));
        else if (choice == "Sil" && await Ui.Confirm("Sil", "Bu kayıt silinsin mi?"))
        {
            await Db.Delete(r);
            await LoadAsync();
        }
    }

    // ───────── İlaçlar ─────────
    async void OnAddMedication(object s, EventArgs e) => await EditMedication(new Medication());

    async Task EditMedication(Medication m)
    {
        var name = await DisplayPromptAsync("İlaç", "İlaç adı:", "İleri", "Vazgeç", initialValue: m.Name ?? "");
        if (string.IsNullOrWhiteSpace(name)) return;
        var dose = await DisplayPromptAsync("Doz", "Doz (örn. 1 tablet, 5 mg):", "İleri", "Vazgeç", initialValue: m.Dose ?? "1 tablet");
        if (dose == null) return;
        var times = await DisplayPromptAsync("Kullanım saatleri", "Saatleri virgülle yazın (örn. 08:00, 20:00):", "Kaydet", "Vazgeç", initialValue: m.Times ?? "08:00");
        if (times == null) return;

        bool isNew = m.Id == 0;
        m.Name = name.Trim();
        m.Dose = dose.Trim();
        m.Times = NormalizeTimes(times);
        await Db.Save(m);

        if (isNew && !string.IsNullOrEmpty(m.Times) &&
            await Ui.Confirm("Hatırlatıcı", "Bu ilaç için her gün hatırlatıcı oluşturulsun mu?"))
        {
            foreach (var t in m.Times.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                await Db.Save(new Reminder { Time = t, Icon = "💊", Title = $"{m.Name} ({m.Dose})" });
        }
        await LoadAsync();
    }

    static string NormalizeTimes(string input)
    {
        var parts = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Replace('.', ':'))
            .Select(p => TimeSpan.TryParse(p, out var ts) ? ts.ToString(@"hh\:mm") : null)
            .Where(p => p != null)
            .Distinct()
            .OrderBy(p => p);
        return string.Join(", ", parts);
    }

    async void OnMedicationTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Medication m) return;
        var choice = await DisplayActionSheetAsync(m.Name, "Vazgeç", "Sil", "Düzenle", m.Active ? "Pasif yap" : "Aktif yap");
        switch (choice)
        {
            case "Düzenle": await EditMedication(m); break;
            case "Pasif yap":
            case "Aktif yap":
                m.Active = !m.Active;
                await Db.Save(m);
                await LoadAsync();
                break;
            case "Sil":
                if (await Ui.Confirm("Sil", $"{m.Name} silinsin mi?"))
                {
                    await Db.Delete(m);
                    await LoadAsync();
                }
                break;
        }
    }
}
