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
