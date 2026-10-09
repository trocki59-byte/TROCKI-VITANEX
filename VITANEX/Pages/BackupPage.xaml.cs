using VITANEX.Services;

namespace VITANEX.Pages;

public partial class BackupPage : ContentPage
{
    static string BackupFolder
    {
        get
        {
            var dir = System.IO.Path.Combine(FileSystem.AppDataDirectory, "backups");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public BackupPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    void Refresh()
    {
        var info = new FileInfo(Db.DbPath);
        DbInfo.Text = info.Exists
            ? $"Mevcut veritabanı: {info.Length / 1024.0:0.#} KB  ·  son değişiklik {info.LastWriteTime:dd.MM.yyyy HH:mm}"
            : "Veritabanı henüz oluşturulmadı.";

        var list = FileItem.List(BackupFolder, "*.db3");
        BindableLayout.SetItemsSource(BackupList, list);
        NoBackups.IsVisible = list.Count == 0;
    }

    async void OnBackup(object sender, EventArgs e)
    {
        try
        {
            await Db.Get();      // dosyanın var olduğundan emin ol
            await Db.Close();
            var target = System.IO.Path.Combine(BackupFolder, $"VITANEX_yedek_{DateTime.Now:yyyyMMdd_HHmm}.db3");
            File.Copy(Db.DbPath, target, true);
            await Db.Get();
            Refresh();

            if (await DisplayAlertAsync("Yedek oluşturuldu", "Yedeği cihaz dışına (Drive, e-posta, bilgisayar) göndermek ister misiniz?", "Paylaş", "Sonra"))
                await ShareBackup(target);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Hata", ex.Message, "Tamam");
        }
    }

    static Task ShareBackup(string path) =>
        Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "TROÇKİ VİTANEX yedeği",
            File = new ShareFile(path, "application/octet-stream")
        });

    async void OnBackupTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not FileItem f) return;
        var choice = await DisplayActionSheetAsync(f.Name, "Vazgeç", "Sil", "Geri Yükle", "Paylaş");
        try
        {
            switch (choice)
            {
                case "Geri Yükle": await Restore(f.Path); break;
                case "Paylaş": await ShareBackup(f.Path); break;
                case "Sil":
                    if (await Ui.Confirm("Sil", "Bu yedek silinsin mi?"))
                    {
                        File.Delete(f.Path);
                        Refresh();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Hata", ex.Message, "Tamam");
        }
    }

    async void OnRestoreFromFile(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "VİTANEX yedek dosyasını seçin" });
            if (result == null) return;

            // Önce geçici bir kopyaya al, SQLite dosyası olduğunu doğrula
            var temp = System.IO.Path.Combine(FileSystem.CacheDirectory, "restore_tmp.db3");
            using (var src = await result.OpenReadAsync())
            using (var dst = File.Create(temp))
                await src.CopyToAsync(dst);

            await Restore(temp);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Hata", ex.Message, "Tamam");
        }
    }

    static bool IsSqlite(string path)
    {
        var header = new byte[16];
        using var fs = File.OpenRead(path);
        if (fs.Read(header, 0, 16) < 16) return false;
        return System.Text.Encoding.ASCII.GetString(header, 0, 15) == "SQLite format 3";
    }

    async Task Restore(string path)
    {
        if (!IsSqlite(path))
        {
            await DisplayAlertAsync("Geçersiz dosya", "Seçilen dosya bir VİTANEX yedeği değil.", "Tamam");
            return;
        }
        if (!await Ui.Confirm("Geri Yükle", "Mevcut tüm veriler bu yedekle DEĞİŞTİRİLECEK. Devam edilsin mi?", "Geri Yükle"))
            return;

        // Güvenlik için mevcut veriyi otomatik yedekle
        await Db.Get();
        await Db.Close();
        if (File.Exists(Db.DbPath))
            File.Copy(Db.DbPath, System.IO.Path.Combine(BackupFolder, $"VITANEX_otomatik_{DateTime.Now:yyyyMMdd_HHmmss}.db3"), true);

        File.Copy(path, Db.DbPath, true);
        foreach (var extra in new[] { "-wal", "-shm", "-journal" })
            if (File.Exists(Db.DbPath + extra)) File.Delete(Db.DbPath + extra);

        await Db.Get();   // tabloları kontrol eder / eksikleri oluşturur
        Refresh();
        await DisplayAlertAsync("Tamamlandı", "Veriler geri yüklendi. (Önceki verileriniz otomatik yedek olarak saklandı.)", "Tamam");
    }
}
