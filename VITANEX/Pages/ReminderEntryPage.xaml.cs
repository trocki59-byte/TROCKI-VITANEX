using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class ReminderEntryPage : ContentPage
{
    readonly Reminder _rem;
    string _icon;

    public ReminderEntryPage(Reminder rem = null)
    {
        InitializeComponent();
        _rem = rem ?? new Reminder { Time = DateTime.Now.AddHours(1).ToString("HH:00"), Icon = "⏰", Active = true };
        Title = rem == null ? "Yeni Hatırlatıcı" : "Hatırlatıcıyı Düzenle";

        TitleField.Text = _rem.Title;
        TimeField.Time = TimeSpan.TryParse(_rem.Time, out var ts) ? ts : new TimeSpan(9, 0, 0);
        ActiveSwitch.IsToggled = _rem.Active;
        _icon = _rem.Icon ?? "⏰";
        BuildIcons();
    }

    void BuildIcons()
    {
        IconPanel.Children.Clear();
        for (int i = 0; i < Reminder.Icons.Length; i++)
        {
            var ic = Reminder.Icons[i];
            var selected = ic == _icon;
            var b = new Border
            {
                HeightRequest = 46,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                StrokeThickness = selected ? 2 : 0,
                Stroke = new SolidColorBrush(AppColors.Primary),
                BackgroundColor = selected ? AppColors.PrimaryLight : AppColors.Surface2,
                Content = new Label { Text = ic, FontSize = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };
            var tap = new TapGestureRecognizer();
            var icon = ic;
            tap.Tapped += (s, e) => { _icon = icon; BuildIcons(); };
            b.GestureRecognizers.Add(tap);
            IconPanel.Add(b, i % 6, i / 6);
        }
    }

    async void OnSave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleField.Text))
        {
            await DisplayAlertAsync("Eksik bilgi", "Hatırlatıcı metnini yazın.", "Tamam");
            return;
        }
        _rem.Title = TitleField.Text.Trim();
        _rem.Time = ((TimeSpan)TimeField.Time).ToString(@"hh\:mm");
        _rem.Icon = _icon;
        _rem.Active = ActiveSwitch.IsToggled;
        await Db.Save(_rem);
        await Navigation.PopAsync();
    }
}
