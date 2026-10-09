using VITANEX.Models;

namespace VITANEX.Services;

public enum Period { Daily, Weekly, Monthly, Yearly }

public record Bucket(string Label, DateTime Start, DateTime End);

public static class Stats
{
    /// <summary>
    /// Günlük sağlık skoru (0-100): su hedefi, uyku hedefi ve egzersiz hedefinin ortalaması.
    /// </summary>
    public static double HealthScore(IEnumerable<HealthRecord> dayRecords, int waterMl)
    {
        var list = dayRecords.ToList();
        double water = Math.Min(1, waterMl / (double)Math.Max(1, AppSettings.WaterGoal));
        double sleepH = list.Where(r => r.SleepHours.HasValue).Select(r => r.SleepHours.Value).DefaultIfEmpty(0).Max();
        double sleep = Math.Min(1, sleepH / Math.Max(0.5, AppSettings.SleepGoal));
        double ex = Math.Min(1, list.Sum(r => r.ExerciseMin ?? 0) / (double)Math.Max(1, AppSettings.ExerciseGoal));
        return Math.Round((water + sleep + ex) / 3 * 100);
    }

    /// <summary>Bir dönemde veri girilen günlerin ortalama sağlık skoru.</summary>
    public static double PeriodHealthScore(List<HealthRecord> recs, List<WaterLog> water, DateTime start, DateTime end)
    {
        var days = recs.Select(r => r.Date.Date)
                       .Concat(water.Select(w => w.Date.Date))
                       .Where(d => d >= start && d < end)
                       .Distinct()
                       .ToList();
        if (days.Count == 0) return 0;
        return Math.Round(days.Average(d =>
            HealthScore(recs.Where(r => r.Date.Date == d), water.Where(w => w.Date.Date == d).Sum(w => w.Ml))));
    }

    public static string PeriodName(Period p) => p switch
    {
        Period.Daily => "Son 7 gün",
        Period.Weekly => "Son 8 hafta",
        Period.Monthly => "Son 12 ay",
        _ => "Son 5 yıl"
    };

    public static List<Bucket> Buckets(Period p)
    {
        var today = DateTime.Today;
        var list = new List<Bucket>();
        switch (p)
        {
            case Period.Daily:
                for (int i = 6; i >= 0; i--)
                {
                    var d = today.AddDays(-i);
                    list.Add(new Bucket(d.ToString("ddd", Fmt.TR), d, d.AddDays(1)));
                }
                break;
            case Period.Weekly:
                var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                for (int i = 7; i >= 0; i--)
                {
                    var s = monday.AddDays(-7 * i);
                    list.Add(new Bucket(s.ToString("dd.MM"), s, s.AddDays(7)));
                }
                break;
            case Period.Monthly:
                var m = new DateTime(today.Year, today.Month, 1);
                for (int i = 11; i >= 0; i--)
                {
                    var s = m.AddMonths(-i);
                    list.Add(new Bucket(s.ToString("MMM", Fmt.TR), s, s.AddMonths(1)));
                }
                break;
            default:
                for (int i = 4; i >= 0; i--)
                {
                    var s = new DateTime(today.Year - i, 1, 1);
                    list.Add(new Bucket(s.Year.ToString(), s, s.AddYears(1)));
                }
                break;
        }
        return list;
    }
}
