using VITANEX.Models;

namespace VITANEX.Services;

/// <summary>Android 10+ için "Fiziksel etkinlik" (ACTIVITY_RECOGNITION) izni.</summary>
public class ActivityPermission : Permissions.BasePlatformPermission
{
#if ANDROID
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new[] { ("android.permission.ACTIVITY_RECOGNITION", true) };
#endif
}

/// <summary>
/// Yürüme sayar. Telefonun donanım adım sayacını (TYPE_STEP_COUNTER) kullanır:
/// sensör uygulama kapalıyken de saymaya devam eder, uygulama açılınca aradaki adımlar eklenir.
/// </summary>
public static class StepCounter
{
    public static event Action Changed;

    static readonly SemaphoreSlim Gate = new(1, 1);

    public static bool Enabled
    {
        get => Preferences.Default.Get("steps_enabled", false);
        set => Preferences.Default.Set("steps_enabled", value);
    }

    public static bool Running { get; private set; }

    // ───────── Hesaplar ─────────
    public static double Km(int steps) => steps * AppSettings.StrideCm / 100000.0;

    public static async Task<int> Kcal(int steps)
    {
        double kg = 70;
        try
        {
            var c = await Db.Get();
            var last = await c.Table<HealthRecord>().Where(h => h.Weight != null).OrderByDescending(h => h.Date).FirstOrDefaultAsync();
            if (last?.Weight != null) kg = last.Weight.Value;
        }
        catch { }
        return (int)Math.Round(steps * 0.04 * kg / 70.0);
    }

    // ───────── Veri ─────────
    public static async Task<StepDay> Day(DateTime d)
    {
        var day = d.Date;
        var c = await Db.Get();
        return await c.Table<StepDay>().Where(x => x.Date == day).FirstOrDefaultAsync() ?? new StepDay { Date = day };
    }

    public static async Task<int> Today() => (await Day(DateTime.Today)).Steps;

    public static async Task SetDay(DateTime d, int steps)
    {
        await Gate.WaitAsync();
        try
        {
            var r = await Day(d);
            r.Steps = Math.Max(0, steps);
            await Db.Save(r);
        }
        finally { Gate.Release(); }
        Changed?.Invoke();
    }

    /// <summary>Son günlerin adımları (eski → yeni).</summary>
    public static async Task<List<(DateTime Day, int Steps)>> LastDays(int n)
    {
        var start = DateTime.Today.AddDays(-(n - 1));
        var rows = await Db.Between<StepDay>(start, DateTime.Today.AddDays(1));
        return Enumerable.Range(0, n).Select(i =>
        {
            var d = start.AddDays(i);
            return (d, rows.Where(r => r.Date.Date == d).Sum(r => r.Steps));
        }).ToList();
    }

    // ───────── Sensör ─────────
    public static bool Supported
    {
        get
        {
#if ANDROID
            return Manager?.GetDefaultSensor(Android.Hardware.SensorType.StepCounter) != null;
#else
            return false;
#endif
        }
    }

    /// <summary>İzni ister ve sayımı başlatır. Başarılıysa true.</summary>
    public static async Task<bool> Enable()
    {
        if (!Supported) return false;
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            var st = await Permissions.CheckStatusAsync<ActivityPermission>();
            if (st != PermissionStatus.Granted) st = await Permissions.RequestAsync<ActivityPermission>();
            if (st != PermissionStatus.Granted) return false;
        }
        Enabled = true;
        Start();
        return true;
    }

    /// <summary>Uygulama açılınca / öne gelince çağrılır.</summary>
    public static async Task Resume()
    {
        if (!Enabled || !Supported) return;
        if (OperatingSystem.IsAndroidVersionAtLeast(29) &&
            await Permissions.CheckStatusAsync<ActivityPermission>() != PermissionStatus.Granted) return;
        Start();
    }

    public static void Start()
    {
#if ANDROID
        if (Running) return;
        var sensor = Manager?.GetDefaultSensor(Android.Hardware.SensorType.StepCounter);
        if (sensor == null) return;
        _listener ??= new StepSensorListener();
        Running = Manager.RegisterListener(_listener, sensor, Android.Hardware.SensorDelay.Ui);
#endif
    }

    public static void Stop()
    {
#if ANDROID
        if (!Running) return;
        Manager?.UnregisterListener(_listener);
        Running = false;
#endif
    }

    public static void Disable()
    {
        Stop();
        Enabled = false;
        Preferences.Default.Remove("step_last_total");
        Changed?.Invoke();
    }

    /// <summary>Sensörün açılıştan beri toplam değerini işler, farkı bugüne ekler.</summary>
    static async Task Report(long total)
    {
        await Gate.WaitAsync();
        try
        {
            long last = Preferences.Default.Get("step_last_total", -1L);
            Preferences.Default.Set("step_last_total", total);
            if (last < 0) return;                                  // ilk okuma: başlangıç noktası
            long delta = total >= last ? total - last : total;     // telefon yeniden başlatıldıysa sayaç sıfırlanır
            if (delta <= 0) return;
            if (delta > 60000) delta = 60000;

            var r = await Day(DateTime.Today);
            r.Steps += (int)delta;
            await Db.Save(r);
        }
        catch { }
        finally { Gate.Release(); }
        MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke());
    }

#if ANDROID
    static StepSensorListener _listener;

    static Android.Hardware.SensorManager Manager =>
        Android.App.Application.Context.GetSystemService(Android.Content.Context.SensorService) as Android.Hardware.SensorManager;

    internal static void OnSensorTotal(long total) => _ = Report(total);
#endif
}

#if ANDROID
/// <summary>Android adım sensörü dinleyicisi.</summary>
public class StepSensorListener : Java.Lang.Object, Android.Hardware.ISensorEventListener
{
    public void OnAccuracyChanged(Android.Hardware.Sensor sensor, Android.Hardware.SensorStatus accuracy) { }

    public void OnSensorChanged(Android.Hardware.SensorEvent e)
    {
        if (e?.Values == null || e.Values.Count == 0) return;
        StepCounter.OnSensorTotal((long)e.Values[0]);
    }
}
#endif
