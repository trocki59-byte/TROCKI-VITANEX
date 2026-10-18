using System.Text;
using global::Android.App;
using global::Android.Content;
using global::Android.OS;
using VITANEX.Services;

namespace VITANEX.Droid;

/// <summary>
/// Ödeme bildirimleri: AlarmManager ile belirlenen saatte bildirim gösterir.
/// Uygulama kapalıyken de çalışır; telefon yeniden başlatılınca alarmlar yeniden kurulur.
/// </summary>
public static class PaymentNotifier
{
    public const string ChannelId = "vitanex_hatirlatmalar";
    const string Store = "vitanex_alarms";
    const string KeyList = "list";

    class Saved
    {
        public int Id { get; set; }
        public long When { get; set; }      // Unix ms (UTC)
        public string Title { get; set; }
        public string Body { get; set; }
    }

    static Context Ctx => global::Android.App.Application.Context;

    public static void EnsureChannel(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var nm = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
        if (nm.GetNotificationChannel(ChannelId) != null) return;
        var ch = new NotificationChannel(ChannelId, "VİTANEX hatırlatmaları", NotificationImportance.High)
        {
            Description = "Ödeme günleri (kredi, kart, KMH, kira, aidat) ve öğün saatleri için hatırlatma"
        };
        nm.CreateNotificationChannel(ch);
    }

    /// <summary>Eski alarmları iptal eder, verilen listeyi kurar ve saklar.</summary>
    public static void Schedule(List<PaymentAlarm> alarms)
    {
        var ctx = Ctx;
        EnsureChannel(ctx);
        foreach (var old in Load(ctx)) Cancel(ctx, old.Id);

        var saved = alarms.Select(a => new Saved
        {
            Id = a.Id,
            When = new DateTimeOffset(a.When).ToUnixTimeMilliseconds(),
            Title = a.Title,
            Body = a.Body
        }).ToList();

        foreach (var s in saved) Set(ctx, s);
        var prefs = ctx.GetSharedPreferences(Store, FileCreationMode.Private);
        prefs.Edit().PutString(KeyList, Encode(saved)).Apply();
    }

    /// <summary>Telefon yeniden başlayınca saklanan alarmları geri kurar.</summary>
    public static void Restore(Context ctx)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var s in Load(ctx).Where(x => x.When > now)) Set(ctx, s);
    }

    // Basit saklama biçimi: her satır "id|zaman|başlık(base64)|metin(base64)"
    static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? ""));
    static string UnB64(string s) => Encoding.UTF8.GetString(Convert.FromBase64String(s));

    static string Encode(List<Saved> list) =>
        string.Join("\n", list.Select(x => $"{x.Id}|{x.When}|{B64(x.Title)}|{B64(x.Body)}"));

    static List<Saved> Load(Context ctx)
    {
        var result = new List<Saved>();
        try
        {
            var text = ctx.GetSharedPreferences(Store, FileCreationMode.Private).GetString(KeyList, null);
            if (string.IsNullOrEmpty(text)) return result;
            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var p = line.Split('|');
                if (p.Length < 4) continue;
                result.Add(new Saved { Id = int.Parse(p[0]), When = long.Parse(p[1]), Title = UnB64(p[2]), Body = UnB64(p[3]) });
            }
        }
        catch { }
        return result;
    }

    static PendingIntent Pending(Context ctx, int id, string title, string body)
    {
        var intent = new Intent(ctx, typeof(PaymentAlarmReceiver));
        intent.PutExtra("id", id);
        intent.PutExtra("title", title ?? "");
        intent.PutExtra("body", body ?? "");
        return PendingIntent.GetBroadcast(ctx, id, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
    }

    static void Set(Context ctx, Saved s)
    {
        var am = (AlarmManager)ctx.GetSystemService(Context.AlarmService);
        var pi = Pending(ctx, s.Id, s.Title, s.Body);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            am.SetAndAllowWhileIdle(AlarmType.RtcWakeup, s.When, pi);
        else
            am.Set(AlarmType.RtcWakeup, s.When, pi);
    }

    static void Cancel(Context ctx, int id)
    {
        var am = (AlarmManager)ctx.GetSystemService(Context.AlarmService);
        var pi = Pending(ctx, id, null, null);
        am.Cancel(pi);
        pi.Cancel();
    }

    /// <summary>Bildirimi gösterir; dokununca uygulama açılır.</summary>
    public static void Show(Context ctx, int id, string title, string body)
    {
        EnsureChannel(ctx);
        var open = ctx.PackageManager.GetLaunchIntentForPackage(ctx.PackageName);
        open?.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        var tap = open == null ? null : PendingIntent.GetActivity(ctx, id, open, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

#pragma warning disable CA1422, CS0618
        Notification.Builder b = Build.VERSION.SdkInt >= BuildVersionCodes.O
            ? new Notification.Builder(ctx, ChannelId)
            : new Notification.Builder(ctx);
        b.SetContentTitle(title)
         .SetContentText(body)
         .SetStyle(new Notification.BigTextStyle().BigText(body))
         .SetSmallIcon(ctx.ApplicationInfo.Icon)
         .SetAutoCancel(true);
        if (tap != null) b.SetContentIntent(tap);
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) b.SetPriority((int)NotificationPriority.High);
#pragma warning restore CA1422, CS0618

        var nm = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
        nm.Notify(id, b.Build());
    }
}

/// <summary>Alarm saati gelince bildirimi gösterir.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public class PaymentAlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        if (intent == null) return;
        PaymentNotifier.Show(context, intent.GetIntExtra("id", 1), intent.GetStringExtra("title"), intent.GetStringExtra("body"));
    }
}

/// <summary>Telefon açılınca ödeme alarmlarını yeniden kurar.</summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
public class PaymentBootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        if (intent?.Action == Intent.ActionBootCompleted) PaymentNotifier.Restore(context);
    }
}
