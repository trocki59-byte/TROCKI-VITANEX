using VITANEX.Models;

namespace VITANEX.Services;

/// <summary>İlaç bildirimleri: aktif her ilaç için yazılan saatlerde, önümüzdeki 14 gün.</summary>
public static class Medications
{
    public static async Task<List<PaymentAlarm>> Alarms()
    {
        var list = new List<PaymentAlarm>();
        try
        {
            var c = await Db.Get();
            var meds = await c.Table<Medication>().Where(m => m.Active).ToListAsync();
            var now = DateTime.Now;
            foreach (var m in meds)
            {
                var times = (m.Times ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => TimeSpan.TryParse(t, out var ts) ? ts : (TimeSpan?)null)
                    .Where(t => t.HasValue).Select(t => t.Value).Distinct().Take(9).ToList();
                for (int d = 0; d < 14; d++)
                {
                    var day = DateTime.Today.AddDays(d);
                    for (int k = 0; k < times.Count; k++)
                    {
                        var when = day + times[k];
                        if (when <= now) continue;
                        string dose = string.IsNullOrWhiteSpace(m.Dose) ? "" : $" — {m.Dose}";
                        list.Add(new PaymentAlarm(1_000_000 + (m.Id % 2000) * 1000 + d * 10 + k, when,
                            "💊 İlaç zamanı", $"{m.Name}{dose} ({times[k]:hh\\:mm})"));
                    }
                }
            }
        }
        catch { }
        return list;
    }
}
