using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class RemindersPage : ContentPage
{
    public RemindersPage()
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
        var c = await Db.Get();
        var list = (await c.Table<Reminder>().ToListAsync()).OrderBy(r => r.Time).ToList();
        BindableLayout.SetItemsSource(ReminderList, list);
        EmptyLabel.IsVisible = list.Count == 0;
    }

    async void OnAdd(object s, EventArgs e) => await Nav.Push(new ReminderEntryPage());

    async void OnTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Reminder r) return;
        var choice = await DisplayActionSheetAsync($"{r.Time} {r.Title}", "Vazgeç", "Sil",
            "Düzenle", r.Active ? "Kapat" : "Aç");
        switch (choice)
        {
            case "Düzenle": await Nav.Push(new ReminderEntryPage(r)); break;
            case "Kapat":
            case "Aç":
                r.Active = !r.Active;
                await Db.Save(r);
                await LoadAsync();
                break;
            case "Sil":
                if (await Ui.Confirm("Sil", "Hatırlatıcı silinsin mi?"))
                {
                    await Db.Delete(r);
                    await LoadAsync();
                }
                break;
        }
    }
}
