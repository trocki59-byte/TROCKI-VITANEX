using VITANEX.Services;

namespace VITANEX.Pages;

/// <summary>Şifalı Bitkiler rehberi: günün bitkisi, arama, kategori ve favoriler.</summary>
public class PlantsPage : ContentPage
{
    readonly Entry _search;
    readonly HorizontalStackLayout _chips = new() { Spacing = 8 };
    readonly VerticalStackLayout _list = new() { Spacing = 0 };
    readonly Label _empty;
    string _cat = "";   // "" = tümü, "fav" = favoriler, aksi hâlde kategori harfi
    CancellationTokenSource _typing;

    public PlantsPage()
    {
        Title = "Şifalı Bitkiler";

        _search = new Entry { Placeholder = "🔍  Bitki veya şikâyet ara (örn. öksürük, uyku)", ClearButtonVisibility = ClearButtonVisibility.WhileEditing };
        _search.TextChanged += async (s, e) =>
        {
            _typing?.Cancel();
            var cts = _typing = new CancellationTokenSource();
            try { await Task.Delay(250, cts.Token); } catch { return; }
            BuildList();
        };

        _empty = new Label { Style = Ui.Style("Muted"), Margin = new Thickness(0, 14), IsVisible = false };

        var root = new VerticalStackLayout
        {
            Padding = new Thickness(16, 16, 16, 40),
            Spacing = 12,
            MaximumWidthRequest = 900,
            Children =
            {
                DayCard(),
                new Border { Style = Ui.Style("Card"), Padding = new Thickness(10, 2), Content = _search },
                new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _chips },
                new Border { Style = Ui.Style("Card"), Padding = new Thickness(14, 2), Content = new VerticalStackLayout { Children = { _list, _empty } } },
                new Label
                {
                    Style = Ui.Style("Muted"), LineHeight = 1.3,
                    Text = "⚠️ Bu rehber geleneksel kullanım bilgisi sunar; tanı ve tedavi yerine geçmez. Hastalığınız varsa, ilaç kullanıyorsanız, gebe veya emziriyorsanız bitkisel ürün kullanmadan önce doktorunuza ya da eczacınıza danışın.\nFotoğraflar: Wikimedia Commons (her bitkinin sayfasında yazar ve lisans bilgisi var)."
                }
            }
        };
        Content = new ScrollView { Content = root };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BuildChips();
        BuildList();
    }

    View DayCard()
    {
        int i = PlantData.OfTheDay;
        var p = PlantData.All[i];
        var grid = new Grid { HeightRequest = 200 };
        grid.Children.Add(PlantImages.Box(i, -1, 200, 18));
        grid.Children.Add(new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            Background = new LinearGradientBrush(new GradientStopCollection
            {
                new GradientStop(Color.FromArgb("#00000000"), 0.3f),
                new GradientStop(Color.FromArgb("#E60B2A18"), 1f)
            }, new Point(0, 0), new Point(0, 1))
        });
        grid.Children.Add(new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.End,
            Padding = new Thickness(14, 0, 14, 12),
            Spacing = 2,
            Children =
            {
                new Label { Text = "🌿 GÜNÜN BİTKİSİ", TextColor = Colors.White, FontSize = 11, FontAttributes = FontAttributes.Bold, CharacterSpacing = 2 },
                new Label
                {
                    TextColor = Colors.White,
                    FormattedText = new FormattedString
                    {
                        Spans =
                        {
                            new Span { Text = p.Name + "  ", FontSize = 21, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                            new Span { Text = p.Latin, FontSize = 13, FontAttributes = FontAttributes.Italic, TextColor = Colors.White }
                        }
                    }
                },
                new Label { Text = p.Uses, TextColor = Colors.White, FontSize = 12.5, MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation }
            }
        });
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) => await Nav.Push(new PlantDetailPage(i));
        grid.GestureRecognizers.Add(tap);
        return grid;
    }

    void BuildChips()
    {
        _chips.Children.Clear();
        var favs = PlantData.Favorites;
        AddChip("", $"Tümü ({PlantData.All.Length})");
        AddChip("fav", $"⭐ Favoriler ({favs.Count})");
        foreach (var kv in PlantData.Categories)
            AddChip(kv.Key.ToString(), $"{kv.Value.Emoji} {kv.Value.Name}");
    }

    void AddChip(string key, string text)
    {
        var b = new Button { Text = text, Style = Ui.Style("ChipButton") };
        Ui.SetChip(b, _cat == key);
        b.Clicked += (s, e) =>
        {
            _cat = key;
            foreach (var c in _chips.Children.OfType<Button>()) Ui.SetChip(c, c == b);
            BuildList();
        };
        _chips.Children.Add(b);
    }

    static string Low(string s) => (s ?? "").ToLower(Fmt.TR);

    void BuildList()
    {
        _list.Children.Clear();
        var favs = PlantData.Favorites;
        var q = Low(_search.Text?.Trim());
        int shown = 0;

        for (int i = 0; i < PlantData.All.Length; i++)
        {
            var p = PlantData.All[i];
            bool catOk = _cat == "" || (_cat == "fav" ? favs.Contains(i) : p.Cats.Contains(_cat[0]));
            if (!catOk) continue;
            if (q.Length > 0 && !Low($"{p.Name} {p.Latin} {p.Uses} {p.CategoryText}").Contains(q)) continue;

            _list.Children.Add(Row(i, favs.Contains(i), shown > 0));
            shown++;
        }

        _empty.IsVisible = shown == 0;
        _empty.Text = _cat == "fav" ? "Henüz favori bitkiniz yok. Bir bitkiyi açıp ⭐ ile ekleyebilirsiniz." : "Aramanıza uygun bitki bulunamadı.";
    }

    View Row(int i, bool fav, bool line)
    {
        var p = PlantData.All[i];
        var g = new Grid { ColumnSpacing = 12, Padding = new Thickness(0, 10) };
        Ui.Cols(g, 60, -1, 16);
        g.Add(PlantImages.Box(i, 60, 60, 14), 0, 0);
        g.Add(new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 1,
            Children =
            {
                new Label { Text = p.Name + (fav ? "  ⭐" : ""), FontSize = 15.5, FontAttributes = FontAttributes.Bold, TextColor = AppColors.Text },
                new Label { Text = p.Latin, FontSize = 12, FontAttributes = FontAttributes.Italic, TextColor = AppColors.Muted },
                new Label { Text = p.CategoryText, FontSize = 12, TextColor = AppColors.Muted, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation }
            }
        }, 1, 0);
        g.Add(new Label { Text = "›", FontSize = 22, TextColor = AppColors.Muted, VerticalOptions = LayoutOptions.Center }, 2, 0);

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) => await Nav.Push(new PlantDetailPage(i));
        g.GestureRecognizers.Add(tap);

        if (!line) return g;
        return new VerticalStackLayout { Children = { new BoxView { HeightRequest = 1, Color = AppColors.Line }, g } };
    }
}

/// <summary>Tek bitkinin ayrıntı sayfası.</summary>
public class PlantDetailPage : ContentPage
{
    readonly int _i;
    readonly Button _favBtn;

    public PlantDetailPage(int i)
    {
        _i = i;
        var p = PlantData.All[i];
        Title = p.Name;

        var credit = new Label
        {
            FontSize = 10.5, TextColor = AppColors.Muted, HorizontalTextAlignment = TextAlignment.End, Margin = new Thickness(0, 4, 4, 0),
            Text = $"📷 {(string.IsNullOrWhiteSpace(p.Author) ? "" : p.Author + " · ")}{p.License} · Wikimedia Commons ›"
        };
        var creditTap = new TapGestureRecognizer();
        creditTap.Tapped += async (s, e) => { try { await Launcher.Default.OpenAsync(p.CommonsUrl); } catch { } };
        credit.GestureRecognizers.Add(creditTap);

        var chips = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Center };
        foreach (var c in p.Cats)
        {
            var cat = PlantData.Categories[c];
            var chip = new Border
            {
                StrokeThickness = 0, BackgroundColor = AppColors.PrimaryLight, Padding = new Thickness(12, 6), Margin = new Thickness(4),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                Content = new Label { Text = $"{cat.Emoji} {cat.Name}", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.IsDark ? AppColors.PrimaryDark : AppColors.Primary }
            };
            chips.Children.Add(chip);
        }

        _favBtn = new Button { Style = Ui.Style("SecondaryButton") };
        _favBtn.Clicked += (s, e) =>
        {
            bool added = PlantData.ToggleFavorite(_i);
            UpdateFav();
            _ = Ui.Alert(added ? "⭐ Favorilere eklendi" : "Favorilerden çıkarıldı", p.Name);
        };
        UpdateFav();

        var remBtn = new Button { Text = "⏰ Hatırlatıcı" };
        remBtn.Clicked += async (s, e) => await Nav.Push(new ReminderEntryPage(new Models.Reminder
        {
            Title = p.Name + " çayı", Time = "20:00", Icon = "🍵", Active = true
        }));

        var buttons = new Grid { ColumnSpacing = 10 };
        Ui.Cols(buttons, -1, -1);
        buttons.Add(_favBtn, 0, 0);
        buttons.Add(remBtn, 1, 0);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 12, 16, 40),
                Spacing = 12,
                MaximumWidthRequest = 760,
                Children =
                {
                    new VerticalStackLayout { Children = { PlantImages.Box(i, -1, 270, 20), credit } },
                    new Border
                    {
                        Style = Ui.Style("Card"),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 2,
                            Children =
                            {
                                new Label { Text = $"{p.Emoji} {p.Name}", FontSize = 23, FontAttributes = FontAttributes.Bold, TextColor = AppColors.PrimaryDark, HorizontalOptions = LayoutOptions.Center },
                                new Label { Text = p.Latin, FontSize = 13.5, FontAttributes = FontAttributes.Italic, TextColor = AppColors.Muted, HorizontalOptions = LayoutOptions.Center },
                                chips
                            }
                        }
                    },
                    Section("🌱 Kullanılan kısım", p.Part, null),
                    Section("✨ Geleneksel kullanım", p.Uses, null),
                    Section("🍵 Nasıl kullanılır?", p.HowTo, null),
                    Section("⚠️ Dikkat", p.Caution, AppColors.Red),
                    buttons,
                    new Label { Style = Ui.Style("Muted"), Text = "Bilgiler geleneksel ve bilimsel kaynaklara dayalı genel bilgidir; tedavi yerine geçmez." }
                }
            }
        };
    }

    void UpdateFav() =>
        _favBtn.Text = PlantData.Favorites.Contains(_i) ? "★ Favoriden çıkar" : "☆ Favorilere ekle";

    static View Section(string title, string text, Color color) => new Border
    {
        Style = Ui.Style("Card"),
        Content = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = title, FontSize = 15.5, FontAttributes = FontAttributes.Bold, TextColor = color ?? AppColors.PrimaryDark },
                new Label { Text = text, FontSize = 14.5, LineHeight = 1.25, TextColor = AppColors.Text }
            }
        }
    };
}
