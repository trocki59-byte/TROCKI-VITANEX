using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class FinanceEntryPage : ContentPage
{
    readonly FinanceRecord _rec;
    bool _isIncome;

    public FinanceEntryPage(FinanceRecord rec = null)
    {
        InitializeComponent();
        _rec = rec ?? new FinanceRecord { Date = DateTime.Today, IsIncome = false };
        Title = rec == null ? "Yeni Gelir / Gider" : "Kaydı Düzenle";

        DescField.Text = _rec.Description;
        AmountField.Text = _rec.Amount > 0 ? _rec.Amount.ToString("0.##", Fmt.TR) : "";
        DateField.Date = _rec.Date.Date;
        SetType(_rec.IsIncome, _rec.Category);
    }

    void SetType(bool income, string category = null)
    {
        _isIncome = income;
        IncomeBtn.BackgroundColor = income ? AppColors.Green : AppColors.PrimaryLight;
        IncomeBtn.TextColor = income ? Colors.White : AppColors.Green;
        ExpenseBtn.BackgroundColor = !income ? AppColors.Red : AppColors.PrimaryLight;
        ExpenseBtn.TextColor = !income ? Colors.White : AppColors.Red;

        var cats = income ? FinanceRecord.IncomeCategories : FinanceRecord.ExpenseCategories;
        CategoryField.ItemsSource = cats;
        CategoryField.SelectedItem = category != null && cats.Contains(category) ? category : cats[0];
    }

    void OnIncome(object s, EventArgs e) => SetType(true);
    void OnExpense(object s, EventArgs e) => SetType(false);

    async void OnSave(object sender, EventArgs e)
    {
        var amount = Fmt.ParseDecimal(AmountField.Text);
        if (amount == null || amount <= 0)
        {
            await DisplayAlertAsync("Eksik bilgi", "Geçerli bir tutar girin.", "Tamam");
            return;
        }
        _rec.IsIncome = _isIncome;
        _rec.Amount = amount.Value;
        _rec.Category = CategoryField.SelectedItem as string ?? "Diğer";
        _rec.Description = string.IsNullOrWhiteSpace(DescField.Text) ? _rec.Category : DescField.Text.Trim();
        _rec.Date = ((DateTime)DateField.Date).Date.AddHours(12);
        await Db.Save(_rec);
        await Navigation.PopAsync();
    }
}
