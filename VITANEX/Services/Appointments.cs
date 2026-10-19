using VITANEX.Models;

namespace VITANEX.Services;

/// <summary>Doktor randevuları. Hatırlatma: 1 gün önce (aynı saatte) ve 2 saat önce.</summary>
public static class Appointments
{
    public static readonly string[] Depts =
    {
        "Aile Hekimi", "Dahiliye", "Kardiyoloji", "Göğüs Hastalıkları", "Nöroloji", "Ortopedi", "Göz", "KBB",
        "Üroloji", "Endokrinoloji", "Fizik Tedavi", "Diş", "Cildiye", "Genel Cerrahi", "Psikiyatri", "Kontrol / Tahlil", "Diğer"
    };

    public static string Title(DoctorAppointment a) =>
        $"{a.Doctor}{(string.IsNullOrWhiteSpace(a.Dept) ? "" : " · " + a.Dept)}";

    public static int DaysLeft(DoctorAppointment a) => (int)(a.Date.Date - DateTime.Today).TotalDays;

    public static string WhenText(DoctorAppointment a)
    {
        if (a.Done) return "✓ Gidildi";
        int n = DaysLeft(a);
        return n < 0 ? "Geçti" : n == 0 ? "BUGÜN" : n == 1 ? "Yarın" : $"{n} gün kaldı";
    }

    public static Color WhenColor(DoctorAppointment a)
    {
        if (a.Done) return AppColors.Green;
        int n = DaysLeft(a);
        return n == 0 ? AppColors.Red : n == 1 ? AppColors.Orange : AppColors.Muted;
    }

    public static async Task<List<DoctorAppointment>> All()
    {
        var c = await Db.Get();
        return await c.Table<DoctorAppointment>().ToListAsync();
    }

    /// <summary>Yaklaşan (gidilmemiş, dünden sonra) randevular — en yakını önce.</summary>
    public static async Task<List<DoctorAppointment>> Upcoming() =>
        (await All()).Where(a => !a.Done && a.Date >= DateTime.Today.AddDays(-1)).OrderBy(a => a.Date).ToList();

    /// <summary>Bugün veya yarın olan randevular.</summary>
    public static async Task<List<DoctorAppointment>> Due() =>
        (await All()).Where(a => !a.Done && a.Date >= DateTime.Today && a.Date < DateTime.Today.AddDays(2))
                     .OrderBy(a => a.Date).ToList();

    public static async Task<List<PaymentAlarm>> Alarms()
    {
        var list = new List<PaymentAlarm>();
        try
        {
            var now = DateTime.Now;
            foreach (var a in (await All()).Where(x => !x.Done && x.Date > now))
            {
                string t = Title(a), at = a.Date.ToString("HH:mm", Fmt.TR);
                string place = string.IsNullOrWhiteSpace(a.Place) ? "" : $" · {a.Place}";
                var dayBefore = a.Date.AddDays(-1);
                var twoHours = a.Date.AddHours(-2);
                int baseId = 2_000_000 + (a.Id % 400_000) * 2;
                if (dayBefore > now)
                    list.Add(new PaymentAlarm(baseId, dayBefore, "🩺 Yarın doktor randevunuz var",
                        $"{t} — {a.Date.ToString("d MMMM dddd", Fmt.TR)} saat {at}{place}"));
                if (twoHours > now)
                    list.Add(new PaymentAlarm(baseId + 1, twoHours, "🩺 2 saat sonra doktor randevusu",
                        $"{t} — saat {at}{place}" + (string.IsNullOrWhiteSpace(a.Note) ? "" : $"\n📝 {a.Note}")));
            }
        }
        catch { }
        return list;
    }

    public static async Task Save(DoctorAppointment a)
    {
        await Db.Save(a);
        await Payments.Reschedule();
    }

    public static async Task Delete(DoctorAppointment a)
    {
        await Db.Delete(a);
        await Payments.Reschedule();
    }
}
