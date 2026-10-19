using VITANEX.Models;

namespace VITANEX.Services;

/// <summary>Düzenli ödemeler: kredi taksiti, kredi kartı, KMH, kira, aidat… Hatırlatma: 1 gün önce ve ödeme günü 09:00.</summary>
public static class Payments
{
    public static readonly (string Type, string Icon)[] Types =
    {
        ("Kredi Taksiti", "🏦"), ("Kredi Kartı", "💳"), ("KMH (Ek Hesap)", "🏧"),
        ("Kira", "🏠"), ("Aidat", "🏢"), ("Fatura", "🧾"), ("Diğer", "📌")
    };

    public const int NotifyHour = 9;

    public static string Icon(string type) => Types.FirstOrDefault(t => t.Type == type).Icon ?? "📌";

    public static string Category(string type) =>
        type == "Fatura" ? "Faturalar" : FinanceRecord.ExpenseCategories.Contains(type) ? type : "Diğer";

    public static string Key(DateTime d) => $"{d.Year}-{d.Month:00}";

    static DateTime MonthStart(DateTime d) => new(d.Year, d.Month, 1);

    public static DateTime DueIn(RecurringPayment p, DateTime month)
    {
        var m = MonthStart(month);
        int last = DateTime.DaysInMonth(m.Year, m.Month);
        return new DateTime(m.Year, m.Month, Math.Min(Math.Max(p.Day, 1), last));
    }

    /// <summary>Sıradaki ödenmemiş ödeme günü (geçen ay ödenmediyse onu döndürür).</summary>
    public static DateTime NextDue(RecurringPayment p)
    {
        var m0 = MonthStart(DateTime.Today);
        var prev = DueIn(p, m0.AddMonths(-1));
        if (prev >= p.Since.Date && !p.IsPaid(Key(prev))) return prev;
        var d = DueIn(p, m0);
        if (p.IsPaid(Key(d))) d = DueIn(p, m0.AddMonths(1));
        if (p.StartDate.HasValue && d < p.StartDate.Value.Date)
        {
            d = DueIn(p, p.StartDate.Value);
            if (d < p.StartDate.Value.Date) d = DueIn(p, p.StartDate.Value.AddMonths(1));
        }
        return d;
    }

    public static int DaysLeft(RecurringPayment p) => (int)(NextDue(p) - DateTime.Today).TotalDays;

    public static string WhenText(int n) =>
        n < 0 ? $"{-n} gün gecikti!" : n == 0 ? "BUGÜN" : n == 1 ? "Yarın" : $"{n} gün kaldı";

    public static Color WhenColor(int n) => n <= 0 ? AppColors.Red : n == 1 ? AppColors.Orange : AppColors.Muted;

    public static async Task<List<RecurringPayment>> Active()
    {
        var c = await Db.Get();
        return (await c.Table<RecurringPayment>().Where(p => p.Active).ToListAsync())
               .OrderBy(DaysLeft).ToList();
    }

    /// <summary>Yarın, bugün veya gecikmiş ödemeler.</summary>
    public static async Task<List<RecurringPayment>> Due() =>
        (await Active()).Where(p => DaysLeft(p) <= 1).ToList();

    /// <summary>Yeni ödeme: bu ayın günü geçtiyse bu ay ödenmiş sayılır.</summary>
    public static async Task Add(RecurringPayment p)
    {
        if (p.StartDate.HasValue && p.StartDate.Value.Date > DateTime.Today)
            p.Since = p.StartDate.Value.Date;              // ileri tarihli: o güne kadar ödeme yok
        else
        {
            p.StartDate = null;
            p.Since = DateTime.Today;
            var d = DueIn(p, DateTime.Today);
            if (d < DateTime.Today) p.MarkPaid(Key(d));
        }
        await Db.Save(p);
        await Reschedule();
    }

    /// <summary>Ödendi: gider olarak kaydeder, sonraki aya geçer, taksiti düşer. Kredi bittiyse true.</summary>
    public static async Task<bool> Pay(RecurringPayment p)
    {
        var due = NextDue(p);
        await Db.Save(new FinanceRecord
        {
            IsIncome = false, Description = p.Name, Amount = p.Amount, Date = DateTime.Now, Category = Category(p.Type)
        });
        p.MarkPaid(Key(due));
        bool finished = false;
        if (p.Left.HasValue)
        {
            p.Left--;
            if (p.Left <= 0) { p.Active = false; finished = true; }
        }
        await Db.Save(p);
        await Reschedule();
        return finished;
    }

    // ───────── Telefon bildirimleri ─────────
    public static async Task<bool> RequestPermission()
    {
        try
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(33)) return true;
            var st = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (st != PermissionStatus.Granted) st = await Permissions.RequestAsync<Permissions.PostNotifications>();
            return st == PermissionStatus.Granted;
        }
        catch { return false; }
    }

    /// <summary>
    /// Tüm telefon bildirimlerini yeniden kurar: ödemeler (1 gün önce 09:00 ve ödeme günü 09:00)
    /// ve açıksa öğün saatleri (önümüzdeki 14 gün).
    /// </summary>
    public static async Task Reschedule()
    {
        try
        {
            var list = new List<PaymentAlarm>();
            var now = DateTime.Now;
            foreach (var p in await Active())
            {
                var first = NextDue(p);
                int count = p.Left.HasValue ? Math.Min(p.Left.Value, 3) : 3;   // önümüzdeki 3 ay
                for (int k = 0; k < count; k++)
                {
                    var due = k == 0 ? first : DueIn(p, first.AddMonths(k));
                    var before = due.AddDays(-1).AddHours(NotifyHour);
                    var onDay = due.AddHours(NotifyHour);
                    string money = Fmt.Money(p.Amount, 0);
                    if (before > now)
                        list.Add(new PaymentAlarm(p.Id * 100 + k * 2, before, $"{Icon(p.Type)} Yarın ödeme günü",
                            $"{p.Name} — {money} ({due.ToString("d MMMM", Fmt.TR)})"));
                    if (onDay > now)
                        list.Add(new PaymentAlarm(p.Id * 100 + k * 2 + 1, onDay, $"{Icon(p.Type)} Bugün ödeme günü",
                            $"{p.Name} — {money}. Ödediyseniz VİTANEX'te ✓ Ödendi deyin."));
                }
            }
            list.AddRange(MealPlan.Alarms());
            list.AddRange(await Medications.Alarms());
            list.AddRange(await Appointments.Alarms());
#if ANDROID
            VITANEX.Droid.PaymentNotifier.Schedule(list);
#endif
        }
        catch { }
    }
}

public record PaymentAlarm(int Id, DateTime When, string Title, string Body);
