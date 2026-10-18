using VITANEX.Charts;
using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class NutritionPage : ContentPage
{
    readonly TwoColumn _layout;
    readonly BarChartDrawable _week = new() { ColorA = AppColors.Green, EmptyText = "Kalori girilmiş öğün yok" };
    DateTime _day = DateTime.Today;

    public NutritionPage()
    {
        InitializeComponent();
        _layout = new TwoColumn(Host, ColA, ColB);
        WeekChart.Drawable = _week;
        SizeChanged += (s, e) => _layout.Apply(Width);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        DayTitle.Text = _day == DateTime.Today ? "Bugün" : _day.ToString("d MMMM dddd", Fmt.TR);

        var meals = (await Db.Between<Meal>(_day, _day.AddDays(1))).OrderBy(m => m.Order).ThenBy(m => m.Date).ToList();
        BuildMenu(meals);
        BindableLayout.SetItemsSource(MealList, meals);
        NoMeals.IsVisible = meals.Count == 0;

        int cal = meals.Sum(m => m.Calories ?? 0);
        CalorieLabel.Text = $"🔥 Kalori: {cal:N0} / {AppSettings.CalorieGoal:N0} kcal";
        CalorieBar.Progress = Math.Min(1, cal / (double)Math.Max(1, AppSettings.CalorieGoal));

        int ml = (await Db.Between<WaterLog>(_day, _day.AddDays(1))).Sum(w => w.Ml);
        WaterLabel.Text = $"💧 Su: {ml} / {AppSettings.WaterGoal} ml";
        WaterBar.Progress = Math.Min(1, ml / (double)Math.Max(1, AppSettings.WaterGoal));

        var start = DateTime.Today.AddDays(-6);
        var week = await Db.Between<Meal>(start, DateTime.Today.AddDays(1));
        var days = Enumerable.Range(0, 7).Select(i => start.AddDays(i)).ToList();
        _week.Labels = days.Select(d => d.ToString("ddd", Fmt.TR)).ToList();
        _week.SeriesA = days.Select(d => (double)week.Where(m => m.Date.Date == d).Sum(m => m.Calories ?? 0)).ToList();
        WeekChart.Invalidate();

        int activeDays = week.Select(m => m.Date.Date).Distinct().Count();
        WeekSummary.Text = activeDays == 0
            ? "Son 7 günde öğün kaydı yok."
            : $"{week.Count} öğün · {activeDays} gün · Günlük ortalama {week.Sum(m => m.Calories ?? 0) / activeDays:N0} kcal";
    }

    // ───────── Günün menüsü ─────────
    void BuildMenu(List<Meal> meals)
    {
        MenuCard.Children.Clear();
        int total = MealPlan.DayTotal(_day);
        var head = new Grid();
        Ui.Cols(head, -1, 0);
        head.Add(new Label { Text = "🥗 Günün Menüsü", Style = Ui.Style("SectionTitle") }, 0, 0);
        head.Add(new Label { Text = $"Gün {MealPlan.DayNo(_day)} / 30", Style = Ui.Style("Muted"), VerticalOptions = LayoutOptions.Center }, 1, 0);
        MenuCard.Children.Add(head);
        MenuCard.Children.Add(new Label { Style = Ui.Style("Muted"), Text = $"Kilo vermeye yönelik · yaklaşık {total:N0} kcal" });

        var notifyRow = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        Ui.Cols(notifyRow, -1, 0);
        notifyRow.Add(new Label
        {
            Text = "🔔 Öğün bildirimleri\n08:00 · 10:30 · 12:30 · 16:00 · 19:00",
            FontSize = 13, TextColor = AppColors.Text, VerticalOptions = LayoutOptions.Center
        }, 0, 0);
        var sw = new Switch { IsToggled = MealPlan.NotifyEnabled, OnColor = AppColors.Green, VerticalOptions = LayoutOptions.Center };
        sw.Toggled += async (s, e) =>
        {
            if (e.Value && !await Payments.RequestPermission())
            {
                await DisplayAlertAsync("Bildirim izni", "Öğün bildirimlerinin gelmesi için bildirim iznine izin verin:\nAyarlar → Uygulamalar → TROÇKİ VİTANEX → Bildirimler.", "Tamam");
            }
            MealPlan.NotifyEnabled = e.Value;
            await Payments.Reschedule();
            if (e.Value) await DisplayAlertAsync("🔔 Açıldı", "Her gün öğün saatlerinde o günün menüsü bildirim olarak gelecek.", "Tamam");
        };
        notifyRow.Add(sw, 1, 0);
        MenuCard.Children.Add(notifyRow);

        foreach (var slot in MealPlan.Slots)
        {
            var it = MealPlan.Item(_day, slot);
            bool eaten = MealPlan.Eaten(meals, slot);

            var title = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            Ui.Cols(title, 0, -1, 0);
            title.ColumnSpacing = 8;
            title.Add(new Label { Text = slot.Icon, FontSize = 20 }, 0, 0);
            title.Add(new Label { Text = slot.Label, FontAttributes = FontAttributes.Bold, TextColor = AppColors.PrimaryDark, VerticalOptions = LayoutOptions.Center }, 1, 0);
            title.Add(new Label { Text = $"{it.Cal} kcal", Style = Ui.Style("Muted"), VerticalOptions = LayoutOptions.Center }, 2, 0);

            var text = new Label
            {
                Text = it.Text, FontSize = 14, LineHeight = 1.2,
                TextColor = eaten ? AppColors.Muted : AppColors.Text,
                TextDecorations = eaten ? TextDecorations.Strikethrough : TextDecorations.None
            };

            var eat = new Button { Text = eaten ? "✓ Yedim" : "✓ Yedim, kaydet", Style = Ui.Style("SecondaryButton") };
            if (eaten) { eat.BackgroundColor = AppColors.Green; eat.TextColor = Colors.White; }
            var s1 = slot;
            eat.Clicked += async (s, e) => { await MealPlan.ToggleEat(_day, s1); await LoadAsync(); };
            var swap = new Button { Text = "🔄 Değiştir", Style = Ui.Style("SecondaryButton") };
            swap.Clicked += async (s, e) => { MealPlan.Swap(_day, s1); if (MealPlan.NotifyEnabled) await Payments.Reschedule(); await LoadAsync(); };

            var btns = new Grid { ColumnSpacing = 8 };
            Ui.Cols(btns, -1, 0);
            btns.Add(eat, 0, 0);
            btns.Add(swap, 1, 0);

            MenuCard.Children.Add(new BoxView { HeightRequest = 1, Color = AppColors.Line, Margin = new Thickness(0, 6, 0, 0) });
            MenuCard.Children.Add(title);
            MenuCard.Children.Add(text);
            MenuCard.Children.Add(btns);
        }

        var plan = new Button { Text = "📅 Aylık Planı Gör", Style = Ui.Style("SecondaryButton"), Margin = new Thickness(0, 8, 0, 0) };
        plan.Clicked += async (s, e) => await Nav.Push(new MenuPlanPage());
        MenuCard.Children.Add(plan);
    }

    async void OnPrevDay(object s, EventArgs e) { _day = _day.AddDays(-1); await LoadAsync(); }
    async void OnNextDay(object s, EventArgs e) { if (_day < DateTime.Today) { _day = _day.AddDays(1); await LoadAsync(); } }

    async void OnWater200(object s, EventArgs e) => await AddWater(200);
    async void OnWater500(object s, EventArgs e) => await AddWater(500);

    async Task AddWater(int ml)
    {
        var when = _day == DateTime.Today ? DateTime.Now : _day.AddHours(12);
        await Db.Save(new WaterLog { Date = when, Ml = ml });
        await LoadAsync();
    }

    async void OnAddMeal(object s, EventArgs e) => await Nav.Push(new MealEntryPage(null, _day));

    async void OnMealTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Meal m) return;
        var choice = await DisplayActionSheetAsync(m.MealType, "Vazgeç", "Sil", "Düzenle");
        if (choice == "Düzenle") await Nav.Push(new MealEntryPage(m));
        else if (choice == "Sil" && await Ui.Confirm("Sil", "Bu öğün silinsin mi?"))
        {
            await Db.Delete(m);
            await LoadAsync();
        }
    }
}
