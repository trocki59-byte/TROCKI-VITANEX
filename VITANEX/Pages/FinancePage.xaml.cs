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
        await BuildPayments();
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

    // ───────── Düzenli ödemeler ─────────
    async Task BuildPayments()
    {
        var list = await Payments.Active();
        PaysTotal.Text = list.Count == 0
            ? "Kredi taksiti, kredi kartı, KMH, kira, aidat gibi her ay ödediklerinizi ekleyin; 1 gün önce ve ödeme günü bildirim gelir."
            : $"Aylık toplam: {Fmt.Money(list.Sum(p => p.Amount))}  ·  {list.Count} ödeme";
        PaysList.Children.Clear();
        bool line = false;
        foreach (var p in list)
        {
            int n = Payments.DaysLeft(p);
            var due = Payments.NextDue(p);
            var g = new Grid { ColumnSpacing = 10, Padding = new Thickness(0, 10) };
            Ui.Cols(g, 34, -1, 0);
            g.Add(new Label { Text = Payments.Icon(p.Type), FontSize = 24, VerticalOptions = LayoutOptions.Center }, 0, 0);
            g.Add(new VerticalStackLayout
            {
                Spacing = 1,
                Children =
                {
                    new Label { Text = p.Name, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = AppColors.Text },
                    new Label { Text = $"{p.Type} · ödeme günü: {p.Day}" + (p.Left.HasValue ? $" · kalan {p.Left} taksit" : ""), FontSize = 12, TextColor = AppColors.Muted },
                    new Label
                    {
                        FontSize = 13,
                        FormattedText = new FormattedString
                        {
                            Spans =
                            {
                                new Span { Text = due.ToString("d MMMM", Fmt.TR) + " · ", TextColor = AppColors.Text },
                                new Span { Text = Payments.WhenText(n), TextColor = Payments.WhenColor(n), FontAttributes = n <= 1 ? FontAttributes.Bold : FontAttributes.None }
                            }
                        }
                    }
                }
            }, 1, 0);
            g.Add(new Label { Text = Fmt.Money(p.Amount, 0), FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = AppColors.Red, VerticalOptions = LayoutOptions.Center }, 2, 0);

            var tap = new TapGestureRecognizer();
            var item = p;
            tap.Tapped += async (s, e) => await OnPaymentTapped(item);
            g.GestureRecognizers.Add(tap);

            if (line) PaysList.Children.Add(new BoxView { HeightRequest = 1, Color = AppColors.Line });
            PaysList.Children.Add(g);
            line = true;
        }
    }

    async void OnAddPayment(object s, EventArgs e) => await Nav.Push(new PaymentEntryPage());

    async Task OnPaymentTapped(RecurringPayment p)
    {
        var due = Payments.NextDue(p);
        string paid = $"✓ Ödendi ({due.ToString("d MMMM", Fmt.TR)}) — gidere ekle";
        var choice = await DisplayActionSheetAsync($"{Payments.Icon(p.Type)} {p.Name} — {Fmt.Money(p.Amount, 0)}", "Vazgeç", "Sil", paid, "Düzenle");
        if (choice == paid)
        {
            bool finished = await Payments.Pay(p);
            await DisplayAlertAsync(finished ? "🎉 Tebrikler" : "Ödendi",
                finished ? $"{p.Name} bitti! Son taksit ödendi." : $"{p.Name} gidere eklendi. Sonraki ödeme: {Payments.NextDue(p).ToString("d MMMM", Fmt.TR)}", "Tamam");
            await LoadAsync();
        }
        else if (choice == "Düzenle") await Nav.Push(new PaymentEntryPage(p));
        else if (choice == "Sil" && await Ui.Confirm("Sil", $"\"{p.Name}\" düzenli ödemelerden silinsin mi? (Geçmiş giderler silinmez.)"))
        {
            await Db.Delete(p);
            await Payments.Reschedule();
            await LoadAsync();
        }
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
