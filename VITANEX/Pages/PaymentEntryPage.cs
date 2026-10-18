using VITANEX.Models;
using VITANEX.Services;

namespace VITANEX.Pages;

/// <summary>Düzenli ödeme ekleme / düzenleme.</summary>
public class PaymentEntryPage : ContentPage
{
    readonly RecurringPayment _p;
    readonly bool _isNew;
    readonly Picker _type;
    readonly Entry _name, _amount, _day, _left;

    public PaymentEntryPage(RecurringPayment p = null)
    {
        _isNew = p == null;
        _p = p ?? new RecurringPayment { Type = "Kredi Taksiti", Day = 15 };
        Title = _isNew ? "Yeni Düzenli Ödeme" : "Ödemeyi Düzenle";

        _type = new Picker { Title = "Tür", ItemsSource = Payments.Types.Select(t => $"{t.Icon} {t.Type}").ToList() };
        _type.SelectedIndex = Math.Max(0, Array.FindIndex(Payments.Types, t => t.Type == _p.Type));
        _name = new Entry { Placeholder = "örn. Ziraat konut kredisi, Bonus kart, Ev kirası", Text = _p.Name };
        _amount = new Entry { Placeholder = "örn. 4500", Keyboard = Keyboard.Numeric, Text = _p.Amount > 0 ? _p.Amount.ToString("0.##", Fmt.TR) : "" };
        _day = new Entry { Placeholder = "1 – 31", Keyboard = Keyboard.Numeric, Text = _p.Day.ToString() };
        _left = new Entry { Placeholder = "Kredi için kalan taksit (boş bırakılabilir)", Keyboard = Keyboard.Numeric, Text = _p.Left?.ToString() ?? "" };

        var save = new Button { Text = "Kaydet" };
        save.Clicked += OnSave;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16), Spacing = 12, MaximumWidthRequest = 640,
                Children =
                {
                    new Border
                    {
                        Style = Ui.Style("Card"),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 4,
                            Children =
                            {
                                Field("Tür"), _type,
                                Field("Açıklama"), _name,
                                Field("Aylık tutar (₺)"), _amount,
                                Field("Ödeme günü (ayın kaçı?)"), _day,
                                Field("Kalan taksit sayısı"), _left,
                            }
                        }
                    },
                    new Label
                    {
                        Style = Ui.Style("Muted"), LineHeight = 1.3,
                        Text = "🔔 Ödeme gününden 1 gün önce saat 09:00'da ve ödeme günü saat 09:00'da telefonunuza bildirim gelir. Ödeme günü 29–31 ise kısa aylarda ayın son günü kullanılır."
                    },
                    save
                }
            }
        };
    }

    static Label Field(string t) => new() { Text = t, Style = Ui.Style("FieldLabel") };

    async void OnSave(object sender, EventArgs e)
    {
        var amount = Fmt.ParseDecimal(_amount.Text);
        var day = Fmt.ParseInt(_day.Text);
        int? left = string.IsNullOrWhiteSpace(_left.Text) ? null : Fmt.ParseInt(_left.Text);

        if (amount == null || amount <= 0) { await DisplayAlertAsync("Eksik bilgi", "Geçerli bir tutar girin.", "Tamam"); return; }
        if (day == null || day < 1 || day > 31) { await DisplayAlertAsync("Eksik bilgi", "Ödeme günü 1 ile 31 arasında olmalı.", "Tamam"); return; }
        if (left != null && left < 1) { await DisplayAlertAsync("Eksik bilgi", "Kalan taksit 1 veya daha fazla olmalı.", "Tamam"); return; }

        _p.Type = Payments.Types[Math.Max(0, _type.SelectedIndex)].Type;
        _p.Name = string.IsNullOrWhiteSpace(_name.Text) ? _p.Type : _name.Text.Trim();
        _p.Amount = amount.Value;
        _p.Day = day.Value;
        _p.Left = left;

        if (_isNew)
        {
            await Payments.Add(_p);
            if (!await Payments.RequestPermission())
                await DisplayAlertAsync("Bildirim izni", "Hatırlatma bildirimlerinin gelmesi için bildirim iznine izin verin:\nAyarlar → Uygulamalar → TROÇKİ VİTANEX → Bildirimler.", "Tamam");
        }
        else
        {
            await Db.Save(_p);
            await Payments.Reschedule();
        }
        await Navigation.PopAsync();
    }
}
