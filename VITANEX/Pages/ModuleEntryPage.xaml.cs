using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class ModuleEntryPage : ContentPage
{
    readonly ModuleEntry _entry;
    readonly ModulePage.ModuleInfo _info;

    public ModuleEntryPage(ModulePage.ModuleInfo info, ModuleEntry entry = null, string kind = null)
    {
        InitializeComponent();
        _info = info;
        _entry = entry ?? new ModuleEntry
        {
            Module = info.Key,
            Kind = kind ?? info.Kinds[0],
            Date = info.DateFirst ? DateTime.Today.AddDays(1).AddHours(19) : null
        };
        Title = entry == null ? $"{info.Title} — Yeni" : $"{info.Title} — Düzenle";

        KindField.ItemsSource = info.Kinds;
        KindField.SelectedItem = info.Kinds.Contains(_entry.Kind) ? _entry.Kind : info.Kinds[0];
        StatusField.ItemsSource = ModuleEntry.StatusNames;
        StatusField.SelectedIndex = Math.Clamp(_entry.Status, 0, 2);

        TitleField.Text = _entry.Title;
        NoteField.Text = _entry.Note;
        ProgressField.Value = _entry.Progress;
        ProgressLabel.Text = $"%{_entry.Progress}";

        DateSwitch.IsToggled = _entry.Date.HasValue;
        DateRow.IsVisible = _entry.Date.HasValue;
        var d = _entry.Date ?? DateTime.Today.AddHours(19);
        DateField.Date = d.Date;
        TimeField.Time = d.TimeOfDay;
    }

    void OnDateToggled(object sender, ToggledEventArgs e) => DateRow.IsVisible = e.Value;

    void OnProgressChanged(object sender, ValueChangedEventArgs e)
    {
        int v = (int)Math.Round(e.NewValue / 5) * 5;
        ProgressLabel.Text = $"%{v}";
    }

    async void OnSave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleField.Text))
        {
            await DisplayAlertAsync("Eksik bilgi", "Başlık yazın.", "Tamam");
            return;
        }
        _entry.Kind = KindField.SelectedItem as string ?? _info.Kinds[0];
        _entry.Title = TitleField.Text.Trim();
        _entry.Note = NoteField.Text?.Trim();
        _entry.Progress = (int)Math.Round(ProgressField.Value / 5) * 5;
        _entry.Status = Math.Max(0, StatusField.SelectedIndex);
        if (_entry.Status == 2) _entry.Progress = 100;
        else if (_entry.Progress >= 100) _entry.Status = 2;
        _entry.Date = DateSwitch.IsToggled ? ((DateTime)DateField.Date).Date + (TimeSpan)TimeField.Time : null;

        await Db.Save(_entry);
        await Navigation.PopAsync();
    }
}
