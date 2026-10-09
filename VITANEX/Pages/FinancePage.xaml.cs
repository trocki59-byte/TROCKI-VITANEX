using VITANEX.Charts;
using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class FinancePage : ContentPage
{
    readonly TwoColumn _layout;
    readonly BarChartDrawable _chart = new() { SeriesB = new List<double>() };
    DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public FinancePage()
    {
        InitializeComponent();
        _layout = new TwoColumn(Host, ColA, ColB);
        MonthChart.Drawable = _chart;
        SizeChanged += (s, e) => _layout.Apply(Width);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        MonthTitle.Text = _month.ToString("MMMM yyyy", Fmt.TR);
        var recs = (await Db.Between<FinanceRecord>(_month, _month.AddMonths(1)))
                   .OrderByDescending(r => r.Date).ToList();

        decimal inc = recs.Where(r => r.IsIncome).Sum(r => r.Amount);
        decimal exp = recs.Where(r => !r.IsIncome).Sum(r => r.Amount);
        IncomeLabel.Text = Fmt.Money(inc);
        ExpenseLabel.Text = Fmt.Money(exp);
        NetLabel.Text = Fmt.Money(inc - exp);
        NetLabel.TextColor = inc - exp < 0 ? AppColors.Red : AppColors.Primary;
        SavingLabel.Text = inc > 0 ? $"Tasarruf oranı: %{(inc - exp) / inc * 100:0}" : "Bu ay gelir kaydı yok";

        BindableLayout.SetItemsSource(RecordList, recs);
        NoRecords.IsVisible = recs.Count == 0;

        // Kategoriler
        CategoryList.Children.Clear();
        var cats = recs.Where(r => !r.IsIncome).GroupBy(r => r.Category ?? "Diğer")
                       .Select(g => new { Name = g.Key, Sum = g.Sum(x => x.Amount) })
                       .OrderByDescending(x => x.Sum).ToList();
        NoCategories.IsVisible = cats.Count == 0;
        foreach (var c in cats)
        {
            var g = new Grid { RowSpacing = 4 };
            Ui.Cols(g, -1, 0);
            g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            g.Add(new Label { Text = c.Name, FontSize = 14 }, 0, 0);
            g.Add(new Label { Text = $"{Fmt.Money(c.Sum)}  (%{(exp > 0 ? c.Sum / exp * 100 : 0):0})", FontSize = 13, FontAttributes = FontAttributes.Bold }, 1, 0);
            var bar = new ProgressBar { Progress = exp > 0 ? (double)(c.Sum / exp) : 0, ProgressColor = AppColors.Red };
            g.Add(bar, 0, 1);
            Grid.SetColumnSpan(bar, 2);
            CategoryList.Children.Add(g);
        }

        // 6 aylık grafik
        var first = _month.AddMonths(-5);
        var six = await Db.Between<FinanceRecord>(first, _month.AddMonths(1));
        var months = Enumerable.Range(0, 6).Select(i => first.AddMonths(i)).ToList();
        _chart.Labels = months.Select(m => m.ToString("MMM", Fmt.TR)).ToList();
        _chart.SeriesA = months.Select(m => (double)six.Where(r => r.IsIncome && r.Date >= m && r.Date < m.AddMonths(1)).Sum(r => r.Amount)).ToList();
        _chart.SeriesB = months.Select(m => (double)six.Where(r => !r.IsIncome && r.Date >= m && r.Date < m.AddMonths(1)).Sum(r => r.Amount)).ToList();
        MonthChart.Invalidate();
    }

    async void OnPrevMonth(object s, EventArgs e) { _month = _month.AddMonths(-1); await LoadAsync(); }
    async void OnNextMonth(object s, EventArgs e) { _month = _month.AddMonths(1); await LoadAsync(); }
    async void OnAdd(object s, EventArgs e) => await Nav.Push(new FinanceEntryPage());

    async void OnRecordTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not FinanceRecord r) return;
        var choice = await DisplayActionSheetAsync(r.Description, "Vazgeç", "Sil", "Düzenle");
        if (choice == "Düzenle") await Nav.Push(new FinanceEntryPage(r));
        else if (choice == "Sil" && await Ui.Confirm("Sil", "Bu kayıt silinsin mi?"))
        {
            await Db.Delete(r);
            await LoadAsync();
        }
    }
}
