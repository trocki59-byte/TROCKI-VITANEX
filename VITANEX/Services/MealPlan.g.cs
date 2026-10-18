// Otomatik oluşturuldu: kilo vermeye yönelik 30 günlük menü verisi (web sürümüyle aynı)
namespace VITANEX.Services;

public static partial class MealPlan
{
    public static readonly Dictionary<string, (string Text, int Cal)[]> Lists = new()
    {
        ["Kahvaltı"] = new (string, int)[]
        {
            ("2 haşlanmış yumurta, 30 g beyaz peynir, domates-salatalık, 5 zeytin, 1 ince dilim tam buğday ekmeği", 380),
            ("Sebzeli menemen (2 yumurta, 1 tatlı kaşığı zeytinyağı), 1 ince dilim tam buğday ekmeği", 360),
            ("Yulaf lapası (40 g yulaf, 1 su bardağı yarım yağlı süt), yarım elma, tarçın, 2 ceviz içi", 380),
            ("Lor peynirli omlet (2 yumurta + 2 yemek kaşığı lor), maydanoz, roka, 1 ince dilim çavdar ekmeği", 370),
            ("40 g beyaz peynir, 1 haşlanmış yumurta, bol yeşillik, 1 ince dilim tam buğday ekmeği, 1 çay kaşığı bal", 360),
            ("200 g yoğurt + 2 yemek kaşığı yulaf + yarım muz + 1 tatlı kaşığı keten tohumu", 340),
            ("Ispanaklı yumurta (2 yumurta), domates, 1 ince dilim tam buğday ekmeği", 350),
            ("Tam buğday ekmeğiyle peynirli tost (40 g peynir), domates-salatalık, şekersiz çay", 380),
            ("1 haşlanmış yumurta, 30 g kaşar, yeşil biber, 2 ceviz içi, 1 ince dilim tam buğday ekmeği", 390),
            ("Çılbır (2 yumurta, sarımsaklı yoğurt, az pul biberli tereyağı), 1 ince dilim tam buğday ekmeği", 400),
            ("Mantarlı omlet (2 yumurta), yeşillik tabağı, 1 ince dilim çavdar ekmeği", 350),
            ("150 g süzme yoğurt + 1 kase mevsim meyvesi + 1 yemek kaşığı ceviz", 310),
            ("60 g lor peyniri, 1 haşlanmış yumurta, domates, salatalık, 6 zeytin, 1 ince dilim tam buğday ekmeği", 380),
            ("Yulaf lapası (40 g yulaf, süt) + 1 tatlı kaşığı şekersiz fıstık ezmesi + tarçın", 390),
            ("Domatesli-biberli omlet (2 yumurta, 20 g kaşar), 1 ince dilim tam buğday ekmeği", 380),
        },
        ["Öğle Yemeği"] = new (string, int)[]
        {
            ("1 kase mercimek çorbası, bol mevsim salatası (1 tatlı kaşığı zeytinyağı), 1 ince dilim tam buğday ekmeği", 400),
            ("Izgara tavuk göğsü (120 g), 4 yemek kaşığı bulgur pilavı, çoban salata", 480),
            ("Zeytinyağlı taze fasulye, 1 kase yoğurt, 1 ince dilim tam buğday ekmeği", 420),
            ("Nohutlu ton balığı salatası (yağı süzülmüş ton, 4 yemek kaşığı nohut, yeşillik, limon)", 450),
            ("1 kase az yağlı kuru fasulye, cacık, bol yeşil salata", 450),
            ("Tavuklu sebze sote, 1 kase yoğurt", 430),
            ("1 kase ezogelin çorbası, 3 ızgara köfte, mevsim salatası", 480),
            ("Zeytinyağlı enginar, 1 kase yoğurt, 1 ince dilim tam buğday ekmeği", 380),
            ("Fırında derisiz tavuk baget (1 adet) ve sebzeler, yeşil salata", 450),
            ("Yeşil mercimek yemeği, cacık, 1 ince dilim tam buğday ekmeği", 450),
            ("Zeytinyağlı kabak, 1 kase yoğurt, 4 yemek kaşığı bulgur pilavı", 430),
            ("Tavuk şiş (6 parça), közlenmiş biber-domates, 3 yemek kaşığı bulgur pilavı", 490),
            ("Yumurtalı ıspanak yemeği, 1 kase yoğurt, 1 ince dilim tam buğday ekmeği", 400),
            ("1 kase nohut yemeği, mevsim salatası", 430),
            ("1 porsiyon tam buğday makarna (domates soslu, ton balıklı), yeşil salata", 500),
        },
        ["Akşam Yemeği"] = new (string, int)[]
        {
            ("Fırında somon (120 g), buharda brokoli, yeşil salata", 480),
            ("Etli kabak dolması (4 adet), 1 kase yoğurt", 450),
            ("4 ızgara köfte, közlenmiş biber ve domates, roka salatası", 470),
            ("Fırında levrek, roka-maydanoz salatası, limon", 420),
            ("Zeytinyağlı bezelye, 1 kase yoğurt, 1 ince dilim tam buğday ekmeği", 400),
            ("Sebzeli tavuk sote, 3 yemek kaşığı bulgur pilavı", 470),
            ("Mantar sote, lor peynirli çoban salata, 1 ince dilim tam buğday ekmeği", 400),
            ("Fırında az yağlı patlıcan musakka, cacık", 480),
            ("1 kase sebze çorbası, 2 dilim ızgara hellim, yeşil salata", 430),
            ("Hamsi ya da istavrit buğulama, bol salata", 450),
            ("Az kıymalı ıspanak yemeği, 1 kase yoğurt", 400),
            ("Etli türlü, 1 kase yoğurt", 450),
            ("Fırında tavuk göğsü, fırın mevsim sebzeleri", 430),
            ("Zeytinyağlı pırasa, 1 kase yoğurt, 1 ince dilim tam buğday ekmeği", 380),
            ("6 adet mercimek köftesi, marul-maydanoz, 1 bardak ayran", 430),
        },
        ["Ara Öğün"] = new (string, int)[]
        {
            ("1 küçük elma + 10 çiğ badem", 170),
            ("1 kase yoğurt + tarçın", 120),
            ("1 armut", 100),
            ("2 ceviz içi + 2 kuru kayısı", 150),
            ("1 bardak ayran + 1 mandalina", 120),
            ("Havuç-salatalık çubukları + 2 yemek kaşığı humus", 130),
            ("1 portakal", 80),
            ("1 bardak kefir", 120),
            ("2 kuru incir + 5 fındık", 150),
            ("1 küçük muz", 100),
        },
    };

    public static readonly string[] Tips =
    {
        "Günde en az 2–2,5 litre su için; acıkınca önce bir bardak su.",
        "Tabağın yarısı sebze, çeyreği protein, çeyreği tahıl olsun.",
        "Şekerli içecek, beyaz ekmek ve kızartmayı azaltın.",
        "Yemeği yavaş yiyin; doyma hissi 20 dakikada gelir.",
        "Akşam yemeğini yatmadan en az 3 saat önce bitirin.",
        "Her gün en az 30 dakika yürüyün; Yürüme Sayar hedefinizi takip edin.",
        "Tuzu azaltın; baharat ve limonla tatlandırın.",
        "Haftada 0,5–1 kg kayıp sağlıklı bir hızdır.",
    };
}
