# TROÇKİ VİTANEX — Kişisel Yaşam Yönetim Sistemi

**Hayatını Sen Yönet — Daha sağlıklı, daha dengeli, daha üretken bir yaşam.**

C# · .NET MAUI · Android (telefon + tablet) · SQLite (yerel veritabanı)

---

## 1. Visual Studio'da açma

1. ZIP'e sağ tıklayıp **Tümünü ayıkla** deyin ve kalıcı bir klasöre çıkarın (ör. `C:\Projeler\VITANEX`).
   ZIP'in içinden doğrudan açmayın; o zaman proje geçici (Temp) klasörde açılır ve değişiklikler kaybolur.
2. `VITANEX.sln` dosyasına çift tıklayın.
3. Üstteki çalıştırma listesinden **Android Emulator** veya USB ile bağlı telefonunuzu / tabletinizi seçin.
4. ▶ (Çalıştır) tuşuna basın. İlk açılışta NuGet paketleri (sqlite-net-pcl) otomatik indirilir.

> **.NET sürümü:** Proje Visual Studio 2026 ile gelen **.NET 10** (`net10.0-android`) için ayarlıdır.

## 2. APK oluşturma (telefona / tablete kurmak için)

**Yol A — Visual Studio menüsünden:**
Çözüm Gezgini → VITANEX projesine sağ tık → **Yayımla (Publish…)** → *Ad Hoc* → imzalama anahtarı oluşturun → **Farklı Kaydet**.
Oluşan `.apk` dosyasını cihaza kopyalayıp açın (bilinmeyen kaynaklara izin verin).

**Yol B — Komut satırından** (Visual Studio > Araçlar > Komut Satırı > Geliştirici PowerShell):

```
cd VITANEX
dotnet publish -f net10.0-android -c Release
```

APK: `VITANEX\bin\Release\net10.0-android\publish\com.trocki.vitanex-Signed.apk`

> **Sürüm 1.1:** Yürüme Sayar ve Şifalı Bitkiler eklendi. Yürüme sayar Android 10+ cihazlarda "Fiziksel etkinlik" izni ister.

## 3. Ekranlar ve özellikler

| Bölüm | Neler var |
|---|---|
| 🏠 **Ana Sayfa** | Logo, karşılama ("Günaydın Troçki"), tarih, Sağlık % / Finans ₺ / Görev x/y kartları, 12 modül, Bugünün Hatırlatıcıları, + hızlı ekle, bildirim zili |
| ❤️ **Sağlık** | Kilo, tansiyon, nabız, SpO₂, uyku, egzersiz, not · su takibi (+200/+500) · ilaçlar (doz, saat, otomatik hatırlatıcı) · kilo ve tansiyon grafikleri · kayıt geçmişi |
| 🍽️ **Beslenme** | Kahvaltı / öğle / akşam / ara öğün, kalori, not · gün gün gezinme · su · son 7 gün kalori grafiği |
| 💰 **Finans** | Gelir/gider kaydı (açıklama, tutar, tarih, kategori) · aylık gelir, gider, net bakiye, tasarruf oranı · 6 aylık grafik · kategori dağılımı |
| ✅ **Görevler** | Başlık, tarih, saat, öncelik, durum (Bekliyor / Devam Ediyor / Tamamlandı) · filtreler · daireye dokununca durum değişir · yarına erteleme |
| 📘 **Eğitim** · 🌱 **Kişisel Gelişim** · 👥 **Sosyal Yaşam** · 💼 **İş / Kariyer** | Türe göre kayıtlar, ilerleme (%), durum, tarih, not |
| 📊 **İstatistikler** | Günlük / Haftalık / Aylık / Yıllık · Sağlık-Finans-Görev halka göstergeleri · gelir-gider, sağlık skoru, kilo, görev, su grafikleri |
| 📄 **Raporlar** | Genel / Sağlık / Finans / Görev / Beslenme raporu · tarih aralığı · önizleme · **PDF kaydet, paylaş, yazdır** |
| ☁️ **Yedekleme** | Tek tuşla yedek · paylaş (Drive, e-posta) · dosyadan geri yükleme (önce otomatik güvenlik yedeği alır) |
| 👣 **Yürüme Sayar** | Telefonun adım sensörüyle otomatik sayım (uygulama kapalıyken de sayar, açılınca eklenir) · günlük hedef, adım uzunluğu · mesafe ve kalori · son 7 gün grafiği, hedef serisi, en iyi gün · elle adım girme |
| 🌿 **Şifalı Bitkiler** | 60 bitki · gerçek fotoğraflar (Wikimedia Commons, bir kez indirilir sonra çevrimdışı) · günün bitkisi · arama ve 10 kategori · kullanılan kısım, geleneksel kullanım, hazırlanışı, ⚠️ dikkat · favoriler · çay hatırlatıcısı |
| ⚙️ **Ayarlar** | Tema (Cihaza göre / Açık / Koyu), kullanıcı adı, para birimi, günlük hedefler (su, uyku, egzersiz, kalori), hatırlatıcı yönetimi, tüm verileri silme |

**Koyu tema:** Ayarlar → Tema'dan ya da ana sayfadaki ☰ menüsünden 🌙 Koyu tema ile açılır. "Cihaz ayarına göre" seçilirse telefonun/tabletin temasını izler.

**Tablet uyumu:** Telefonda 4 sütunlu modül ızgarası; tablette 6 sütun. Tablet yatayda ana sayfa iki bölmeye ayrılır (solda kartlar + modüller, sağda hatırlatıcılar). Sağlık, Beslenme, Finans ve İstatistik ekranları tablette iki sütun kart düzenine geçer.

**Sağlık skoru nasıl hesaplanıyor?** Günlük su, uyku ve egzersiz hedeflerine ulaşma oranlarının ortalaması (hedefler Ayarlar'dan değiştirilebilir).

## 4. Proje yapısı

```
VITANEX/
├─ VITANEX.sln
└─ VITANEX/
   ├─ Models/Models.cs          → SQLite tabloları (HealthRecord, WaterLog, Medication, Meal,
   │                               FinanceRecord, TaskItem, ModuleEntry, Reminder)
   ├─ Services/
   │   ├─ Db.cs                 → veritabanı bağlantısı, kaydet/sil, tarih aralığı sorgusu
   │   ├─ Stats.cs              → sağlık skoru, dönem dilimleri
   │   ├─ ReportService.cs      → rapor içeriği
   │   ├─ PdfExporter.cs        → Android PDF çıktısı (ek paket gerekmez)
   │   ├─ Nav.cs                → modüller ve gezinme
   │   └─ Common.cs             → renkler, ayarlar, biçimlendirme, tablet düzeni
   ├─ Charts/Charts.cs          → sütun, çizgi ve halka grafikler (MAUI Graphics)
   ├─ Pages/                    → tüm ekranlar (XAML + C#)
   ├─ Resources/                → logo, ikon, açılış ekranı, manzara, renkler, stiller
   └─ Platforms/Android/        → Android ayarları
```

## 5. Sonraki sürüm için öneriler

- Android bildirimleri (hatırlatıcı saatinde telefon bildirimi) — `Plugin.LocalNotification`
- Uygulama kilidi: PIN / parmak izi
- Alışkanlıklarda seri (streak) takibi
- Bulut senkronizasyonu (ör. Supabase)
