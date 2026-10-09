using System.Collections.Concurrent;

namespace VITANEX.Services;

/// <summary>Şifalı bitki kaydı (veriler PlantData.g.cs içinde).</summary>
public record Plant(string Name, string Latin, string Emoji, string Cats, string Part, string Uses,
                    string HowTo, string Caution, string ImageUrl, string Author, string License, string FileName)
{
    public string CategoryText => string.Join(" · ", Cats.Select(c => PlantData.Categories[c].Name));
    public string CommonsUrl => "https://commons.wikimedia.org/wiki/File:" + FileName;
}

public static partial class PlantData
{
    public static int OfTheDay => (int)(DateTime.Today.Ticks / TimeSpan.TicksPerDay % All.Length);

    // ───────── Favoriler (Preferences) ─────────
    public static HashSet<int> Favorites
    {
        get
        {
            var s = Preferences.Default.Get("plant_favs", "");
            return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x, out var v) ? v : -1).Where(v => v >= 0).ToHashSet();
        }
    }

    public static bool ToggleFavorite(int i)
    {
        var f = Favorites;
        bool added = f.Add(i);
        if (!added) f.Remove(i);
        Preferences.Default.Set("plant_favs", string.Join(",", f));
        return added;
    }
}

/// <summary>
/// Bitki fotoğraflarını internetten bir kez indirir, cihazda saklar (sonra çevrimdışı da görünür).
/// Wikimedia kuralı gereği kendi User-Agent bilgimizi gönderiyoruz.
/// </summary>
public static class PlantImages
{
    static readonly HttpClient Http = CreateClient();
    static readonly SemaphoreSlim Gate = new(4, 4);
    static readonly ConcurrentDictionary<int, Task<string>> Running = new();

    static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("TROCKI-VITANEX/1.1 (Android; https://github.com/trocki59-byte/TROCKI-VITANEX)");
        return c;
    }

    static string Folder
    {
        get
        {
            var d = Path.Combine(FileSystem.CacheDirectory, "bitkiler");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    static string PathOf(int i) => Path.Combine(Folder, $"bitki_{i:00}.jpg");

    /// <summary>Yerel dosya yolu; indirilemezse null.</summary>
    public static Task<string> Get(int i)
    {
        var p = PathOf(i);
        if (File.Exists(p) && new FileInfo(p).Length > 0) return Task.FromResult(p);
        return Running.GetOrAdd(i, Download);
    }

    static async Task<string> Download(int i)
    {
        await Gate.WaitAsync();
        try
        {
            var p = PathOf(i);
            var bytes = await Http.GetByteArrayAsync(PlantData.All[i].ImageUrl);
            await File.WriteAllBytesAsync(p, bytes);
            return p;
        }
        catch
        {
            return null;
        }
        finally
        {
            Gate.Release();
            Running.TryRemove(i, out _);
        }
    }

    /// <summary>Fotoğraf kutusu: yüklenene kadar bitki simgesi görünür.</summary>
    public static View Box(int i, double width, double height, double radius)
    {
        var plant = PlantData.All[i];
        var grid = new Grid { WidthRequest = width, HeightRequest = height };
        var emoji = new Label
        {
            Text = plant.Emoji, FontSize = Math.Min(48, height * 0.45),
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
        };
        var img = new Image { Aspect = Aspect.AspectFill, Opacity = 0 };
        grid.Children.Add(emoji);
        grid.Children.Add(img);

        var border = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = AppColors.PrimaryLight,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = radius },
            Content = grid
        };
        if (width < 0) { grid.WidthRequest = -1; border.HorizontalOptions = LayoutOptions.Fill; }
        else border.WidthRequest = width;
        border.HeightRequest = height;

        _ = Load(i, img);
        return border;
    }

    static async Task Load(int i, Image img)
    {
        var path = await Get(i);
        if (path == null) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            img.Source = ImageSource.FromFile(path);
            img.Opacity = 1;
        });
    }
}
