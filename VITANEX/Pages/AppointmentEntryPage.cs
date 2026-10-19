using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

/// <summary>Doktor randevusu ekleme / düzenleme.</summary>
public class AppointmentEntryPage : ContentPage
{
    readonly DoctorAppointment _a;
    readonly bool _isNew;
    readonly Entry _doctor, _place;
    readonly Picker _dept;
    readonly DatePicker _date;
    readonly TimePicker _time;
    readonly Editor _note;

    public AppointmentEntryPage(DoctorAppointment a = null)
    {
        _isNew = a == null;
        _a = a ?? new DoctorAppointment { Date = DateTime.Today.AddDays(1).AddHours(9) };
        Title = _isNew ? "Yeni Doktor Randevusu" : "Randevuyu Düzenle";

        _doctor = new Entry { Placeholder = "örn. Dr. Ayşe Yılmaz", Text = _a.Doctor };
        _dept = new Picker { Title = "Bölüm seçin (isteğe bağlı)", ItemsSource = Appointments.Depts.ToList() };
        _dept.SelectedIndex = Array.IndexOf(Appointments.Depts, _a.Dept ?? "");
        _date = new DatePicker { Date = _a.Date.Date, Format = "d MMMM yyyy dddd" };
        _time = new TimePicker { Time = _a.Date.TimeOfDay, Format = "HH:mm" };
        _place = new Entry { Placeholder = "örn. Çerkezköy Devlet Hastanesi", Text = _a.Place };
        _note = new Editor { Placeholder = "örn. Tahlil sonuçlarını götür, aç karnına git", Text = _a.Note, AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 70 };

        var save = new Button { Text = "Kaydet" };
        save.Clicked += OnSave;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16), Spacing = 12, MaximumWidthRequest = 640,
                Children =
                {
                    new Border
                    {
                        Style = Ui.Style("Card"),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 4,
                            Children =
                            {
                                Field("👨‍⚕️ Doktor adı"), _doctor,
                                Field("🏥 Bölüm"), _dept,
                                Field("📅 Randevu tarihi"), _date,
                                Field("🕘 Saat"), _time,
                                Field("📍 Hastane / Yer"), _place,
                                Field("📝 Not"), _note,
                            }
                        }
                    },
                    new Label
                    {
                        Style = Ui.Style("Muted"), LineHeight = 1.3,
                        Text = "🔔 Randevudan 1 gün önce (aynı saatte) ve 2 saat önce telefonunuza bildirim gelir."
                    },
                    save
                }
            }
        };
    }

    static Label Field(string t) => new() { Text = t, Style = Ui.Style("FieldLabel") };

    async void OnSave(object sender, EventArgs e)
    {
        string dept = _dept.SelectedIndex >= 0 ? Appointments.Depts[_dept.SelectedIndex] : "";
        string doctor = _doctor.Text?.Trim() ?? "";
        if (doctor.Length == 0 && dept.Length == 0)
        {
            await DisplayAlertAsync("Eksik bilgi", "Doktor adını veya bölümü yazın.", "Tamam");
            return;
        }
        _a.Doctor = doctor.Length > 0 ? doctor : dept;
        _a.Dept = doctor.Length > 0 ? dept : "";
        _a.Date = ((DateTime)_date.Date).Date + (TimeSpan)_time.Time;
        _a.Place = _place.Text?.Trim() ?? "";
        _a.Note = _note.Text?.Trim() ?? "";

        await Appointments.Save(_a);
        if (_isNew && !await Payments.RequestPermission())
            await DisplayAlertAsync("Bildirim izni", "Randevu hatırlatması gelmesi için bildirim iznine izin verin:\nAyarlar → Uygulamalar → TROÇKİ VİTANEX → Bildirimler.", "Tamam");
        await Navigation.PopAsync();
    }
}
