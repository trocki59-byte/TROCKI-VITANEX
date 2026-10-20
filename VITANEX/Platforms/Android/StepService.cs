using global::Android.App;
using global::Android.Content;
using global::Android.Content.PM;
using global::Android.Hardware;
using global::Android.OS;
using VITANEX.Services;

namespace VITANEX.Droid;

/// <summary>
/// Yürüme sayar arka plan hizmeti: ekran kilitliyken, telefon cepteyken ve uygulama kapalıyken de
/// adımları canlı sayar; bildirim alanında "👣 Bugün 3.245 adım" olarak gösterir.
/// </summary>
[Service(Name = "com.trocki.vitanex.StepService", Exported = false, ForegroundServiceType = ForegroundService.TypeHealth)]
public class StepService : Service, ISensorEventListener
{
    public const string ChannelId = "vitanex_adim";
    const int NotifId = 77;

    SensorManager _sm;
    long _lastUi;

    public static bool IsRunning { get; private set; }

    /// <summary>Hizmeti başlatır (izin verilmişse).</summary>
    public static bool Begin(Context ctx = null)
    {
        try
        {
            ctx ??= global::Android.App.Application.Context;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q &&
                ctx.CheckSelfPermission("android.permission.ACTIVITY_RECOGNITION") != Permission.Granted)
                return false;
            var i = new Intent(ctx, typeof(StepService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) ctx.StartForegroundService(i);
            else ctx.StartService(i);
            return true;
        }
        catch { return false; }
    }

    public static void End(Context ctx = null)
    {
        try
        {
            ctx ??= global::Android.App.Application.Context;
            ctx.StopService(new Intent(ctx, typeof(StepService)));
        }
        catch { }
    }

    public override IBinder OnBind(Intent intent) => null;

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        EnsureChannel(this);
        var n = BuildNotification(this, "👣 Adımlarınız sayılıyor", "Ekran kilitliyken de sayılır");
        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
                StartForeground(NotifId, n, ForegroundService.TypeHealth);
            else
                StartForeground(NotifId, n);
        }
        catch
        {
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        if (!IsRunning)
        {
            _sm = (SensorManager)GetSystemService(SensorService);
            var sensor = _sm?.GetDefaultSensor(SensorType.StepCounter);
            if (sensor == null) { StopSelf(); return StartCommandResult.NotSticky; }
            _sm.RegisterListener(this, sensor, SensorDelay.Normal);
            IsRunning = true;
        }
        _ = Refresh();
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        try { _sm?.UnregisterListener(this); } catch { }
        IsRunning = false;
        base.OnDestroy();
    }

    public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) { }

    public void OnSensorChanged(SensorEvent e)
    {
        if (e?.Values == null || e.Values.Count == 0) return;
        _ = Handle((long)e.Values[0]);
    }

    async Task Handle(long total)
    {
        await StepCounter.Report(total);
        long now = Java.Lang.JavaSystem.CurrentTimeMillis();
        if (now - _lastUi < 3000) return;          // bildirimi en fazla 3 sn'de bir güncelle
        _lastUi = now;
        await Refresh();
    }

    async Task Refresh()
    {
        try
        {
            int n = await StepCounter.Today();
            int goal = Math.Max(1, AppSettings.StepGoal);
            string title = $"👣 Bugün {n.ToString("N0", Fmt.TR)} adım";
            string text = $"Hedef {goal.ToString("N0", Fmt.TR)} · %{Math.Min(999, n * 100 / goal)} · {StepCounter.Km(n).ToString("0.00", Fmt.TR)} km";
            var nm = (NotificationManager)GetSystemService(NotificationService);
            nm.Notify(NotifId, BuildNotification(this, title, text));
        }
        catch { }
    }

    static void EnsureChannel(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var nm = (NotificationManager)ctx.GetSystemService(NotificationService);
        if (nm.GetNotificationChannel(ChannelId) != null) return;
        var ch = new NotificationChannel(ChannelId, "Yürüme sayar", NotificationImportance.Low)
        {
            Description = "Günlük adım sayısını gösterir (sessiz)"
        };
        ch.SetShowBadge(false);
        nm.CreateNotificationChannel(ch);
    }

    static Notification BuildNotification(Context ctx, string title, string text)
    {
        var open = ctx.PackageManager.GetLaunchIntentForPackage(ctx.PackageName);
        open?.SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
        var tap = open == null ? null : PendingIntent.GetActivity(ctx, NotifId, open, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

#pragma warning disable CA1422, CS0618
        Notification.Builder b = Build.VERSION.SdkInt >= BuildVersionCodes.O
            ? new Notification.Builder(ctx, ChannelId)
            : new Notification.Builder(ctx);
        b.SetContentTitle(title)
         .SetContentText(text)
         .SetSmallIcon(ctx.ApplicationInfo.Icon)
         .SetOngoing(true)
         .SetOnlyAlertOnce(true)
         .SetShowWhen(false);
        if (tap != null) b.SetContentIntent(tap);
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) b.SetPriority((int)NotificationPriority.Low);
#pragma warning restore CA1422, CS0618
        return b.Build();
    }
}
