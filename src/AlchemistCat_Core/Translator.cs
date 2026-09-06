using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Глобальная система локализации на 3 языка для проекта "Алхимический Кот".
/// Обеспечивает перевод интерфейса, синглтон-структуру и поддержку языков для Яндекс Игр (RU, EN, TR).
/// </summary>
public class Translator : MonoBehaviour
{
    public static Translator Instance { get; private set; } // Статический синглтон переводчика игры
    private static int _languageID = 0; // Идентификатор языка (0 = RU, 1 = EN, 2 = TR)
    public static int LanguageID // Публичное свойство текущего языка
    {
        get { return _languageID; }
        set { _languageID = value; }
    }

    private static List<Transtable_Text> listId = new List<Transtable_Text>(); // Зарегистрированные текстовые элементы
    private static List<Transtable_Dropdown> listDropdowns = new List<Transtable_Dropdown>(); // Зарегистрированные выпадающие списки

    [Header("Шрифты Локализации")]
    public TMP_FontAsset defaultFont; // Стандартный шрифт
    public TMP_FontAsset chineseFont; // Шрифт для китайского языка
    public TMP_FontAsset koreanFont; // Шрифт для корейского языка

    [Header("Интервалы букв")]
    public float russianCharacterSpacing = -8f; // Межбуквенный интервал для кириллицы

    // Специфичные строки для Алхимического Кота
    private static string[][] LineText = // Таблица переводов строк интерфейса
    {
        // 0 - Russian
        new string[] {
            "Старт", "Продолжить", "Опции", "Разработчики", "Выход", "Звуки", "Музыка", "Инверсия", "Лаборатория", "Графика",
            "Разрешение", "Весь экран", "Язык", "Загрузка...", "Перезаписать сейв?", "Новая игра", "Сохранено", "Сброс", "Управление", "Назад",
            "Качество", "Рейтинг", "Магазин", "Привет, Кот-Алхимик!", "Слот ", "Выбор сохранения", "Перезапись", "(Пусто)",
            "Мяу! Начнем варку?", "Мыши пойманы!", "Улучшить котел", "Книга рецептов", "Поймать мышь", "Играть в Дартс", "Далее", "Сохранить", "Мышей в амбаре: ",
            "Очень Низкое", "Низкое", "Среднее", "Высокое", "Очень Высокое", "Ультра",
            "Вы уверены?", "Да", "Нет", "Загрузить", "Меню кота", "Золото: ", "Кристаллы: ", "Уровень Кота: ", "Зелья: ", "Настройки",
            "Гардероб Аватарок", "Ур. ", "Выбрано", "Надеть", "Категория Аватарок", "Простые Аватарки (до 100 Ур.)", "Покупные Аватарки (с 5 Ур.)", "Премиум Аватарки (с 3 Ур.)", "Рамки Профиля", "С 5 Ур.", "С 3 Ур.", "Закрыто"
        },
        // 1 - English
        new string[] {
            "Start", "Continue", "Options", "Credits", "Exit", "Sounds", "Music", "Inversion", "Laboratory", "Graphics",
            "Resolution", "Full Screen", "Language", "Loading...", "Overwrite save?", "New Game", "Saved", "Reset", "Controls", "Back",
            "Quality", "Rating", "Shop", "Welcome, Alchemist Cat!", "Slot ", "Select Save Slot", "Overwrite", "(Empty)",
            "Meow! Start brewing?", "Mice caught!", "Upgrade Cauldron", "Recipe Book", "Catch Mice", "Play Darts", "Continue", "Save", "Mice in Barn: ",
            "Very Low", "Low", "Medium", "High", "Very High", "Ultra",
            "Are you sure?", "Yes", "No", "Load", "Cat Menu", "Gold: ", "Crystals: ", "Cat Level: ", "Potions: ", "Settings",
            "Avatar Wardrobe", "Lvl. ", "Selected", "Equip", "Avatar Category", "Free Avatars (up to Lvl 100)", "Shop Avatars (from Lvl 5)", "Premium Avatars (from Lvl 3)", "Profile Frames", "From Lvl 5", "From Lvl 3", "Locked"
        },
        // 2 - Turkish (TR) - Идеально для Яндекс Игр!
        new string[] {
            "Başlat", "Devam Et", "Seçenekler", "Yapımcılar", "Çıkış", "Sesler", "Müzik", "Ters Çevir", "Laboratuvar", "Grafik",
            "Çözünürlük", "Tam Ekran", "Dil", "Yükleniyor...", "Kayıt üzerine yazılsın mı?", "Yeni Oyun", "Kaydedildi", "Sıfırla", "Kontroller", "Geri",
            "Kalite", "Derecelendirme", "Mağaza", "Hoş geldin, Simyacı Kedi!", "Yuva ", "Kayıt Yuvası Seç", "Üzerine Yaz", "(Boş)",
            "Miyav! İksir yapmaya başla?", "Fareler yakalandı!", "Kazanı Geliştir", "Tarif Kitabı", "Fare Yakala", "Dart Oyna", "Devam Et", "Kaydet", "Barn'daki Fareler: ",
            "Çok Düşük", "Düşük", "Orta", "Yüksek", "Çok Yüksek", "Ultra",
            "Emin misiniz?", "Evet", "Hayır", "Yükle", "Kedi Menüsü", "Altın: ", "Kristaller: ", "Kedi Seviyesi: ", "İksirler: ", "Ayarlar",
            "Avatar Gardırobu", "Seviye ", "Seçildi", "Kuşan", "Avatar Kategorisi", "Ücretsiz Avatarlar (100 Seviyeye Kadar)", "Mağaza Avatarları (5. Seviyeden)", "Premium Avatarlar (3. Seviyeden)", "Profil Çerçeveleri", "5. Seviyeden", "3. Seviyeden", "Kilitli"
        }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            if (gameObject.name != "ALCHEMIST_TRANSLATOR")
            {
                GameObject translatorObject = new GameObject("ALCHEMIST_TRANSLATOR"); // Создание персистентного объекта локализации
                Translator customTranslator = translatorObject.AddComponent<Translator>();
                
                customTranslator.defaultFont = this.defaultFont;
                customTranslator.chineseFont = this.chineseFont;
                customTranslator.koreanFont = this.koreanFont;
                customTranslator.russianCharacterSpacing = this.russianCharacterSpacing;
                
                Instance = customTranslator; // Присвоение синглтона
                DontDestroyOnLoad(translatorObject); // Сохранение при смене сцен
                
                _languageID = PlayerPrefs.GetInt("Alchemist_Language", 0); // Загрузка сохраненного языка
                Update_texts(); // Обновление текста на экране
                
                Destroy(this); // Уничтожение временного компонента
                return;
            }

            Instance = this; // Инициализация прямого инстанса
            _languageID = PlayerPrefs.GetInt("Alchemist_Language", 0); // Загрузка языка из настроек
            Update_texts(); // Обновление всех текстов
        }
        else if (Instance != this)
        {
            Instance.defaultFont = this.defaultFont; // Перенос стандартного шрифта
            Instance.chineseFont = this.chineseFont; // Перенос китайского шрифта
            Instance.koreanFont = this.koreanFont; // Перенос корейского шрифта
            Instance.russianCharacterSpacing = this.russianCharacterSpacing; // Перенос межбуквенного интервала
            Update_texts(); // Синхронизация текстов
            Destroy(this); // Уничтожение дубликата
        }
    }

    public static void SelectLanguage(int id) // Метод выбора языка интерфейса
    {
        _languageID = id; // Установка нового ID языка
        PlayerPrefs.SetInt("Alchemist_Language", _languageID); // Сохранение выбора в PlayerPrefs
        Update_texts(); // Обновление всех текстов в активной сцене
    }

    public static string GetText(int textKey) // Получение строки по ее уникальному ID
    {
        int lang = _languageID; // Считывание текущего языка
        if (lang < 0 || lang >= LineText.Length) lang = 1; // Защита от выхода за пределы массива (по умолчанию EN)

        if (textKey >= 0 && textKey < LineText[lang].Length) // Проверка диапазона ключа строки
        {
            return LineText[lang][textKey]; // Возврат переведенного текста
        }
        return "ID:" + textKey; // Заглушка при отсутствии ключа
    }

    public static string GetText9(string ru, string en, string de, string fr, string es, string pt, string ja, string ko, string zh) // Мультиязычный маппер текста
    {
        switch (_languageID) // Выбор языка по ID
        {
            case 0: return ru; // Русский язык
            case 1: return en; // Английский язык
            case 2: return en; // Турецкий мапим на английский для сторонних скриптов без TR локали
            default: return en; // Английский по умолчанию
        }
    }

    public static void Add(Transtable_Text idtext) { if (!listId.Contains(idtext)) listId.Add(idtext); } // Регистрация текстового элемента
    public static void Delete(Transtable_Text idtext) { listId.Remove(idtext); } // Удаление текстового элемента из реестра
    public static void AddDropdown(Transtable_Dropdown dd) { if (!listDropdowns.Contains(dd)) listDropdowns.Add(dd); } // Регистрация выпадающего списка
    public static void DeleteDropdown(Transtable_Dropdown dd) { listDropdowns.Remove(dd); } // Удаление выпадающего списка из реестра

    public static void FormatText(Transtable_Text text) // Форматирование и применение параметров шрифта к тексту
    {
        if (text == null || text.UIText == null) return; // Проверка валидности ссылки
        
        text.UIText.text = GetText(text.TextID); // Установка переведенного текста
        text.UIText.characterSpacing = 0f; // Межбуквенный интервал
        text.UIText.wordSpacing = 0f; // Межсловный интервал
        text.UIText.lineSpacing = 0f; // Межстрочный интервал
        text.UIText.textWrappingMode = TextWrappingModes.NoWrap; // Отключение переноса строк

        // Если включена опция boldForRussian и активный язык русский, делаем текст жирным (Bold)
        if (text.boldForRussian && _languageID == 0) // Проверка жирного шрифта для RU
        {
            text.UIText.fontStyle = FontStyles.Bold; // Жирное начертание
        }
        else
        {
            text.UIText.fontStyle = FontStyles.Normal; // Обычное начертание
        }

        if (Instance == null) return; // Выход при отсутствии инстанса

        if (_languageID == 7) // Корейский язык
        {
            if (Instance.koreanFont != null) text.UIText.font = Instance.koreanFont; // Назначение корейского шрифта
        }
        else if (_languageID == 8 || _languageID == 6) // Китайский / Японский
        {
            if (Instance.chineseFont != null) text.UIText.font = Instance.chineseFont; // Назначение азиатского шрифта
        }
        else // Европейские языки
        {
            if (text.originalFont != null) text.UIText.font = text.originalFont; // Исходный шрифт компонента
            else if (Instance.defaultFont != null) text.UIText.font = Instance.defaultFont; // Базовый шрифт игры

            if (_languageID == 0) // Для русского языка
            {
                text.UIText.characterSpacing = 0f; // Сброс смещения
            }
        }
    }

    public static void Update_texts() // Обновление всех зарегистрированных текстовых элементов и списков
    {
        if (Instance == null) return; // Проверка готовности переводчика
        foreach (var text in listId) // Перебор всех текстовых полей
        {
            if (text != null) FormatText(text); // Форматирование и перевод текста
        }
        foreach (var dd in listDropdowns) // Перебор выпадающих списков
        {
            if (dd != null) dd.UpdateDropdown(); // Перевод опций выпадающего списка
        }
    }

    public static void TranslateAll() // Глобальный перевод всего интерфейса
    {
        Update_texts(); // Вызов перерисовки текстов
    }
}
