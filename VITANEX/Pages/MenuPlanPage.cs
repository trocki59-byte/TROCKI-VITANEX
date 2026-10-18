using VITANEX.Services;

namespace VITANEX.Pages;

/// <summary>Bu ayın 30 günlük kilo verme menüsü ve ipuçları.</summary>
public class MenuPlanPage : ContentPage
{
    public MenuPlanPage()
    {
        Title = "Aylık Menü";
        var today = DateTime.Today;
        var first = new DateTime(today.Year, today.Month, 1);
        int days = DateTime.DaysInMonth(today.Year, today.Month);

        var root = new VerticalStackLayout { Padding = new Thickness(16, 16, 16, 40), Spacing = 12, MaximumWidthRequest = 820 };
        root.Children.Add(new Label
        {
            Style = Ui.Style("Muted"), FontSize = 13, LineHeight = 1.3,
            Text = $"{today.ToString("MMMM yyyy", Fmt.TR)} için 30 günlük kilo verme menüsü (günde yaklaşık 1.350–1.650 kcal). Bugün yeşil çerçeveli."
        });

        View todayCard = null;
        for (int i = 0; i < days; i++)
        {
            var d = first.AddDays(i);
            var stack = new VerticalStackLayout { Spacing = 5 };
            var head = new Grid();
            Ui.Cols(head, -1, 0);
            head.Add(new Label { Text = d.ToString("d MMMM dddd", Fmt.TR), FontAttributes = FontAttributes.Bold, TextColor = AppColors.PrimaryDark, FontSize = 15 }, 0, 0);
            head.Add(new Label { Text = $"{MealPlan.DayTotal(d)} kcal", Style = Ui.Style("Muted"), VerticalOptions = LayoutOptions.Center }, 1, 0);
            stack.Children.Add(head);
            foreach (var s in MealPlan.Slots)
                stack.Children.Add(new Label { Text = $"{s.Icon}  {MealPlan.Item(d, s).Text}", FontSize = 13, LineHeight = 1.15, TextColor = AppColors.Text });

            var card = new Border { Style = Ui.Style("Card"), Content = stack };
            if (d == today)
            {
                card.Stroke = new SolidColorBrush(AppColors.Green);
                card.StrokeThickness = 2;
                todayCard = card;
            }
            root.Children.Add(card);
        }

        var tips = new VerticalStackLayout { Spacing = 6 };
        tips.Children.Add(new Label { Text = "💡 Kilo Verme İpuçları", Style = Ui.Style("SectionTitle") });
        foreach (var t in MealPlan.Tips)
            tips.Children.Add(new Label { Text = "• " + t, FontSize = 14, TextColor = AppColors.Text });
        tips.Children.Add(new Label
        {
            Style = Ui.Style("Muted"), LineHeight = 1.3, Margin = new Thickness(0, 6, 0, 0),
            Text = "Bu menü genel bir öneridir. Diyabet, tansiyon, böbrek hastalığı gibi bir durumunuz varsa ya da ilaç kullanıyorsanız diyetisyeninize / doktorunuza danışın."
        });
        root.Children.Add(new Border { Style = Ui.Style("Card"), Content = tips });

        var scroll = new ScrollView { Content = root };
        Content = scroll;

        // Açılınca bugünün kartına kaydır
        Appearing += async (s, e) =>
        {
            if (todayCard == null) return;
            await Task.Delay(150);
            try { await scroll.ScrollToAsync(todayCard, ScrollToPosition.Start, false); } catch { }
        };
    }
}
