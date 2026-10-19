using VITANEX.Charts;
using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class HealthPage : ContentPage
{
    readonly LineChartDrawable _weight = new() { LineColor = AppColors.Primary, EmptyText = "Kilo kaydı yok" };
    readonly LineChartDrawable _bp = new() { LineColor = AppColors.Red, EmptyText = "Tansiyon kaydı yok" };
    readonly LineChartDrawable _spo = new() { LineColor = Color.FromArgb("#12B5A6"), EmptyText = "SpO₂ ölçümü yok" };

    readonly TwoColumn _layout;

    public HealthPage()
    {
        InitializeComponent();
        _layout = new TwoColumn(Host, ColA, ColB);
        WeightChart.Drawable = _weight;
        BpChart.Drawable = _bp;
        SpoChart.Drawable = _spo;
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

        BuildSpo2(all);
        await BuildAppointments();

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

    // ───────── SpO₂ (ayrı ölçüm) ─────────
    static (string Text, Color Color) SpoLevel(int v) =>
        v >= 95 ? ("Normal", AppColors.Green) : v >= 90 ? ("Düşük — takip edin", AppColors.Orange) : ("Çok düşük — doktora danışın", AppColors.Red);

    void BuildSpo2(List<HealthRecord> all)
    {
        var s = all.Where(r => r.SpO2.HasValue).Take(30).Reverse().ToList();
        var last = s.LastOrDefault();
        if (last == null)
        {
            SpoValue.Text = "—";
            SpoState.Text = "";
            SpoInfo.Text = "Henüz SpO₂ ölçümü yok. Parmak oksimetresiyle ölçüp girin.";
        }
        else
        {
            var (t, col) = SpoLevel(last.SpO2.Value);
            SpoValue.Text = $"%{last.SpO2}";
            SpoValue.TextColor = col;
            SpoState.Text = t;
            SpoState.TextColor = col;
            SpoInfo.Text = $"Son ölçüm: {last.DateText}" + (last.Pulse.HasValue ? $" · Nabız {last.Pulse}" : "");
        }
        _spo.Labels = s.Select(r => r.Date.ToString("dd.MM")).ToList();
        _spo.Values = s.Select(r => (double?)r.SpO2.Value).ToList();
        SpoChart.IsVisible = s.Count > 1;
        SpoChart.Invalidate();
    }

    async void OnAddSpo2(object sender, EventArgs e)
    {
        var v = await DisplayPromptAsync("🫁 SpO₂ Ölçümü", "Oksimetrede görünen SpO₂ değeri (%):", "İleri", "Vazgeç",
            placeholder: "98", maxLength: 3, keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(v)) return;
        var spo = Fmt.ParseInt(v);
        if (spo == null || spo < 50 || spo > 100)
        {
            await DisplayAlertAsync("Hatalı değer", "SpO₂ 50 ile 100 arasında olmalı.", "Tamam");
            return;
        }
        var p = await DisplayPromptAsync("💓 Nabız", "Oksimetrede nabız da görünüyorsa yazın (boş bırakabilirsiniz):", "Kaydet", "Vazgeç",
            placeholder: "72", maxLength: 3, keyboard: Keyboard.Numeric);
        if (p == null) return;
        var pulse = Fmt.ParseInt(p);
        await Db.Save(new HealthRecord { Date = DateTime.Now, SpO2 = spo, Pulse = pulse is > 20 and < 250 ? pulse : null });
        if (spo < 90)
            await DisplayAlertAsync("⚠️ Dikkat", "SpO₂ %90'ın altında. Nefes darlığı, göğüs ağrısı veya morarma varsa hemen doktora başvurun (112).", "Tamam");
        await LoadAsync();
    }

    // ───────── Doktor randevuları ─────────
    async Task BuildAppointments()
    {
        var all = await Appointments.All();
        var up = all.Where(a => !a.Done && a.Date >= DateTime.Today.AddDays(-1)).OrderBy(a => a.Date).ToList();
        var past = all.Except(up).OrderByDescending(a => a.Date).Take(3).ToList();

        ApptList.Children.Clear();
        if (up.Count == 0)
            ApptList.Children.Add(new Label { Text = "Yaklaşan randevu yok.", Style = Ui.Style("Muted") });
        foreach (var a in up) ApptList.Children.Add(ApptRow(a));
        if (past.Count > 0)
        {
            ApptList.Children.Add(new Label { Text = "Geçmiş", Style = Ui.Style("Muted"), FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 8, 0, 0) });
            foreach (var a in past) ApptList.Children.Add(ApptRow(a));
        }
    }

    View ApptRow(DoctorAppointment a)
    {
        var g = new Grid { ColumnSpacing = 8, Padding = new Thickness(0, 6) };
        Ui.Cols(g, -1, 0);
        var info = new VerticalStackLayout { Spacing = 1 };
        info.Children.Add(new Label { Text = "🩺 " + Appointments.Title(a), FontAttributes = FontAttributes.Bold, FontSize = 15, TextColor = AppColors.Text });
        info.Children.Add(new Label
        {
            Text = a.Date.ToString("d MMMM yyyy dddd · HH:mm", Fmt.TR) + (string.IsNullOrWhiteSpace(a.Place) ? "" : " · 📍 " + a.Place),
            Style = Ui.Style("Muted")
        });
        if (!string.IsNullOrWhiteSpace(a.Note))
            info.Children.Add(new Label { Text = "📝 " + a.Note, Style = Ui.Style("Muted") });
        g.Add(info, 0, 0);
        g.Add(new Label { Text = Appointments.WhenText(a), TextColor = Appointments.WhenColor(a), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }, 1, 0);
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) => await OnAppointmentTapped(a);
        g.GestureRecognizers.Add(tap);
        return g;
    }

    async void OnAddAppointment(object sender, EventArgs e) => await Nav.Push(new AppointmentEntryPage());

    async Task OnAppointmentTapped(DoctorAppointment a)
    {
        var choice = await DisplayActionSheetAsync("🩺 " + Appointments.Title(a), "Vazgeç", "Sil", "Düzenle", a.Done ? "Gidilmedi olarak işaretle" : "✓ Gidildi");
        switch (choice)
        {
            case "Düzenle": await Nav.Push(new AppointmentEntryPage(a)); break;
            case "✓ Gidildi":
            case "Gidilmedi olarak işaretle":
                a.Done = !a.Done;
                await Appointments.Save(a);
                await LoadAsync();
                break;
            case "Sil":
                if (await Ui.Confirm("Sil", "Bu randevu silinsin mi?"))
                {
                    await Appointments.Delete(a);
                    await LoadAsync();
                }
                break;
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
            await Ui.Confirm("Hatırlatıcı", "Bu ilaç için her gün hatırlatıcı oluşturulsun mu? (Ana sayfada listelenir.)"))
        {
            foreach (var t in m.Times.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                await Db.Save(new Reminder { Time = t, Icon = "💊", Title = $"{m.Name} ({m.Dose})" });
        }

        // Telefon bildirimi: ilaç saatlerinde "💊 İlaç zamanı"
        if (isNew && !await Payments.RequestPermission())
            await DisplayAlertAsync("Bildirim izni", "İlaç saatlerinde bildirim gelmesi için bildirim iznine izin verin:\nAyarlar → Uygulamalar → TROÇKİ VİTANEX → Bildirimler.", "Tamam");
        await Payments.Reschedule();
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
                await Payments.Reschedule();
                await LoadAsync();
                break;
            case "Sil":
                if (await Ui.Confirm("Sil", $"{m.Name} silinsin mi?"))
                {
                    await Db.Delete(m);
                    await Payments.Reschedule();
                    await LoadAsync();
                }
                break;
        }
    }
}
