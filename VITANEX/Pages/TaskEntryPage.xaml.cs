using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class TaskEntryPage : ContentPage
{
    readonly TaskItem _task;

    public TaskEntryPage(TaskItem task = null)
    {
        InitializeComponent();
        _task = task ?? new TaskItem { Date = DateTime.Today, Priority = 1, Status = 0 };
        Title = task == null ? "Yeni Görev" : "Görevi Düzenle";

        PriorityField.ItemsSource = TaskItem.PriorityNames;
        StatusField.ItemsSource = TaskItem.StatusNames;

        TitleField.Text = _task.Title;
        DateField.Date = _task.Date.Date;
        bool hasTime = !string.IsNullOrEmpty(_task.Time) && TimeSpan.TryParse(_task.Time, out _);
        TimeSwitch.IsToggled = hasTime;
        TimeField.IsEnabled = hasTime;
        TimeField.Time = hasTime ? TimeSpan.Parse(_task.Time) : new TimeSpan(9, 0, 0);
        PriorityField.SelectedIndex = Math.Clamp(_task.Priority, 0, 2);
        StatusField.SelectedIndex = Math.Clamp(_task.Status, 0, 2);
        NoteField.Text = _task.Note;
    }

    void OnTimeToggled(object sender, ToggledEventArgs e) => TimeField.IsEnabled = e.Value;

    async void OnSave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleField.Text))
        {
            await DisplayAlertAsync("Eksik bilgi", "Görev başlığını yazın.", "Tamam");
            return;
        }
        int oldStatus = _task.Status;
        _task.Title = TitleField.Text.Trim();
        _task.Date = ((DateTime)DateField.Date).Date;
        _task.Time = TimeSwitch.IsToggled ? ((TimeSpan)TimeField.Time).ToString(@"hh\:mm") : null;
        _task.Priority = Math.Max(0, PriorityField.SelectedIndex);
        _task.Status = Math.Max(0, StatusField.SelectedIndex);
        _task.Note = NoteField.Text?.Trim();
        if (_task.Status == 2 && oldStatus != 2) _task.CompletedAt = DateTime.Now;
        if (_task.Status != 2) _task.CompletedAt = null;

        await Db.Save(_task);
        await Navigation.PopAsync();
    }
}
