using VITANEX.Charts;
using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class StatisticsPage : ContentPage
{
    readonly TwoColumn _layout;
    Period _period = Period.Monthly;

    readonly DonutDrawable _dHealth = new() { RingColor = AppColors.Green };
    readonly DonutDrawable _dFinance = new() { RingColor = AppColors.Accent };
    readonly DonutDrawable _dTasks = new() { RingColor = AppColors.Orange };
    readonly BarChartDrawable _finance = new() { SeriesB = new List<double>() };
    readonly LineChartDrawable _health = new() { LineColor = AppColors.Green, ZeroBased = true, EmptyText = "Sağlık verisi yok" };
    readonly LineChartDrawable _weight = new() { LineColor = AppColors.Primary, EmptyText = "Kilo kaydı yok" };
    readonly BarChartDrawable _tasks = new() { ColorA = AppColors.Orange, EmptyText = "Tamamlanan görev yok" };
    readonly BarChartDrawable _water = new() { ColorA = AppColors.Accent, EmptyText = "Su kaydı yok" };

    public StatisticsPage()
    {
        InitializeComponent();
        _layout = new TwoColumn(Host, ColA, ColB);
        DonutHealth.Drawable = _dHealth;
        DonutFinance.Drawable = _dFinance;
        DonutTasks.Drawable = _dTasks;
        FinanceChart.Drawable = _finance;
        HealthChart.Drawable = _health;
        WeightChart.Drawable = _weight;
        TaskChart.Drawable = _tasks;
        WaterChart.Drawable = _water;
        SizeChanged += (s, e) => _layout.Apply(Width);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async void OnPeriod(object sender, EventArgs e)
    {
        _period = Enum.Parse<Period>(((Button)sender).ClassId);
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        foreach (var b in PeriodBar.Children.OfType<Button>())
            Ui.SetChip(b, b.ClassId == _period.ToString());

        var buckets = Stats.Buckets(_period);
        var start = buckets[0].Start;
        var end = buckets[^1].End;
        PeriodLabel.Text = $"{Stats.PeriodName(_period)}  ({start:dd.MM.yyyy} – {end.AddDays(-1):dd.MM.yyyy})";

        var hrs = await Db.Between<HealthRecord>(start, end);
        var water = await Db.Between<WaterLog>(start, end);
        var fin = await Db.Between<FinanceRecord>(start, end);
        var tasks = await Db.Between<TaskItem>(start, end);

        // Genel ilerleme
        double hScore = Stats.PeriodHealthScore(hrs, water, start, end);
        decimal inc = fin.Where(f => f.IsIncome).Sum(f => f.Amount);
        decimal exp = fin.Where(f => !f.IsIncome).Sum(f => f.Amount);
        double fScore = inc > 0 ? Math.Clamp((double)((inc - exp) / inc) * 100, 0, 100) : 0;
        int done = tasks.Count(t => t.Status == 2);
        double tScore = tasks.Count > 0 ? done * 100.0 / tasks.Count : 0;

        _dHealth.Percent = hScore;
        _dFinance.Percent = fScore;
        _dTasks.Percent = tScore;

        SummaryLabel.Text =
            $"Sağlık: veri girilen günlerin ortalama skoru.\n" +
            $"Finans: tasarruf oranı — gelir {Fmt.Money(inc, 0)}, gider {Fmt.Money(exp, 0)}, net {Fmt.Money(inc - exp, 0)}.\n" +
            $"Görevler: {tasks.Count} görevin {done} tanesi tamamlandı.";

        var labels = buckets.Select(b => b.Label).ToList();

        _finance.Labels = labels;
        _finance.SeriesA = buckets.Select(b => (double)fin.Where(f => f.IsIncome && f.Date >= b.Start && f.Date < b.End).Sum(f => f.Amount)).ToList();
        _finance.SeriesB = buckets.Select(b => (double)fin.Where(f => !f.IsIncome && f.Date >= b.Start && f.Date < b.End).Sum(f => f.Amount)).ToList();

        _health.Labels = labels;
        _health.Values = buckets.Select(b =>
        {
            bool any = hrs.Any(r => r.Date >= b.Start && r.Date < b.End) || water.Any(w => w.Date >= b.Start && w.Date < b.End);
            return any ? (double?)Stats.PeriodHealthScore(hrs, water, b.Start, b.End) : null;
        }).ToList();

        _weight.Labels = labels;
        _weight.Values = buckets.Select(b =>
        {
            var w = hrs.Where(r => r.Weight.HasValue && r.Date >= b.Start && r.Date < b.End).Select(r => r.Weight.Value).ToList();
            return w.Count > 0 ? (double?)Math.Round(w.Average(), 1) : null;
        }).ToList();

        _tasks.Labels = labels;
        _tasks.SeriesA = buckets.Select(b => (double)tasks.Count(t => t.Status == 2 && t.Date >= b.Start && t.Date < b.End)).ToList();

        _water.Labels = labels;
        _water.SeriesA = buckets.Select(b => water.Where(w => w.Date >= b.Start && w.Date < b.End).Sum(w => w.Ml) / 1000.0).ToList();

        foreach (var g in new[] { DonutHealth, DonutFinance, DonutTasks, FinanceChart, HealthChart, WeightChart, TaskChart, WaterChart })
            g.Invalidate();
    }
}
