#if ANDROID
using AG = Android.Graphics;
using APdf = Android.Graphics.Pdf;
#endif

namespace VITANEX.Services;

/// <summary>Raporu A4 PDF olarak kaydeder (Android yerleşik PdfDocument API'si, ek paket gerekmez).</summary>
public static class PdfExporter
{
    public static string ReportsFolder
    {
        get
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, "reports");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static Task<string> Export(ReportDoc doc)
    {
#if ANDROID
        return Task.Run(() => ExportAndroid(doc));
#else
        throw new PlatformNotSupportedException("PDF yalnızca Android'de destekleniyor.");
#endif
    }

#if ANDROID
    static AG.Paint P(string color, float size, bool bold = false)
    {
        var p = new AG.Paint { AntiAlias = true, TextSize = size, FakeBoldText = bold };
        p.Color = AG.Color.ParseColor(color);
        return p;
    }

    static string ExportAndroid(ReportDoc doc)
    {
        const int W = 595, H = 842, M = 40;
        var pdf = new APdf.PdfDocument();
        int pageNo = 0;
        APdf.PdfDocument.Page page = null;
        AG.Canvas canvas = null;
        float y = 0;

        var title = P("#FFFFFF", 22, true);
        var sub = P("#DCE8FF", 10);
        var section = P("#0A2E8A", 15, true);
        var label = P("#5B6B8C", 11);
        var value = P("#0E1B3D", 11, true);
        var small = P("#8A97B2", 9);
        var line = P("#E3EAF5", 1);
        var barBg = P("#EEF3FB", 1);
        var bar = P("#1E88FF", 1);
        var band = P("#0B4FD6", 1);

        void Footer()
        {
            canvas.DrawLine(M, H - 32, W - M, H - 32, line);
            canvas.DrawText("TROÇKİ VİTANEX  ·  Hayatını Sen Yönet", M, H - 18, small);
            var t = $"Sayfa {pageNo}";
            canvas.DrawText(t, W - M - small.MeasureText(t), H - 18, small);
        }

        void NewPage()
        {
            if (page != null) { Footer(); pdf.FinishPage(page); }
            pageNo++;
            page = pdf.StartPage(new APdf.PdfDocument.PageInfo.Builder(W, H, pageNo).Create());
            canvas = page.Canvas;
            y = M;
        }

        void Need(float h)
        {
            if (y + h > H - 48) NewPage();
        }

        NewPage();

        // Başlık bandı
        canvas.DrawRect(0, 0, W, 96, band);
        canvas.DrawText("TROÇKİ VİTANEX", M, 38, P("#9CC8FF", 12, true));
        canvas.DrawText(doc.Title, M, 64, title);
        canvas.DrawText(doc.Subtitle, M, 84, sub);
        y = 124;

        foreach (var s in doc.Sections)
        {
            Need(60);
            canvas.DrawText(s.Title, M, y, section);
            y += 8;
            canvas.DrawLine(M, y, W - M, y, line);
            y += 18;

            foreach (var r in s.Rows)
            {
                Need(18);
                canvas.DrawText(r.Label, M + 6, y, label);
                canvas.DrawText(r.Value ?? "", W - M - value.MeasureText(r.Value ?? ""), y, value);
                y += 18;
            }

            if (s.Bars.Count > 0)
            {
                y += 6;
                Need(30);
                canvas.DrawText(s.BarsTitle ?? "", M + 6, y, P("#0A2E8A", 11, true));
                y += 14;
                double max = s.Bars.Max(b => b.Value);
                float barLeft = M + 130, barMax = W - M - 90 - barLeft;
                foreach (var b in s.Bars)
                {
                    Need(20);
                    canvas.DrawText(b.Label, M + 6, y + 10, label);
                    canvas.DrawRect(barLeft, y, barLeft + barMax, y + 12, barBg);
                    float bw = max > 0 ? (float)(b.Value / max) * barMax : 0;
                    canvas.DrawRect(barLeft, y, barLeft + Math.Max(2, bw), y + 12, bar);
                    canvas.DrawText(b.ValueText, W - M - value.MeasureText(b.ValueText), y + 10, value);
                    y += 20;
                }
            }
            y += 18;
        }

        Footer();
        pdf.FinishPage(page);

        var path = Path.Combine(PdfExporter.ReportsFolder,
            $"VITANEX_{Slug(doc.Title)}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        using (var fs = File.Create(path))
            pdf.WriteTo(fs);
        pdf.Close();
        return path;
    }

    static string Slug(string s)
    {
        var map = new Dictionary<char, char> { ['ı'] = 'i', ['İ'] = 'I', ['ş'] = 's', ['Ş'] = 'S', ['ğ'] = 'g', ['Ğ'] = 'G', ['ü'] = 'u', ['Ü'] = 'U', ['ö'] = 'o', ['Ö'] = 'O', ['ç'] = 'c', ['Ç'] = 'C' };
        var chars = s.Select(c => map.TryGetValue(c, out var r) ? r : c)
                     .Select(c => char.IsLetterOrDigit(c) ? c : '_');
        return new string(chars.ToArray());
    }
#endif
}
