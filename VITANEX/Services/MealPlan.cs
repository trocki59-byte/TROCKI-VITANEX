using VITANEX.Models;

namespace VITANEX.Services;

/// <summary>Kilo vermeye yönelik 30 günlük hazır menü (sabah, kuşluk, öğle, ikindi, akşam).</summary>
public static partial class MealPlan
{
    public record Slot(string Key, string Icon, string Label, string ListKey, string MealType, double Hour);

    public static readonly Slot[] Slots =
    {
        new("Kahvaltı", "🍳", "Kahvaltı", "Kahvaltı", "Kahvaltı", 8),
        new("Ara Öğün", "🍎", "Kuşluk Ara Öğünü", "Ara Öğün", "Ara Öğün", 10.5),
        new("Öğle Yemeği", "🥗", "Öğle Yemeği", "Öğle Yemeği", "Öğle Yemeği", 12.5),
        new("İkindi", "🍐", "İkindi Ara Öğünü", "Ara Öğün", "Ara Öğün", 16),
        new("Akşam Yemeği", "🍲", "Akşam Yemeği", "Akşam Yemeği", "Akşam Yemeği", 19),
    };

    public static int DayNo(DateTime d) => (d.Day - 1) % 30 + 1;

    /// <summary>Her gün farklı bir birleşim gelecek şekilde menü sırası.</summary>
    static int BaseIndex(int day, string slot)
    {
        int i = (day - 1) % 30;
        bool h = i >= 15;
        return slot switch
        {
            "Kahvaltı" => i % 15,
            "Öğle Yemeği" => (i * 7 + (h ? 5 : 0)) % 15,
            "Akşam Yemeği" => (i * 4 + (h ? 2 : 0)) % 15,
            "İkindi" => (i + 5) % 10,
            _ => i % 10
        };
    }

    static string SwapKey(DateTime d, string slot) => $"menu_swap_{d:yyyyMMdd}_{slot}";

    public static (int Index, string Text, int Cal) Item(DateTime date, Slot s)
    {
        var list = Lists[s.ListKey];
        int idx = Preferences.Default.Get(SwapKey(date, s.Key), -1);
        if (idx < 0) idx = BaseIndex(DayNo(date), s.Key);
        idx = ((idx % list.Length) + list.Length) % list.Length;
        return (idx, list[idx].Text, list[idx].Cal);
    }

    public static int DayTotal(DateTime date) => Slots.Sum(s => Item(date, s).Cal);

    /// <summary>Bu öğünü listede bir sonrakiyle değiştirir.</summary>
    public static void Swap(DateTime date, Slot s) =>
        Preferences.Default.Set(SwapKey(date, s.Key), Item(date, s).Index + 1);

    // ───────── Öğün bildirimleri ─────────
    public static bool NotifyEnabled
    {
        get => Preferences.Default.Get("meal_notify", false);
        set => Preferences.Default.Set("meal_notify", value);
    }

    /// <summary>Önümüzdeki 14 günün öğün saatleri için bildirim listesi (o günün menüsüyle).</summary>
    public static List<PaymentAlarm> Alarms()
    {
        var list = new List<PaymentAlarm>();
        if (!NotifyEnabled) return list;
        var now = DateTime.Now;
        for (int d = 0; d < 14; d++)
        {
            var day = DateTime.Today.AddDays(d);
            for (int k = 0; k < Slots.Length; k++)
            {
                var s = Slots[k];
                var when = day.AddHours(s.Hour);
                if (when <= now) continue;
                var it = Item(day, s);
                list.Add(new PaymentAlarm(900000 + d * 10 + k, when, $"{s.Icon} {s.Label} zamanı · {it.Cal} kcal", it.Text));
            }
        }
        return list;
    }

    static string Marker(Slot s) => $"📋 Hazır menüden ({s.Label})";

    public static bool Eaten(IEnumerable<Meal> dayMeals, Slot s) => dayMeals.Any(m => m.Note == Marker(s));

    /// <summary>"Yedim": öğünü kaydeder; zaten kayıtlıysa geri alır. Kaydedildiyse true.</summary>
    public static async Task<bool> ToggleEat(DateTime date, Slot s)
    {
        var day = date.Date;
        var existing = (await Db.Between<Meal>(day, day.AddDays(1))).FirstOrDefault(m => m.Note == Marker(s));
        if (existing != null)
        {
            await Db.Delete(existing);
            return false;
        }
        var it = Item(day, s);
        await Db.Save(new Meal
        {
            Date = day.AddHours(s.Hour),
            MealType = s.MealType,
            Description = it.Text,
            Calories = it.Cal,
            Note = Marker(s)
        });
        return true;
    }
}
