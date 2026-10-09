using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

public partial class HealthEntryPage : ContentPage
{
    readonly HealthRecord _rec;

    public HealthEntryPage(HealthRecord rec = null)
    {
        InitializeComponent();
        _rec = rec ?? new HealthRecord { Date = DateTime.Now };
        Title = rec == null ? "Yeni Sağlık Kaydı" : "Kaydı Düzenle";

        DateField.Date = _rec.Date.Date;
        TimeField.Time = _rec.Date.TimeOfDay;
        WeightField.Text = Fmt.Num(_rec.Weight);
        SysField.Text = _rec.Systolic?.ToString();
        DiaField.Text = _rec.Diastolic?.ToString();
        PulseField.Text = _rec.Pulse?.ToString();
        SpO2Field.Text = _rec.SpO2?.ToString();
        SleepField.Text = Fmt.Num(_rec.SleepHours);
        ExerciseField.Text = _rec.ExerciseMin?.ToString();
        NoteField.Text = _rec.Note;
    }

    async void OnSave(object sender, EventArgs e)
    {
        _rec.Date = ((DateTime)DateField.Date).Date + (TimeSpan)TimeField.Time;
        _rec.Weight = Fmt.ParseDouble(WeightField.Text);
        _rec.Systolic = Fmt.ParseInt(SysField.Text);
        _rec.Diastolic = Fmt.ParseInt(DiaField.Text);
        _rec.Pulse = Fmt.ParseInt(PulseField.Text);
        _rec.SpO2 = Fmt.ParseInt(SpO2Field.Text);
        _rec.SleepHours = Fmt.ParseDouble(SleepField.Text);
        _rec.ExerciseMin = Fmt.ParseInt(ExerciseField.Text);
        _rec.Note = NoteField.Text?.Trim();

        bool empty = _rec.Weight == null && _rec.Systolic == null && _rec.Pulse == null && _rec.SpO2 == null &&
                     _rec.SleepHours == null && _rec.ExerciseMin == null && string.IsNullOrWhiteSpace(_rec.Note);
        if (empty)
        {
            await DisplayAlertAsync("Eksik bilgi", "En az bir değer girin.", "Tamam");
            return;
        }
        if (_rec.SpO2 is < 50 or > 100)
        {
            await DisplayAlertAsync("Kontrol", "Oksijen satürasyonu 50–100 arasında olmalı.", "Tamam");
            return;
        }

        await Db.Save(_rec);
        await Navigation.PopAsync();
    }
}
