using SQLite;
using VITANEX.Services;

namespace VITANEX.Models;

public interface IEntity
{
    int Id { get; set; }
}

// ───────────── SAĞLIK ─────────────
public class HealthRecord : IEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public DateTime Date { get; set; }
    public double? Weight { get; set; }
    public int? Systolic { get; set; }
    public int? Diastolic { get; set; }
    public int? Pulse { get; set; }
    public int? SpO2 { get; set; }
    public double? SleepHours { get; set; }
    public int? ExerciseMin { get; set; }
    public string Note { get; set; }

    [Ignore] public string DateText => Date.ToString("dd.MM.yyyy  HH:mm", Fmt.TR);

    [Ignore]
    public string Summary
    {
        get
        {
            var p = new List<string>();
            if (Weight.HasValue) p.Add($"{Weight.Value.ToString("0.#", Fmt.TR)} kg");
            if (Systolic.HasValue && Diastolic.HasValue) p.Add($"Tansiyon {Systolic}/{Diastolic}");
            if (Pulse.HasValue) p.Add($"Nabız {Pulse}");
            if (SpO2.HasValue) p.Add($"SpO₂ %{SpO2}");
            if (SleepHours.HasValue) p.Add($"Uyku {SleepHours.Value.ToString("0.#", Fmt.TR)} sa");
            if (ExerciseMin.HasValue) p.Add($"Egzersiz {ExerciseMin} dk");
            return p.Count == 0 ? "—" : string.Join("  ·  ", p);
        }
    }

    [Ignore] public bool HasNote => !string.IsNullOrWhiteSpace(Note);
}

public class WaterLog : IEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public DateTime Date { get; set; }
    public int Ml { get; set; }
}

public class Medication : IEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; }
    public string Dose { get; set; }
    public string Times { get; set; }   // "08:00, 20:00"
    public bool Active { get; set; } = true;

    [Ignore] public string Info => $"{Dose}  ·  {Times}";
    [Ignore] public string StateText => Active ? "Aktif" : "Pasif";
    [Ignore] public Color StateColor => Active ? AppColors.Green : AppColors.Muted;
}

// ───────────── BESLENME ─────────────
public class Meal : IEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public DateTime Date { get; set; }
    public string MealType { get; set; }     // Kahvaltı, Öğle, Akşam, Ara Öğün
    public string Description { get; set; }
    public int? Calories { get; set; }
    public string Note { get; set; }

    public static readonly string[] Types = { "Kahvaltı", "Öğle Yemeği", "Akşam Yemeği", "Ara Öğün" };

    [Ignore]
    public string Icon => MealType switch
    {
        "Kahvaltı" => "🍳",
        "Öğle Yemeği" => "🥗",
        "Akşam Yemeği" => "🍲",
        _ => "🍎"
    };

    [Ignore] public int Order => Array.IndexOf(Types, MealType) < 0 ? 9 : Array.IndexOf(Types, MealType);
    [Ignore] public string CaloriesText => Calories.HasValue ? $"{Calories} kcal" : "";
    [Ignore] public string TimeText => Date.ToString("HH:mm");
}

// ───────────── FİNANS ─────────────
public class FinanceRecord : IEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public bool IsIncome { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    [Indexed] public DateTime Date { get; set; }
    public string Category { get; set; }

    public static readonly string[] IncomeCategories =
        { "Maaş", "Emekli Maaşı", "Ek Gelir", "Kira Geliri", "Satış", "Faiz / Yatırım", "Diğer" };

    public static readonly string[] ExpenseCategories =
        { "Market", "Faturalar", "Kira", "Ulaşım", "Sağlık", "Eğitim", "Giyim", "Yemek", "Eğlence", "Ev / Bakım", "Diğer" };

    [Ignore] public string AmountText => (IsIncome ? "+ " : "− ") + Fmt.Money(Amount);
    [Ignore] public Color AmountColor => IsIncome ? AppColors.Green : AppColors.Red;
    [Ignore] public string Icon => IsIncome ? "⬆️" : "⬇️";
    [Ignore] public string SubText => $"{Category}  ·  {Date.ToString("dd.MM.yyyy", Fmt.TR)}";
}

// ───────────── GÖREVLER ─────────────
public class TaskItem : IEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Title { get; set; }
    public string Note { get; set; }
    [Indexed] public DateTime Date { get; set; }   // sadece gün
    public string Time { get; set; }               // "HH:mm" veya boş
    public int Priority { get; set; } = 1;         // 0 Düşük, 1 Orta, 2 Yüksek
    public int Status { get; set; }                // 0 Bekliyor, 1 Devam Ediyor, 2 Tamamlandı
    public DateTime? CompletedAt { get; set; }

    public static readonly string[] StatusNames = { "Bekliyor", "Devam Ediyor", "Tamamlandı" };
    public static readonly string[] PriorityNames = { "Düşük", "Orta", "Yüksek" };

    [Ignore] public string StatusText => StatusNames[Math.Clamp(Status, 0, 2)];
    [Ignore] public string PriorityText => PriorityNames[Math.Clamp(Priority, 0, 2)];
    [Ignore] public Color StatusColor => Status switch { 2 => AppColors.Green, 1 => AppColors.Accent, _ => AppColors.Orange };
    [Ignore] public Brush StatusBrush => new SolidColorBrush(StatusColor);
    [Ignore] public Color PriorityColor => Priority switch { 2 => AppColors.Red, 1 => AppColors.Orange, _ => AppColors.Muted };
    [Ignore] public string StatusIcon => Status switch { 2 => "✓", 1 => "▶", _ => "" };
    [Ignore] public TextDecorations TitleDecoration => Status == 2 ? TextDecorations.Strikethrough : TextDecorations.None;
    [Ignore]
    public string WhenText
    {
        get
        {
            string d = Date.Date == DateTime.Today ? "Bugün"
                     : Date.Date == DateTime.Today.AddDays(1) ? "Yarın"
                     : Date.ToString("dd MMM yyyy", Fmt.TR);
            return string.IsNullOrEmpty(Time) ? d : $"{d}  {Time}";
        }
    }
}

// ───────────── EĞİTİM / GELİŞİM / SOSYAL / KARİYER ─────────────
public class ModuleEntry : IEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public string Module { get; set; }     // education, development, social, career
    public string Kind { get; set; }
    public string Title { get; set; }
    public string Note { get; set; }
    public DateTime? Date { get; set; }
    public int Progress { get; set; }                // 0-100
    public int Status { get; set; }                  // 0 Planlandı, 1 Devam Ediyor, 2 Tamamlandı
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public static readonly string[] StatusNames = { "Planlandı", "Devam Ediyor", "Tamamlandı" };

    [Ignore] public double ProgressValue => Math.Clamp(Progress, 0, 100) / 100.0;
    [Ignore] public string ProgressText => $"%{Progress}";
    [Ignore] public string StatusText => StatusNames[Math.Clamp(Status, 0, 2)];
    [Ignore] public Color StatusColor => Status switch { 2 => AppColors.Green, 1 => AppColors.Accent, _ => AppColors.Orange };
    [Ignore] public string DateText => Date.HasValue ? Date.Value.ToString("dd MMM yyyy  HH:mm", Fmt.TR) : "";
    [Ignore] public bool HasDate => Date.HasValue;
    [Ignore] public bool HasNote => !string.IsNullOrWhiteSpace(Note);
}

// ───────────── HATIRLATICILAR ─────────────
public class Reminder : IEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Time { get; set; }   // "HH:mm"
    public string Title { get; set; }
    public string Icon { get; set; }
    public bool Active { get; set; } = true;
    public DateTime? LastDone { get; set; }

    public static readonly string[] Icons = { "💧", "🚶", "🍽️", "💊", "📖", "📅", "🏃", "😴", "🧘", "📞", "🛒", "🍵", "🌿", "👣", "⏰" };

    [Ignore] public bool DoneToday => LastDone.HasValue && LastDone.Value.Date == DateTime.Today;
    [Ignore] public string StateText => Active ? "Açık" : "Kapalı";
}

// ───────────── YÜRÜME SAYAR ─────────────
public class StepDay : IEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public DateTime Date { get; set; }   // günün başlangıcı (00:00)
    public int Steps { get; set; }
}
