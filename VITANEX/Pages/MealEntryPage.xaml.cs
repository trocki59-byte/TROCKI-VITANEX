using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class MealEntryPage : ContentPage
{
    readonly Meal _meal;

    public MealEntryPage(Meal meal = null, DateTime? day = null)
    {
        InitializeComponent();
        TypeField.ItemsSource = Meal.Types;

        if (meal == null)
        {
            var now = DateTime.Now;
            var d = day ?? DateTime.Today;
            _meal = new Meal
            {
                Date = d == DateTime.Today ? now : d.AddHours(12),
                MealType = now.Hour < 11 ? Meal.Types[0] : now.Hour < 16 ? Meal.Types[1] : now.Hour < 21 ? Meal.Types[2] : Meal.Types[3]
            };
            Title = "Yeni Öğün";
        }
        else
        {
            _meal = meal;
            Title = "Öğünü Düzenle";
        }

        TypeField.SelectedItem = _meal.MealType;
        DateField.Date = _meal.Date.Date;
        TimeField.Time = _meal.Date.TimeOfDay;
        DescField.Text = _meal.Description;
        CalField.Text = _meal.Calories?.ToString();
        NoteField.Text = _meal.Note;
    }

    async void OnSave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescField.Text))
        {
            await DisplayAlertAsync("Eksik bilgi", "Ne yediğinizi yazın.", "Tamam");
            return;
        }
        _meal.MealType = TypeField.SelectedItem as string ?? Meal.Types[3];
        _meal.Date = ((DateTime)DateField.Date).Date + (TimeSpan)TimeField.Time;
        _meal.Description = DescField.Text.Trim();
        _meal.Calories = Fmt.ParseInt(CalField.Text);
        _meal.Note = NoteField.Text?.Trim();
        await Db.Save(_meal);
        await Navigation.PopAsync();
    }
}
