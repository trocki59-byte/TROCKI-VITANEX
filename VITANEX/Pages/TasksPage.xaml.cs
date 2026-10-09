using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class TasksPage : ContentPage
{
    string _filter = "today";

    public TasksPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        foreach (var b in Chips.Children.OfType<Button>())
            Ui.SetChip(b, b.ClassId == _filter);

        var c = await Db.Get();
        var all = await c.Table<TaskItem>().ToListAsync();
        var today = DateTime.Today;

        var todays = all.Where(t => t.Date.Date == today).ToList();
        int done = todays.Count(t => t.Status == 2);
        TodayLabel.Text = $"{done} / {todays.Count}";
        TodayBar.Progress = todays.Count == 0 ? 0 : (double)done / todays.Count;
        CountsLabel.Text = $"Bekliyor: {all.Count(t => t.Status == 0)}   ·   Devam: {all.Count(t => t.Status == 1)}   ·   Tamamlandı: {all.Count(t => t.Status == 2)}";

        IEnumerable<TaskItem> list = _filter switch
        {
            "today" => todays,
            "upcoming" => all.Where(t => t.Date.Date > today && t.Status != 2),
            "0" => all.Where(t => t.Status == 0),
            "1" => all.Where(t => t.Status == 1),
            "2" => all.Where(t => t.Status == 2),
            _ => all
        };

        var sorted = _filter == "2"
            ? list.OrderByDescending(t => t.CompletedAt ?? t.Date).ToList()
            : list.OrderBy(t => t.Status == 2).ThenBy(t => t.Date).ThenBy(t => string.IsNullOrEmpty(t.Time) ? "99" : t.Time)
                  .ThenByDescending(t => t.Priority).ToList();

        BindableLayout.SetItemsSource(TaskList, sorted);
        EmptyLabel.IsVisible = sorted.Count == 0;
    }

    async void OnFilter(object sender, EventArgs e)
    {
        _filter = ((Button)sender).ClassId;
        await LoadAsync();
    }

    async void OnAdd(object s, EventArgs e) => await Nav.Push(new TaskEntryPage());

    /// <summary>Daireye dokununca: Bekliyor → Devam Ediyor → Tamamlandı → Bekliyor</summary>
    async void OnToggleTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not TaskItem t) return;
        await SetStatus(t, (t.Status + 1) % 3);
    }

    async Task SetStatus(TaskItem t, int status)
    {
        t.Status = status;
        t.CompletedAt = status == 2 ? DateTime.Now : null;
        await Db.Save(t);
        await LoadAsync();
    }

    async void OnTaskTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not TaskItem t) return;
        var choice = await DisplayActionSheetAsync(t.Title, "Vazgeç", "Sil",
            "✓ Tamamlandı", "▶ Devam Ediyor", "⏳ Bekliyor", "Düzenle", "Yarına ertele");
        switch (choice)
        {
            case "✓ Tamamlandı": await SetStatus(t, 2); break;
            case "▶ Devam Ediyor": await SetStatus(t, 1); break;
            case "⏳ Bekliyor": await SetStatus(t, 0); break;
            case "Düzenle": await Nav.Push(new TaskEntryPage(t)); break;
            case "Yarına ertele":
                t.Date = DateTime.Today.AddDays(1);
                await Db.Save(t);
                await LoadAsync();
                break;
            case "Sil":
                if (await Ui.Confirm("Sil", $"\"{t.Title}\" silinsin mi?"))
                {
                    await Db.Delete(t);
                    await LoadAsync();
                }
                break;
        }
    }
}
