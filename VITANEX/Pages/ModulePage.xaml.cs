using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

/// <summary>Eğitim, Kişisel Gelişim, Sosyal Yaşam ve İş/Kariyer modülleri için ortak ekran.</summary>
public partial class ModulePage : ContentPage
{
    public record ModuleInfo(string Key, string Title, string Icon, string Description, string[] Kinds, bool DateFirst);

    public static readonly Dictionary<string, ModuleInfo> Infos = new()
    {
        ["education"] = new("education", "Eğitim", "📘",
            "Kitaplar, kurslar, araştırmalar ve öğrenme hedefleri.",
            new[] { "Kitap", "Kurs", "Araştırma", "Çalışma Konusu", "Öğrenme Hedefi" }, false),
        ["development"] = new("development", "Kişisel Gelişim", "🌱",
            "Alışkanlıklar, günlük hedefler, motivasyon notları ve kişisel projeler.",
            new[] { "Alışkanlık", "Günlük Hedef", "Motivasyon Notu", "Kişisel Proje" }, false),
        ["social"] = new("social", "Sosyal Yaşam", "👥",
            "Görüşmeler, etkinlikler, seyahatler ve sosyal planlar.",
            new[] { "Görüşme", "Etkinlik", "Seyahat", "Sosyal Plan" }, true),
        ["career"] = new("career", "İş / Kariyer", "💼",
            "Projeler, yapılacak işler, fikirler ve kariyer hedefleri.",
            new[] { "Proje", "Yapılacak İş", "Fikir", "Kariyer Hedefi" }, false),
    };

    readonly ModuleInfo _info;
    string _kind = "Tümü";

    public ModulePage(string key)
    {
        InitializeComponent();
        _info = Infos[key];
        Title = _info.Title;
        IconLabel.Text = _info.Icon;
        DescLabel.Text = _info.Description;

        foreach (var k in new[] { "Tümü" }.Concat(_info.Kinds))
        {
            var b = new Button { Text = k, Style = Ui.Style("ChipButton") };
            b.Clicked += async (s, e) => { _kind = k; await LoadAsync(); };
            Chips.Children.Add(b);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        foreach (var b in Chips.Children.OfType<Button>())
            Ui.SetChip(b, b.Text == _kind);

        var c = await Db.Get();
        var key = _info.Key;
        var all = await c.Table<ModuleEntry>().Where(e => e.Module == key).ToListAsync();

        int done = all.Count(e => e.Status == 2);
        double avg = all.Count == 0 ? 0 : all.Average(e => e.Progress);
        StatsLabel.Text = $"{all.Count} kayıt  ·  {done} tamamlandı  ·  ort. %{avg:0}";
        AvgBar.Progress = avg / 100.0;

        var list = _kind == "Tümü" ? all : all.Where(e => e.Kind == _kind).ToList();
        List<ModuleEntry> sorted;
        if (_info.DateFirst)
        {
            // Sosyal yaşam: yaklaşan etkinlikler önce, geçmişler sonra
            var now = DateTime.Now;
            sorted = list.OrderBy(e => e.Status == 2)
                         .ThenBy(e => e.Date.HasValue && e.Date.Value < now)
                         .ThenBy(e => e.Date ?? DateTime.MaxValue).ToList();
        }
        else
        {
            sorted = list.OrderBy(e => e.Status == 2).ThenByDescending(e => e.CreatedAt).ToList();
        }

        BindableLayout.SetItemsSource(EntryList, sorted);
        EmptyLabel.IsVisible = sorted.Count == 0;
        EmptyLabel.Text = "Henüz kayıt yok. Sağ alttaki + ile ekleyin.";
    }

    async void OnAdd(object s, EventArgs e) =>
        await Nav.Push(new ModuleEntryPage(_info, null, _kind == "Tümü" ? null : _kind));

    async void OnEntryTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not ModuleEntry m) return;
        var choice = await DisplayActionSheetAsync(m.Title, "Vazgeç", "Sil",
            "İlerlemeyi güncelle", "+10 ilerleme", "✓ Tamamlandı", "Düzenle");
        switch (choice)
        {
            case "İlerlemeyi güncelle":
                var txt = await DisplayPromptAsync("İlerleme", "Yüzde kaç tamamlandı? (0-100)", "Kaydet", "Vazgeç",
                    initialValue: m.Progress.ToString(), keyboard: Keyboard.Numeric);
                var v = Fmt.ParseInt(txt);
                if (v.HasValue) await SetProgress(m, v.Value);
                break;
            case "+10 ilerleme": await SetProgress(m, m.Progress + 10); break;
            case "✓ Tamamlandı": await SetProgress(m, 100); break;
            case "Düzenle": await Nav.Push(new ModuleEntryPage(_info, m)); break;
            case "Sil":
                if (await Ui.Confirm("Sil", $"\"{m.Title}\" silinsin mi?"))
                {
                    await Db.Delete(m);
                    await LoadAsync();
                }
                break;
        }
    }

    async Task SetProgress(ModuleEntry m, int p)
    {
        m.Progress = Math.Clamp(p, 0, 100);
        m.Status = m.Progress >= 100 ? 2 : m.Progress > 0 ? 1 : 0;
        await Db.Save(m);
        await LoadAsync();
    }
}
