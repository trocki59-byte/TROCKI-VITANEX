using VITANEX.Services;

namespace VITANEX.Pages;

public class FileItem
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Info { get; set; }

    public static List<FileItem> List(string folder, string pattern)
    {
        if (!Directory.Exists(folder)) return new List<FileItem>();
        return new DirectoryInfo(folder).GetFiles(pattern)
            .OrderByDescending(f => f.LastWriteTime)
            .Select(f => new FileItem
            {
                Name = f.Name,
                Path = f.FullName,
                Info = $"{f.LastWriteTime:dd.MM.yyyy HH:mm}  ·  {f.Length / 1024.0:0.#} KB"
            }).ToList();
    }
}

public partial class ReportsPage : ContentPage
{
    public ReportsPage()
    {
        InitializeComponent();
        TypeField.ItemsSource = ReportService.Types;
        TypeField.SelectedIndex = 0;
        SetRange("month");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSaved();
    }

    void LoadSaved()
    {
        var files = FileItem.List(PdfExporter.ReportsFolder, "*.pdf");
        BindableLayout.SetItemsSource(SavedList, files);
        NoSaved.IsVisible = files.Count == 0;
    }

    void SetRange(string key)
    {
        var t = DateTime.Today;
        var ms = new DateTime(t.Year, t.Month, 1);
        (DateTime s, DateTime e) = key switch
        {
            "week" => (t.AddDays(-(((int)t.DayOfWeek + 6) % 7)), t),
            "lastmonth" => (ms.AddMonths(-1), ms.AddDays(-1)),
            "3m" => (ms.AddMonths(-2), t),
            "year" => (new DateTime(t.Year, 1, 1), t),
            _ => (ms, t)
        };
        StartField.Date = s;
        EndField.Date = e;
    }

    void OnQuickRange(object sender, EventArgs e) => SetRange(((Button)sender).ClassId);

    async Task<ReportDoc> BuildAsync()
    {
        var s = (DateTime)StartField.Date;
        var e = (DateTime)EndField.Date;
        if (e < s) (s, e) = (e, s);
        return await ReportService.Build(Math.Max(0, TypeField.SelectedIndex), s, e);
    }

    void SetBusy(bool b)
    {
        Busy.IsRunning = b;
        Busy.IsVisible = b;
    }

    async void OnPreview(object sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            var doc = await BuildAsync();
            PreviewTitle.Text = doc.Title;
            PreviewSub.Text = doc.Subtitle;
            PreviewBody.Text = doc.ToPlainText();
            PreviewCard.IsVisible = true;
        }
        catch (Exception ex) { await DisplayAlertAsync("Hata", ex.Message, "Tamam"); }
        finally { SetBusy(false); }
    }

    async void OnPdf(object sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            var doc = await BuildAsync();
            var path = await PdfExporter.Export(doc);
            LoadSaved();
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = doc.Title,
                File = new ShareFile(path, "application/pdf")
            });
        }
        catch (Exception ex) { await DisplayAlertAsync("Hata", ex.Message, "Tamam"); }
        finally { SetBusy(false); }
    }

    async void OnSavedTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not FileItem f) return;
        var choice = await DisplayActionSheetAsync(f.Name, "Vazgeç", "Sil", "Aç", "Paylaş / Yazdır");
        try
        {
            switch (choice)
            {
                case "Aç":
                    await Launcher.Default.OpenAsync(new OpenFileRequest(f.Name, new ReadOnlyFile(f.Path, "application/pdf")));
                    break;
                case "Paylaş / Yazdır":
                    await Share.Default.RequestAsync(new ShareFileRequest { Title = f.Name, File = new ShareFile(f.Path, "application/pdf") });
                    break;
                case "Sil":
                    if (await Ui.Confirm("Sil", "Rapor silinsin mi?"))
                    {
                        File.Delete(f.Path);
                        LoadSaved();
                    }
                    break;
            }
        }
        catch (Exception ex) { await DisplayAlertAsync("Hata", ex.Message, "Tamam"); }
    }
}
