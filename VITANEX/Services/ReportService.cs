using System.Text;
using VITANEX.Models;

namespace VITANEX.Services;

public class ReportRow
{
    public string Label { get; set; }
    public string Value { get; set; }
}

public class ReportBar
{
    public string Label { get; set; }
    public double Value { get; set; }
    public string ValueText { get; set; }
}

public class ReportSection
{
    public string Title { get; set; }
    public List<ReportRow> Rows { get; } = new();
    public List<ReportBar> Bars { get; } = new();
    public string BarsTitle { get; set; }

    public void Add(string label, string value) => Rows.Add(new ReportRow { Label = label, Value = value });
}

public class ReportDoc
{
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public List<ReportSection> Sections { get; } = new();

    public string ToPlainText()
    {
        var sb = new StringBuilder();
        foreach (var s in Sections)
        {
            sb.AppendLine($"■ {s.Title}");
            foreach (var r in s.Rows) sb.AppendLine($"   {r.Label}: {r.Value}");
            if (s.Bars.Count > 0)
            {
                sb.AppendLine($"   {s.BarsTitle}:");
                foreach (var b in s.Bars) sb.AppendLine($"     • {b.Label}: {b.ValueText}");
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}

public static class ReportService
{
    public static readonly string[] Types =
        { "Genel Yaşam Özeti", "Sağlık Raporu", "Finans Raporu", "Görev Raporu", "Beslenme Raporu" };

    public static async Task<ReportDoc> Build(int type, DateTime start, DateTime endInclusive)
    {
        var end = endInclusive.Date.AddDays(1);
        var doc = new ReportDoc
        {
            Title = Types[type],
            Subtitle = $"{start:dd.MM.yyyy} – {endInclusive:dd.MM.yyyy}   ·   Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}"
        };

        bool all = type == 0;
        if (all || type == 1) doc.Sections.Add(await Health(start, end));
        if (all || type == 2) doc.Sections.Add(await Finance(start, end));
        if (all || type == 3) doc.Sections.Add(await Tasks(start, end));
        if (all || type == 4) doc.Sections.Add(await Nutrition(start, end));
        if (all) doc.Sections.Add(await Modules());
        return doc;
    }

    static string Avg(IEnumerable<double> v, string fmt = "0.#") =>
        v.Any() ? v.Average().ToString(fmt, Fmt.TR) : "—";

    static async Task<ReportSection> Health(DateTime start, DateTime end)
    {
        var recs = await Db.Between<HealthRecord>(start, end);
        var water = await Db.Between<WaterLog>(start, end);
        var s = new ReportSection { Title = "Sağlık" };
        s.Add("Kayıt sayısı", recs.Count.ToString());

        var w = recs.Where(r => r.Weight.HasValue).OrderBy(r => r.Date).ToList();
        if (w.Count > 0)
        {
            double first = w.First().Weight.Value, last = w.Last().Weight.Value;
            s.Add("Kilo (ilk → son)", $"{first.ToString("0.#", Fmt.TR)} → {last.ToString("0.#", Fmt.TR)} kg  ({(last - first >= 0 ? "+" : "")}{(last - first).ToString("0.#", Fmt.TR)})");
            s.Add("Ortalama kilo", Avg(w.Select(r => r.Weight.Value)) + " kg");
        }
        var bp = recs.Where(r => r.Systolic.HasValue && r.Diastolic.HasValue).ToList();
        if (bp.Count > 0)
            s.Add("Ortalama tansiyon", $"{Avg(bp.Select(r => (double)r.Systolic.Value), "0")}/{Avg(bp.Select(r => (double)r.Diastolic.Value), "0")}");
        s.Add("Ortalama nabız", Avg(recs.Where(r => r.Pulse.HasValue).Select(r => (double)r.Pulse.Value), "0"));
        s.Add("Ortalama SpO₂", "%" + Avg(recs.Where(r => r.SpO2.HasValue).Select(r => (double)r.SpO2.Value), "0"));
        s.Add("Ortalama uyku", Avg(recs.Where(r => r.SleepHours.HasValue).Select(r => r.SleepHours.Value)) + " saat");
        s.Add("Toplam egzersiz", $"{recs.Sum(r => r.ExerciseMin ?? 0)} dk");
        s.Add("Toplam su", $"{water.Sum(x => x.Ml) / 1000.0:0.0} litre");
        s.Add("Ortalama sağlık skoru", "%" + Stats.PeriodHealthScore(recs, water, start, end).ToString("0"));

        var steps = (await Db.Between<StepDay>(start, end)).Where(x => x.Steps > 0).ToList();
        int total = steps.Sum(x => x.Steps);
        s.Add("Toplam adım", total.ToString("N0", Fmt.TR));
        s.Add("Günlük ortalama adım", steps.Count > 0 ? (total / steps.Count).ToString("N0", Fmt.TR) : "—");
        s.Add("Yürünen mesafe", StepCounter.Km(total).ToString("0.0", Fmt.TR) + " km");
        s.Add("Adım hedefine ulaşılan gün", steps.Count(x => x.Steps >= AppSettings.StepGoal).ToString());
        return s;
    }

    static async Task<ReportSection> Finance(DateTime start, DateTime end)
    {
        var recs = await Db.Between<FinanceRecord>(start, end);
        decimal inc = recs.Where(r => r.IsIncome).Sum(r => r.Amount);
        decimal exp = recs.Where(r => !r.IsIncome).Sum(r => r.Amount);
        var s = new ReportSection { Title = "Finans", BarsTitle = "Gider kategorileri" };
        s.Add("Toplam gelir", Fmt.Money(inc));
        s.Add("Toplam gider", Fmt.Money(exp));
        s.Add("Net durum", Fmt.Money(inc - exp));
        s.Add("Tasarruf oranı", inc > 0 ? $"%{(inc - exp) / inc * 100:0}" : "—");
        s.Add("İşlem sayısı", recs.Count.ToString());

        foreach (var g in recs.Where(r => !r.IsIncome).GroupBy(r => r.Category ?? "Diğer")
                              .Select(g => new { g.Key, Sum = g.Sum(x => x.Amount) })
                              .OrderByDescending(x => x.Sum))
            s.Bars.Add(new ReportBar { Label = g.Key, Value = (double)g.Sum, ValueText = Fmt.Money(g.Sum) });
        return s;
    }

    static async Task<ReportSection> Tasks(DateTime start, DateTime end)
    {
        var recs = await Db.Between<TaskItem>(start, end);
        int done = recs.Count(t => t.Status == 2), prog = recs.Count(t => t.Status == 1), wait = recs.Count(t => t.Status == 0);
        var s = new ReportSection { Title = "Görevler", BarsTitle = "Durum dağılımı" };
        s.Add("Toplam görev", recs.Count.ToString());
        s.Add("Tamamlanan", done.ToString());
        s.Add("Devam eden", prog.ToString());
        s.Add("Bekleyen", wait.ToString());
        s.Add("Tamamlanma oranı", recs.Count > 0 ? $"%{done * 100.0 / recs.Count:0}" : "—");
        s.Add("Yüksek öncelikli", recs.Count(t => t.Priority == 2).ToString());
        if (recs.Count > 0)
        {
            s.Bars.Add(new ReportBar { Label = "Tamamlandı", Value = done, ValueText = done.ToString() });
            s.Bars.Add(new ReportBar { Label = "Devam Ediyor", Value = prog, ValueText = prog.ToString() });
            s.Bars.Add(new ReportBar { Label = "Bekliyor", Value = wait, ValueText = wait.ToString() });
        }
        return s;
    }

    static async Task<ReportSection> Nutrition(DateTime start, DateTime end)
    {
        var meals = await Db.Between<Meal>(start, end);
        var s = new ReportSection { Title = "Beslenme", BarsTitle = "Öğün dağılımı" };
        int days = meals.Select(m => m.Date.Date).Distinct().Count();
        int cal = meals.Sum(m => m.Calories ?? 0);
        s.Add("Kaydedilen öğün", meals.Count.ToString());
        s.Add("Kayıtlı gün", days.ToString());
        s.Add("Toplam kalori", $"{cal:N0} kcal");
        s.Add("Günlük ortalama kalori", days > 0 ? $"{cal / days:N0} kcal" : "—");
        foreach (var t in Meal.Types)
        {
            int c = meals.Count(m => m.MealType == t);
            if (c > 0) s.Bars.Add(new ReportBar { Label = t, Value = c, ValueText = c.ToString() });
        }
        return s;
    }

    static async Task<ReportSection> Modules()
    {
        var c = await Db.Get();
        var all = await c.Table<ModuleEntry>().ToListAsync();
        var s = new ReportSection { Title = "Eğitim · Gelişim · Sosyal · Kariyer", BarsTitle = "Ortalama ilerleme" };
        foreach (var (key, name) in new[] { ("education", "Eğitim"), ("development", "Kişisel Gelişim"), ("social", "Sosyal Yaşam"), ("career", "İş / Kariyer") })
        {
            var list = all.Where(e => e.Module == key).ToList();
            s.Add(name, $"{list.Count} kayıt, {list.Count(e => e.Status == 2)} tamamlandı");
            if (list.Count > 0)
            {
                double avg = list.Average(e => e.Progress);
                s.Bars.Add(new ReportBar { Label = name, Value = avg, ValueText = $"%{avg:0}" });
            }
        }
        return s;
    }
}
