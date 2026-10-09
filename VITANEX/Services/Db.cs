using SQLite;
using VITANEX.Models;

namespace VITANEX.Services;

/// <summary>Yerel SQLite veritabanı. Tüm veriler cihazda saklanır.</summary>
public static class Db
{
    public const string FileName = "vitanex.db3";
    static SQLiteAsyncConnection _conn;
    static readonly SemaphoreSlim _lock = new(1, 1);

    public static string DbPath => Path.Combine(FileSystem.AppDataDirectory, FileName);

    public static async Task<SQLiteAsyncConnection> Get()
    {
        if (_conn != null) return _conn;
        await _lock.WaitAsync();
        try
        {
            if (_conn == null)
            {
                var c = new SQLiteAsyncConnection(DbPath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

                await c.CreateTablesAsync(CreateFlags.None,
                    typeof(HealthRecord), typeof(WaterLog), typeof(Medication), typeof(Meal),
                    typeof(FinanceRecord), typeof(TaskItem), typeof(ModuleEntry), typeof(Reminder), typeof(StepDay));

                if (await c.Table<Reminder>().CountAsync() == 0)
                    await c.InsertAllAsync(DefaultReminders());

                _conn = c;
            }
        }
        finally
        {
            _lock.Release();
        }
        return _conn;
    }

    static IEnumerable<Reminder> DefaultReminders() => new[]
    {
        new Reminder { Time = "08:00", Icon = "💧", Title = "Su iç (200 ml)" },
        new Reminder { Time = "09:00", Icon = "🚶", Title = "Sabah yürüyüşü (30 dk)" },
        new Reminder { Time = "12:00", Icon = "🍽️", Title = "Öğle yemeği" },
        new Reminder { Time = "15:00", Icon = "📖", Title = "Kitap oku (15 dk)" },
    };

    public static async Task Save<T>(T item) where T : IEntity, new()
    {
        var c = await Get();
        if (item.Id == 0) await c.InsertAsync(item);
        else await c.UpdateAsync(item);
    }

    public static async Task Delete<T>(T item) where T : IEntity, new()
    {
        var c = await Get();
        await c.DeleteAsync(item);
    }

    public static async Task<List<T>> Between<T>(DateTime start, DateTime end) where T : new()
    {
        // Date alanı olan tablolar için ortak tarih aralığı sorgusu
        var c = await Get();
        var map = c.GetConnection().GetMapping<T>();
        return await c.QueryAsync<T>(
            $"SELECT * FROM \"{map.TableName}\" WHERE Date >= ? AND Date < ? ORDER BY Date",
            start.Ticks, end.Ticks);
    }

    /// <summary>Bağlantıyı kapatır (yedekleme / geri yükleme öncesi).</summary>
    public static async Task Close()
    {
        await _lock.WaitAsync();
        try
        {
            if (_conn != null)
            {
                await _conn.CloseAsync();
                _conn = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
