namespace VITANEX.Services;

/// <summary>Ana sayfa manzarası: sabah Kapadokya, gündüz Uzungöl, akşam Abant, gece Meke Gölü (Wikimedia Commons).</summary>
public static class HeroImages
{
    public record Hero(int Index, int From, int To, string Url, string Location, string Credit);

    const string Base = "https://upload.wikimedia.org/wikipedia/commons/thumb/";

    public static readonly Hero[] All =
    {
        new(0, 5, 12, Base + "a/a7/Hot_air_balloon_ride_at_sunrise_in_Cappadocia.JPG/1280px-Hot_air_balloon_ride_at_sunrise_in_Cappadocia.JPG", "Kapadokya, Nevşehir", "Wolfgang Moroder · CC BY-SA 3.0"),
        new(1, 12, 18, Base + "b/ba/TRABZON-UZUNGOL.jpg/1280px-TRABZON-UZUNGOL.jpg", "Uzungöl, Trabzon", "Muhammed Raşit Kızılkaya · CC BY-SA 4.0"),
        new(2, 18, 21, Base + "0/0b/Abant_G%C3%B6l%C3%BC%2CG%C3%BCn_bat%C4%B1m%C4%B1.jpg/1280px-Abant_G%C3%B6l%C3%BC%2CG%C3%BCn_bat%C4%B1m%C4%B1.jpg", "Abant Gölü, Bolu", "Duyguceltikli · CC BY-SA 4.0"),
        new(3, 21, 29, Base + "6/6e/Lake_Meke_Under_The_Milky_Way.jpg/1280px-Lake_Meke_Under_The_Milky_Way.jpg", "Meke Gölü, Konya", "Banu Nazikcan · CC BY-SA 4.0"),
    };

    public static Hero For(int hour) =>
        All.FirstOrDefault(h => (hour >= h.From && hour < h.To) || (hour + 24 >= h.From && hour + 24 < h.To)) ?? All[1];

    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("TROCKI-VITANEX/1.2 (Android; https://github.com/trocki59-byte/TROCKI-VITANEX)");
        return c;
    }

    /// <summary>Fotoğrafı bir kez indirir, sonra cihazdan açar. İndirilemezse null (çizim manzara kalır).</summary>
    public static async Task<string> Get(Hero h)
    {
        try
        {
            var dir = Path.Combine(FileSystem.CacheDirectory, "manzara");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"manzara_{h.Index}.jpg");
            if (File.Exists(path) && new FileInfo(path).Length > 0) return path;
            var bytes = await Http.GetByteArrayAsync(h.Url);
            await File.WriteAllBytesAsync(path, bytes);
            return path;
        }
        catch { return null; }
    }
}
