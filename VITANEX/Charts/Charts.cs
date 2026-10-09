using VITANEX.Services;
using HA = Microsoft.Maui.Graphics.HorizontalAlignment;
using VA = Microsoft.Maui.Graphics.VerticalAlignment;

namespace VITANEX.Charts;

static class ChartUtil
{
    public static string Short(double v)
    {
        double a = Math.Abs(v);
        if (a >= 1_000_000) return (v / 1_000_000).ToString("0.#", Fmt.TR) + "M";
        if (a >= 1_000) return (v / 1_000).ToString("0.#", Fmt.TR) + "B";
        return v.ToString("0.#", Fmt.TR);
    }

    public static void Empty(ICanvas canvas, RectF r, string text)
    {
        canvas.FontColor = AppColors.Muted;
        canvas.FontSize = 13;
        canvas.DrawString(text, r.X, r.Y, r.Width, r.Height, HA.Center, VA.Center);
    }

    public static void Grid(ICanvas canvas, RectF r, float left, float top, float right, float bottom, double min, double max)
    {
        canvas.FontSize = 10;
        canvas.FontColor = AppColors.Muted;
        canvas.StrokeSize = 1;
        canvas.StrokeColor = AppColors.Line;
        for (int i = 0; i <= 4; i++)
        {
            float y = bottom - (bottom - top) * i / 4f;
            canvas.DrawLine(left, y, right, y);
            canvas.DrawString(Short(min + (max - min) * i / 4), r.Left, y - 7, left - r.Left - 4, 14, HA.Right, VA.Center);
        }
    }
}

/// <summary>Sütun grafik (bir veya iki seri).</summary>
public class BarChartDrawable : IDrawable
{
    public List<string> Labels { get; set; } = new();
    public List<double> SeriesA { get; set; } = new();
    public List<double> SeriesB { get; set; }
    public Color ColorA { get; set; } = AppColors.Green;
    public Color ColorB { get; set; } = AppColors.Red;
    public string EmptyText { get; set; } = "Bu dönemde veri yok";

    public void Draw(ICanvas canvas, RectF r)
    {
        int n = Labels.Count;
        double max = SeriesA.Concat(SeriesB ?? new List<double>()).DefaultIfEmpty(0).Max();
        if (n == 0 || max <= 0) { ChartUtil.Empty(canvas, r, EmptyText); return; }
        max *= 1.1;

        float left = r.Left + 38, right = r.Right - 6, top = r.Top + 8, bottom = r.Bottom - 22;
        ChartUtil.Grid(canvas, r, left, top, right, bottom, 0, max);

        int series = SeriesB == null ? 1 : 2;
        float slot = (right - left) / n;
        float bw = Math.Min(26f, slot * 0.72f / series);

        for (int i = 0; i < n; i++)
        {
            float x0 = left + slot * i + (slot - bw * series) / 2f;
            DrawBar(canvas, x0, bottom, top, bw, SeriesA[i], max, ColorA);
            if (SeriesB != null) DrawBar(canvas, x0 + bw, bottom, top, bw, SeriesB[i], max, ColorB);

            canvas.FontSize = 10;
            canvas.FontColor = AppColors.Muted;
            canvas.DrawString(Labels[i], left + slot * i - 4, bottom + 4, slot + 8, 16, HA.Center, VA.Top);
        }
    }

    static void DrawBar(ICanvas canvas, float x, float bottom, float top, float w, double v, double max, Color c)
    {
        if (v <= 0) return;
        float h = (float)(v / max * (bottom - top));
        canvas.FillColor = c;
        canvas.FillRoundedRectangle(x + 1, bottom - h, w - 2, h, Math.Min(5f, h / 2f));
    }
}

/// <summary>Çizgi grafik. Değeri olmayan noktalar (null) atlanır.</summary>
public class LineChartDrawable : IDrawable
{
    public List<string> Labels { get; set; } = new();
    public List<double?> Values { get; set; } = new();
    public Color LineColor { get; set; } = AppColors.Primary;
    public string EmptyText { get; set; } = "Bu dönemde veri yok";
    public bool ZeroBased { get; set; }

    public void Draw(ICanvas canvas, RectF r)
    {
        var vals = Values.Where(v => v.HasValue).Select(v => v.Value).ToList();
        if (Labels.Count == 0 || vals.Count == 0) { ChartUtil.Empty(canvas, r, EmptyText); return; }

        double min = ZeroBased ? 0 : vals.Min(), max = vals.Max();
        if (Math.Abs(max - min) < 0.0001) { min -= 1; max += 1; }
        double pad = (max - min) * 0.15;
        if (!ZeroBased) min -= pad;
        max += pad;

        float left = r.Left + 38, right = r.Right - 10, top = r.Top + 8, bottom = r.Bottom - 22;
        ChartUtil.Grid(canvas, r, left, top, right, bottom, min, max);

        int n = Labels.Count;
        float step = n > 1 ? (right - left) / (n - 1) : 0;
        var path = new PathF();
        bool started = false;
        var points = new List<PointF>();

        for (int i = 0; i < n; i++)
        {
            float x = n > 1 ? left + step * i : (left + right) / 2;
            canvas.FontSize = 10;
            canvas.FontColor = AppColors.Muted;
            float lw = Math.Max(step, 30);
            canvas.DrawString(Labels[i], x - lw / 2, bottom + 4, lw, 16, HA.Center, VA.Top);

            if (!Values[i].HasValue) continue;
            float y = bottom - (float)((Values[i].Value - min) / (max - min)) * (bottom - top);
            if (!started) { path.MoveTo(x, y); started = true; }
            else path.LineTo(x, y);
            points.Add(new PointF(x, y));
        }

        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 3;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);

        foreach (var p in points)
        {
            canvas.FillColor = AppColors.Card;
            canvas.FillCircle(p.X, p.Y, 5);
            canvas.FillColor = LineColor;
            canvas.FillCircle(p.X, p.Y, 3);
        }
    }
}

/// <summary>Halka (donut) ilerleme göstergesi.</summary>
public class DonutDrawable : IDrawable
{
    public double Percent { get; set; }
    public Color RingColor { get; set; } = AppColors.Primary;

    public void Draw(ICanvas canvas, RectF r)
    {
        float size = Math.Min(r.Width, r.Height) - 14;
        float x = r.Center.X - size / 2, y = r.Center.Y - size / 2;
        float stroke = Math.Max(8, size / 9);

        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeColor = AppColors.Line;
        canvas.DrawEllipse(x, y, size, size);

        double p = Math.Clamp(Percent, 0, 100) / 100.0;
        if (p >= 0.999)
        {
            canvas.StrokeColor = RingColor;
            canvas.DrawEllipse(x, y, size, size);
        }
        else if (p > 0.001)
        {
            canvas.StrokeColor = RingColor;
            canvas.DrawArc(x, y, size, size, 90, (float)(90 - 360 * p), true, false);
        }

        canvas.FontColor = AppColors.PrimaryDark;
        canvas.FontSize = Math.Max(14, size / 4.2f);
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.DrawString($"%{Percent:0}", r.X, r.Y, r.Width, r.Height, HA.Center, VA.Center);
    }
}
