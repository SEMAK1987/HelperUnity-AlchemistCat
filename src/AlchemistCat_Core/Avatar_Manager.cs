using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core v18.12.21)
/// Менеджер Аватарок, Рамок и Профиля Игрока с поддержкой Локализации (RU / EN / TR):
/// - 5 Рамок Профиля (1 стартовая, 3 магазинные с 5 ур., 1 донатная с 3 ур.)
/// - 26 Аватарок (до 100 уровня, 5 покупных за Золото, 5 премиум за Кристаллы)
/// - Автоматический перевод через Translator.GetText(ID)
/// - 4-цветный градиент полоски опыта (Белый -> Зеленый -> Оранжевый -> Красный)
/// </summary>
public class Avatar_Manager : MonoBehaviour
{
    public static Avatar_Manager Instance { get; private set; }

    [Header("UI Панель Аватарок и Рамок")]
    public GameObject avatarPanel; // Главная всплывающая панель гардероба аватарок и рамок
    public Button closeButton; // Кнопка с крестиком для закрытия окна гардероба
    public Transform scrollContent; // Внутренний контейнер списка (Content в ScrollRect)
    public GameObject avatarItemPrefab; // Префаб отдельной ячейки с аватаркой / рамкой
    public GameObject categoryHeaderPrefab; // Префаб разделительного заголовка категории

    [Header("Настройки Сетки Гардероба")]
    public int columnsCount = 3; // Количество колонок в сетке гардероба
    public Vector2 cellSize = new Vector2(145, 170); // Размер одной ячейки гардероба (ширина, высота)
    public Vector2 cellSpacing = new Vector2(16, 16); // Отступы между ячейками по X и Y
    public Vector2 panelSize = new Vector2(620, 840); // Размер окна гардероба

    [Header("Адаптивная Настройка Гардероба")]
    public bool autoAdaptResolution = false; // Отключено принудительное растяжение, чтобы не ломать верстку в 4K и на телефонах
    public int pcColumnsCount = 3; // Количество колонок на мониторах ПК
    public Vector2 pcPanelSize = new Vector2(620, 840); // Размеры окна для ПК версии
    public int mobileColumnsCount = 3; // Количество колонок на мобильных экранах
    public Vector2 mobilePanelSize = new Vector2(620, 840); // Размеры окна для мобильных устройств

    [Header("Настройки Цветов Гардероба (Легко настраивать в Инспекторе)")]
    public Color categoryHeaderColor = new Color(1f, 0.92f, 0.45f, 1f); // #FFEBA3 Яркий золотой цвет заголовков
    public Color selectedStatusColor = new Color(0.3f, 1f, 0.75f, 1f);   // #4DFFBF Изумрудно-зеленый цвет для выбранного предмета
    public Color wearStatusColor = new Color(1f, 0.95f, 0.4f, 1f);       // #FFF266 Золотой цвет кнопки "Надеть"
    public Color levelLockedColor = new Color(1f, 0.45f, 0.55f, 1f);     // #FF738C Розово-красный цвет для заблокированных предметов
    public Color shopGoldPriceColor = new Color(1f, 0.85f, 0.2f, 1f);    // #FFD933 Золотой цвет ценников за монеты
    public Color premiumCrystalColor = new Color(0.95f, 0.5f, 1f, 1f);   // #F280FF Пурпурный цвет ценников за кристаллы
    public Color cellBackgroundColor = new Color(0.12f, 0.11f, 0.18f, 0.85f); // Темный контрастный фон ячейки

    [Header("Иконка Профиля в верхнем левом углу")]
    public Button avatarIconButton; // Кнопка-круг аватара в левом верхнем углу интерфейса
    public Image currentAvatarDisplayImage; // Слой с картинкой выбранного кота
    public Image currentFrameDisplayImage; // Слой с выбранной золотой/кристаллической рамкой
    public TextMeshProUGUI levelBadgeText; // Текстовый бейдж уровня (например "Ур. 1")
    public Image expProgressBar; // Полоска опыта кота (Image Type: Filled)
    public TextMeshProUGUI expProgressText; // Текст опыта кота (например "0/10 XP")

    [Header("Шкала Опыта Мастерства (Алхимический Ранг)")]
    public GameObject masteryContainer; // Родительский контейнер второй полоски алхимического ранга
    public TextMeshProUGUI masteryRankTitleText; // Название ранга ("Новичок", "Новичок-травник" и т.д.)
    public Image masteryExpProgressBar; // Полоска опыта мастерства (Filled Image)
    public TextMeshProUGUI masteryExpProgressText; // Текст опыта мастерства (например "0/100 XP")
    public Vector2 masteryBarPosition = new Vector2(130, -32); // Позиция второй полоски по X и Y
    public Vector2 masteryBarScale = new Vector2(1f, 0.85f); // Масштаб второй полоски
    public Color noviceTextColor = Color.white; // Цвет шрифта для начальных рангов
    public Color herbalistTextColor = new Color(0.32f, 0.75f, 0.50f, 1f); // Травянисто-зеленый цвет шрифта
    public AudioClip masteryRankUpSound; // Звуковой эффект при повышении алхимического ранга

    [System.Serializable]
    public enum AvatarCategory
    {
        Free,      // Простые и уровневые до 100 ур.
        Shop,      // Покупные за золото (с 5 ур.)
        Premium    // Премиум за кристаллы (с 3 ур.)
    }

    [System.Serializable]
    public class AvatarData
    {
        public int id;
        public string avatarNameRU;
        public string avatarNameEN;
        public string avatarNameTR;
        public AvatarCategory category;
        public Sprite avatarSprite;
        public bool isUnlockedByDefault = false;
        public int unlockLevelRequired = 0;
        public int goldPrice = 0;
        public int crystalPrice = 0;

        public string GetLocalizedName()
        {
            int lang = PlayerPrefs.GetInt("SelectedLanguage", 0);
            if (lang == 1 && !string.IsNullOrEmpty(avatarNameEN)) return avatarNameEN;
            if (lang == 2 && !string.IsNullOrEmpty(avatarNameTR)) return avatarNameTR;
            return string.IsNullOrEmpty(avatarNameRU) ? $"Avatar #{id}" : avatarNameRU;
        }
    }

    [System.Serializable]
    public class FrameData
    {
        public int id;
        public string frameNameRU;
        public string frameNameEN;
        public string frameNameTR;
        public Sprite frameSprite;
        public AvatarCategory category;
        public bool isUnlockedByDefault = false;
        public int unlockLevelRequired = 0;
        public int goldPrice = 0;
        public int crystalPrice = 0;

        public string GetLocalizedName()
        {
            int lang = PlayerPrefs.GetInt("SelectedLanguage", 0);
            if (lang == 1 && !string.IsNullOrEmpty(frameNameEN)) return frameNameEN;
            if (lang == 2 && !string.IsNullOrEmpty(frameNameTR)) return frameNameTR;
            return string.IsNullOrEmpty(frameNameRU) ? $"Frame #{id}" : frameNameRU;
        }
    }

    [Header("Позиции и Масштаб Элементов Профиля (Ручная и Автоматическая Калибровка)")]
    public bool autoAlignProfileOffsets = true;
    public Vector2 avatarRingPosition = new Vector2(50, -50); // Позиция кольца аватара
    public Vector2 avatarRingScale = new Vector2(1.2f, 1.2f); // Размер кольца аватара
    public Vector2 levelBadgePosition = new Vector2(130, 18);  // Сдвинуто ближе к кольцу
    public Vector2 expBarPosition = new Vector2(130, -4);      // Сдвинуто вплотную к кольцу аватара
    public Vector2 expBarScale = new Vector2(1f, 1f);         // Масштаб шкалы опыта
    public float levelTextFontSize = 24f;                     // Размер шрифта "Ур. 1"

    [Header("Коллекция Аватарок (До 100 Уровня)")]
    public List<AvatarData> allAvatars = new List<AvatarData>();

    [Header("Коллекция 14 Рамок Профиля")]
    public List<FrameData> allFrames = new List<FrameData>();

    [Header("Звуки")]
    public AudioClip selectSound;
    public AudioClip levelUpSound;

    // Опыт и Уровень Кота
    private int currentLevel = 1;
    private int currentExp = 0;
    private int maxExp = 10;
    private int selectedAvatarId = 0;
    private int selectedFrameId = 0;

    // Опыт Мастерства и Алхимический Ранг
    private int currentMasteryRankIndex = 0; // 0 = Новичок, 1 = Новичок-травник...
    private int currentMasteryExp = 0;
    private int maxMasteryExp = 100;

    private static readonly string[] MasteryRankNamesRU = new string[]
    {
        "Новичок", "Новичок-травник", "Подмастерье угля", "Экстрактор", "Знаток пропорций", "Сертифицированный ученик",
        "Практик масел", "Дистиллятор", "Мастер ферментации", "Каталитический химик", "Старший фармацевт",
        "Эфирный экспериментатор", "Кристаллограф", "Мастер трансмутации", "Вивисектор сущностей", "Архимагистр рецептуры",
        "Повелитель температур", "Ткач реальности", "Конструктор душ", "Хранитель Первоматерии", "Создатель Философского камня"
    };

    private static readonly int[] MasteryRankThresholds = new int[]
    {
        100, 300, 500, 1000, 1500, 3000,
        5000, 7000, 10000, 15000, 20000,
        25000, 30000, 37000, 45000, 60000,
        70000, 85000, 120000, 200000, 500000
    };

    private void Awake()
    {
        Instance = this; // Инициализация синглтона для глобального доступа

        // Корректировка позиций элементов интерфейса (смещение вправо от аватара)
        if (expBarPosition.x <= 0f) expBarPosition.x = 130f; // Позиция полоски опыта кота
        if (masteryBarPosition.x <= 0f) masteryBarPosition.x = 130f; // Позиция полоски опыта мастерства
        if (levelBadgePosition.x <= 0f) levelBadgePosition.x = 130f; // Позиция бейджа уровня

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners(); // Очистка старых событий кнопки закрытия
            closeButton.onClick.AddListener(CloseAvatarPanel); // Назначение закрытия панели гардероба
        }

        if (avatarIconButton != null)
        {
            avatarIconButton.onClick.RemoveAllListeners(); // Очистка старых событий кнопки аватара
            avatarIconButton.onClick.AddListener(OnAvatarIconClicked); // Назначение открытия панели гардероба
        }

        LoadPlayerProfile(); // Загрузка сохраненного прогресса профиля (уровень, опыт, выбранные облики)
        InitDefaultData(); // Инициализация начальной коллекции аватарок и рамок
    }

    private void Start()
    {
        UpdateProfileUI(); // Первичное обновление визуала аватара, рамки и полоски опыта
        UpdateMasteryUI(); // Первичное обновление ранга алхимика и второй шкалы мастерства
    }

    /// <summary>
    /// Расчет максимального опыта для текущего уровня: 
    /// Ур 1 = 10 XP, Ур 2 = 20 XP, Ур 3 = 30 XP ... Ур 100 = 1000 XP (Формула: Level * 10 XP)
    /// </summary>
    public static int GetMaxExpForLevel(int level) // Расчет порога опыта кота по формуле
    {
        return Mathf.Clamp(level, 1, 100) * 10; // Опыт = Уровень * 10 (макс 1000)
    }

    private void LoadPlayerProfile() // Загрузка сохраненных данных профиля игрока
    {
        currentLevel = PlayerPrefs.GetInt("Player_Level", 1); // Текущий уровень кота
        currentExp = PlayerPrefs.GetInt("Player_Exp", 0); // Текущий опыт кота
        maxExp = GetMaxExpForLevel(currentLevel); // Максимальный опыт для уровня
        selectedAvatarId = PlayerPrefs.GetInt("Selected_Avatar_Id", 0); // Выбранный ID аватарки
        selectedFrameId = PlayerPrefs.GetInt("Selected_Frame_Id", 0); // Выбранный ID рамки

        // Защита: стартовая выбранная аватарка по умолчанию должна быть 0 (бесплатная Стартовый Ученик #1)
        if (selectedAvatarId < 0 || selectedAvatarId > 2) // Если не из стартовых
        {
            if (PlayerPrefs.GetInt($"Avatar_Unlocked_{selectedAvatarId}", 0) != 1) // И не куплена
            {
                selectedAvatarId = 0; // Сброс на базовую
                PlayerPrefs.SetInt("Selected_Avatar_Id", 0); // Сохранение сброса
            }
        }

        if (selectedFrameId != 0) // Если выбрана не стартовая рамка
        {
            if (PlayerPrefs.GetInt($"Frame_Unlocked_{selectedFrameId}", 0) != 1) // И она не разблокирована
            {
                selectedFrameId = 0; // Сброс на стандартную рамку
                PlayerPrefs.SetInt("Selected_Frame_Id", 0); // Сохранение сброса
            }
        }

        currentMasteryRankIndex = PlayerPrefs.GetInt("Player_Mastery_Rank", 0); // Ранг алхимика
        currentMasteryExp = PlayerPrefs.GetInt("Player_Mastery_Exp", 0); // Опыт алхимика
        maxMasteryExp = GetMaxExpForMasteryRank(currentMasteryRankIndex); // Порог опыта для текущего ранга
    }

    public static int GetMaxExpForMasteryRank(int rankIdx) // Порог опыта мастерства алхимии
    {
        if (rankIdx >= 0 && rankIdx < MasteryRankThresholds.Length) // Проверка диапазона
            return MasteryRankThresholds[rankIdx]; // Возврат значения из таблицы
        return 100; // Значение по умолчанию
    }

    public void AddExperience(int amount) // Начисление опыта коту и повышение уровня
    {
        currentExp += amount; // Прибавление опыта
        while (currentLevel < 100 && currentExp >= maxExp) // Проверка достижения нового уровня
        {
            currentExp -= maxExp; // Списание опыта текущего уровня
            currentLevel++; // Повышение уровня кота
            maxExp = GetMaxExpForLevel(currentLevel); // Расчет порога следующего уровня

            if (levelUpSound != null && SettingsManager.Instance != null) // Если звук назначен
                SettingsManager.Instance.PlaySoundEffect(levelUpSound); // Воспроизведение звука левелапа
        }

        if (currentLevel >= 100) // Ограничение максимального 100 уровня
        {
            currentLevel = 100; // Фиксация 100 уровня
            maxExp = GetMaxExpForLevel(100); // Фиксация макс. опыта
            if (currentExp > maxExp) currentExp = maxExp; // Ограничение переполнения
        }

        PlayerPrefs.SetInt("Player_Level", currentLevel); // Сохранение уровня
        PlayerPrefs.SetInt("Player_Exp", currentExp); // Сохранение опыта
        PlayerPrefs.SetInt("Player_MaxExp", maxExp); // Сохранение макс. опыта
        PlayerPrefs.Save(); // Запись на диск

        UpdateProfileUI(); // Обновление UI плашки профиля
    }

    public void AddGold(int amount) // Начисление монет золота
    {
        if (GameManager.Instance != null) // Если есть игровой менеджер
        {
            GameManager.Instance.AddGold(amount); // Начисление через менеджер
        }
        else
        {
            int current = PlayerPrefs.GetInt("Player_Gold", 5000); // Чтение текущего запаса
            PlayerPrefs.SetInt("Player_Gold", current + amount); // Сохранение нового запаса
            PlayerPrefs.Save(); // Запись на диск
        }
    }

    public void AddStones(int amount) // Начисление камней
    {
        if (GameManager.Instance != null) // Если есть менеджер игры
        {
            GameManager.Instance.AddResources(0, amount, 0, 0); // Начисление ресурса
        }
        else
        {
            int current = PlayerPrefs.GetInt("Player_Stones", 10); // Чтение текущего запаса
            PlayerPrefs.SetInt("Player_Stones", current + amount); // Прибавление камней
            PlayerPrefs.Save(); // Запись на диск
        }
    }

    public void AddScrolls(int amount) // Начисление свитков
    {
        if (GameManager.Instance != null) // Если есть менеджер игры
        {
            GameManager.Instance.AddResources(0, 0, amount, 0); // Начисление ресурса
        }
        else
        {
            int current = PlayerPrefs.GetInt("Player_Scrolls", 3); // Чтение свитков
            PlayerPrefs.SetInt("Player_Scrolls", current + amount); // Прибавление свитков
            PlayerPrefs.Save(); // Запись на диск
        }
    }

    public void AddCrystals(int amount) // Начисление премиум кристаллов
    {
        if (GameManager.Instance != null) // Если менеджер активен
        {
            GameManager.Instance.AddCrystals(amount); // Начисление кристаллов
        }
        else
        {
            int current = PlayerPrefs.GetInt("Player_Crystals", 0); // Чтение текущих кристаллов
            PlayerPrefs.SetInt("Player_Crystals", current + amount); // Сохранение с кристаллами
            PlayerPrefs.Save(); // Запись на диск
        }
    }

    public void GainPlayerExperience(int amount) // Обертка для получения опыта кота
    {
        AddExperience(amount); // Вызов основного метода начисления
    }

    public void AddMasteryExperience(int amount) // Начисление опыта мастерства алхимика
    {
        currentMasteryExp += amount; // Прибавление очков мастерства
        maxMasteryExp = GetMaxExpForMasteryRank(currentMasteryRankIndex); // Обновление порога

        while (currentMasteryRankIndex < MasteryRankThresholds.Length - 1 && currentMasteryExp >= maxMasteryExp) // Проверка ранга
        {
            currentMasteryExp -= maxMasteryExp; // Списание опыта
            currentMasteryRankIndex++; // Повышение алхимического ранга
            maxMasteryExp = GetMaxExpForMasteryRank(currentMasteryRankIndex); // Расчет нового порога

            if (masteryRankUpSound != null && SettingsManager.Instance != null) // Звук повышения ранга
                SettingsManager.Instance.PlaySoundEffect(masteryRankUpSound); // Воспроизведение звука
            else if (levelUpSound != null && SettingsManager.Instance != null) // Запасной звук
                SettingsManager.Instance.PlaySoundEffect(levelUpSound); // Воспроизведение
        }

        PlayerPrefs.SetInt("Player_Mastery_Rank", currentMasteryRankIndex); // Сохранение ранга
        PlayerPrefs.SetInt("Player_Mastery_Exp", currentMasteryExp); // Сохранение опыта
        PlayerPrefs.Save(); // Запись на диск

        UpdateMasteryUI(); // Обновление второй шкалы интерфейса
    }

    [ContextMenu("Сбросить Прогресс Профиля и Мастерства (Reset Profile & Mastery)")]
    public void ResetProfileAndMasteryProgress() // Метод сброса профиля для тестирования
    {
        PlayerPrefs.DeleteKey("Player_Level"); // Удаление ключа уровня
        PlayerPrefs.DeleteKey("Player_Exp"); // Удаление ключа опыта
        PlayerPrefs.DeleteKey("Player_MaxExp"); // Удаление макс. опыта
        PlayerPrefs.DeleteKey("Player_Mastery_Rank"); // Удаление ранга мастерства
        PlayerPrefs.DeleteKey("Player_Mastery_Exp"); // Удаление опыта мастерства
        PlayerPrefs.DeleteKey("Mastery_Flask_Consumed"); // Удаление статуса выпитой колбы
        PlayerPrefs.DeleteKey("Tutorial_Avatar_Chosen"); // Удаление флага выбора аватара
        PlayerPrefs.DeleteKey("Selected_Avatar_Id"); // Удаление выбранного аватара
        PlayerPrefs.DeleteKey("Selected_Frame_Id"); // Удаление выбранной рамки
        PlayerPrefs.Save(); // Сохранение изменений

        currentLevel = 1; // Уровень 1
        currentExp = 0; // Опыт 0
        maxExp = 10; // Порог 10
        selectedAvatarId = 0; // Аватарка #0
        selectedFrameId = 0; // Рамка #0
        currentMasteryRankIndex = 0; // Ранг Новичок
        currentMasteryExp = 0; // Опыт 0
        maxMasteryExp = 100; // Порог 100

        UpdateProfileUI(); // Перерисовка профиля
        UpdateMasteryUI(); // Перерисовка шкалы мастерства
        Debug.Log("[Avatar_Manager] Профиль и мастерство успешно сброшены к начальному состоянию!"); // Лог сброса
    }

    public void UpdateMasteryUI() // Обновление текста ранга и шкалы мастерства
    {
        AutoSanitizeMasteryBarLayout(); // Авто-выравнивание позиции элементов

        string rankTitle = currentMasteryRankIndex < MasteryRankNamesRU.Length ? MasteryRankNamesRU[currentMasteryRankIndex] : "Новичок"; // Получение названия ранга
        if (masteryRankTitleText != null) // Текст ранга
        {
            masteryRankTitleText.text = rankTitle; // Запись названия
            if (currentMasteryRankIndex == 0) // Начальный ранг
            {
                masteryRankTitleText.color = noviceTextColor; // Белый #FFFFFF
            }
            else // Продвинутые ранги
            {
                masteryRankTitleText.color = herbalistTextColor; // Травянисто-зеленый #52B788
            }
        }

        if (masteryExpProgressText != null) // Текст опыта
        {
            masteryExpProgressText.text = $"{currentMasteryExp}/{maxMasteryExp} XP"; // Формат X/Y XP
        }

        if (masteryExpProgressBar != null) // Заливка полоски
        {
            float fillRatio = maxMasteryExp > 0 ? Mathf.Clamp01((float)currentMasteryExp / maxMasteryExp) : 0f; // Доля заполнения (0..1)
            masteryExpProgressBar.fillAmount = fillRatio; // Применение к Image

            // Переливающийся изумрудно-бирюзовый градиент для мастерства
            if (fillRatio <= 0.01f) // Начало шкалы
                masteryExpProgressBar.color = new Color(0.9f, 0.95f, 0.9f, 1f); // Беловато-зеленый
            else if (fillRatio < 0.5f) // До половины
                masteryExpProgressBar.color = new Color(0.3f, 0.9f, 0.6f, 1f); // Травянисто-зеленый
            else // Ближе к максимуму
                masteryExpProgressBar.color = new Color(0.15f, 0.75f, 0.85f, 1f); // Бирюзово-магический
        }
    }

    /// <summary>
    /// Автоматическая юстировка шкалы мастерства и ее дочерних элементов (текст, заливка, название ранга)
    /// </summary>
    private void AutoSanitizeMasteryBarLayout() // Юстировка координат и размеров шкалы мастерства
    {
        RectTransform expBgRect = (expProgressBar != null && expProgressBar.transform.parent != null) 
            ? expProgressBar.transform.parent.GetComponent<RectTransform>() // Получение RectTransform фона шкалы кота
            : null;

        float baseX = expBgRect != null ? expBgRect.anchoredPosition.x : 130f; // Базовая координата X
        float baseY = expBgRect != null ? expBgRect.anchoredPosition.y : -4f; // Базовая координата Y
        Vector2 baseSize = expBgRect != null ? expBgRect.sizeDelta : new Vector2(130f, 18f); // Базовый размер полоски

        // 1. Контейнер / Фон шкалы мастерства (Mastery_Exp_Bar_Background)
        Transform masteryBarTransform = masteryExpProgressBar != null ? masteryExpProgressBar.transform.parent : null; // Родительский объект полоски
        if (masteryBarTransform == null && masteryContainer != null) masteryBarTransform = masteryContainer.transform; // Запасной контейнер

        RectTransform masteryBarBg = masteryBarTransform != null ? masteryBarTransform.GetComponent<RectTransform>() : null; // RectTransform фона мастерства
        if (masteryBarBg != null) // Если фон существует
        {
            if (masteryBarBg.parent != null && (masteryBarBg.parent.name.Contains("Container") || masteryBarBg.parent.name.Contains("Avatar"))) // Проверка иерархии
            {
                masteryBarBg.sizeDelta = baseSize; // Установка одинакового размера
                masteryBarBg.anchoredPosition = new Vector2(baseX, baseY - 32f); // Располагается ровно под первой полоской
                masteryBarBg.localScale = Vector3.one; // Нормализация масштаба (1,1,1)
            }
        }

        // 2. Заливка шкалы мастерства (Mastery_Exp_Fill)
        if (masteryExpProgressBar != null) // Проверка компонента шкалы
        {
            RectTransform fillRect = masteryExpProgressBar.GetComponent<RectTransform>(); // RectTransform заливки
            if (fillRect != null) // Если найден
            {
                fillRect.anchorMin = Vector2.zero; // Растяжение по родителю min
                fillRect.anchorMax = Vector2.one; // Растяжение по родителю max
                fillRect.offsetMin = Vector2.zero; // Нулевой отступ слева-снизу
                fillRect.offsetMax = Vector2.zero; // Нулевой отступ справа-сверху
                fillRect.pivot = new Vector2(0.5f, 0.5f); // Центральный пивот
                fillRect.localScale = Vector3.one; // Масштаб 1
            }
        }

        // 3. Текст опыта ("0/100 XP" / "0/300 XP")
        if (masteryExpProgressText != null) // Текстовый индикатор опыта
        {
            RectTransform textRect = masteryExpProgressText.GetComponent<RectTransform>(); // RectTransform текста
            if (textRect != null) // Если найден
            {
                textRect.anchorMin = Vector2.zero; // Привязка по центру/родителю
                textRect.anchorMax = Vector2.one; // Привязка
                textRect.offsetMin = Vector2.zero; // Отступ
                textRect.offsetMax = Vector2.zero; // Отступ
                textRect.anchoredPosition = Vector2.zero; // Центрирование
                textRect.localScale = Vector3.one; // Нормальный масштаб
            }
            masteryExpProgressText.alignment = TextAlignmentOptions.Center; // Выравнивание по центру
        }

        // 4. Текст названия ранга ("Новичок" / "Новичок-травник")
        if (masteryRankTitleText != null) // Текст ранга
        {
            RectTransform titleRect = masteryRankTitleText.GetComponent<RectTransform>(); // RectTransform заголовка ранга
            if (titleRect != null) // Если найден
            {
                Vector2 titleSize = new Vector2(180f, 22f); // Размер области текста
                if (masteryBarBg != null && titleRect.IsChildOf(masteryBarBg)) // Если ребенок фона
                {
                    titleRect.anchoredPosition = new Vector2(0f, 18f); // Прямо над второй полоской
                    titleRect.sizeDelta = titleSize; // Установка габаритов
                }
                else // Если на одном уровне
                {
                    titleRect.anchoredPosition = new Vector2(baseX, baseY - 16f); // Смещение по Y
                    titleRect.sizeDelta = titleSize; // Установка габаритов
                }
                titleRect.localScale = Vector3.one; // Масштаб 1
            }
            masteryRankTitleText.alignment = TextAlignmentOptions.Center; // Выравнивание по центру
        }
    }

    public void UpdateProfileUI() // Обновление всей плашки профиля игрока в левом углу
    {
        // Автоматическое позиционирование кольца аватара, уровня и шкал опыта
        if (autoAlignProfileOffsets) // Если авто-выравнивание включено
        {
            if (avatarIconButton != null) // Кнопка аватара
            {
                RectTransform ringRect = avatarIconButton.GetComponent<RectTransform>(); // RectTransform кнопки
                if (ringRect != null) // Если найден
                {
                    ringRect.anchoredPosition = avatarRingPosition; // Установка позиции
                    ringRect.localScale = new Vector3(avatarRingScale.x, avatarRingScale.y, 1f); // Установка масштаба
                }
            }

            RectTransform expBgRect = (expProgressBar != null && expProgressBar.transform.parent != null) 
                ? expProgressBar.transform.parent.GetComponent<RectTransform>() // Фон полоски опыта
                : null;

            if (expBgRect != null) // Если найден
            {
                expBgRect.anchoredPosition = expBarPosition; // Позиция полоски опыта
                expBgRect.localScale = new Vector3(expBarScale.x, expBarScale.y, 1f); // Масштаб полоски опыта

                if (levelBadgeText != null) // Текст бейджа уровня
                {
                    RectTransform lvlRect = levelBadgeText.GetComponent<RectTransform>(); // RectTransform бейджа
                    if (lvlRect != null) // Если найден
                    {
                        lvlRect.anchoredPosition = new Vector2(expBgRect.anchoredPosition.x, expBgRect.anchoredPosition.y + 22f); // Смещение над полоской
                    }
                    levelBadgeText.fontSize = levelTextFontSize; // Размер шрифта
                }
            }

            AutoSanitizeMasteryBarLayout(); // Подгонка второй шкалы мастерства
        }

        string lvlPrefix = Translator.GetText(54); // Префикс "Ур. " / "Lvl. "
        if (levelBadgeText != null) // Бейдж уровня
        {
            levelBadgeText.text = $"{lvlPrefix}{currentLevel}"; // Вывод текста уровня
        }

        if (expProgressText != null) // Текст опыта
        {
            expProgressText.text = $"{currentExp}/{maxExp} XP"; // Формат X/Y XP
        }

        if (expProgressBar != null) // Полоска опыта
        {
            float fillRatio = maxExp > 0 ? Mathf.Clamp01((float)currentExp / maxExp) : 0f; // Соотношение опыта (0..1)
            expProgressBar.fillAmount = fillRatio; // Применение к Image

            // 4-цветный градиент: Белый -> Зеленый -> Оранжевый -> Красный
            if (fillRatio <= 0.01f) // 0%
                expProgressBar.color = new Color(0.95f, 0.95f, 0.95f, 1f); // Белый
            else if (fillRatio < 0.45f) // До 45%
                expProgressBar.color = new Color(0.2f, 0.85f, 0.35f, 1f); // Зеленый
            else if (fillRatio < 0.85f) // До 85%
                expProgressBar.color = new Color(1f, 0.65f, 0.1f, 1f);   // Оранжевый
            else // Выше 85%
                expProgressBar.color = new Color(0.95f, 0.2f, 0.2f, 1f);  // Красный
        }

        if (currentAvatarDisplayImage != null) // Картинка кота в кольце
        {
            if (allAvatars.Count > 0) // Если список инициализирован
            {
                AvatarData cur = allAvatars.Find(a => a.id == selectedAvatarId); // Поиск выбранного аватара
                if (cur != null && cur.avatarSprite != null) // Если спрайт задан
                {
                    currentAvatarDisplayImage.sprite = cur.avatarSprite; // Установка спрайта
                    currentAvatarDisplayImage.enabled = true; // Включение отображения
                    currentAvatarDisplayImage.color = Color.white; // Белый цвет
                }
                else if (allAvatars[0].avatarSprite != null) // Запасной начальный спрайт
                {
                    currentAvatarDisplayImage.sprite = allAvatars[0].avatarSprite; // Установка спрайта по умолчанию
                    currentAvatarDisplayImage.enabled = true; // Включение отображения
                    currentAvatarDisplayImage.color = Color.white; // Белый цвет
                }
            }
            else // Если список пуст
            {
                currentAvatarDisplayImage.enabled = (currentAvatarDisplayImage.sprite != null); // Проверка наличия спрайта
                if (currentAvatarDisplayImage.enabled) currentAvatarDisplayImage.color = Color.white; // Белый цвет
            }
        }

        if (currentFrameDisplayImage != null) // Картинка рамки профиля
        {
            if (allFrames.Count > 0) // Если список рамок есть
            {
                FrameData curF = allFrames.Find(f => f.id == selectedFrameId); // Поиск выбранной рамки
                if (curF != null && curF.frameSprite != null) // Если спрайт рамки задан
                {
                    currentFrameDisplayImage.sprite = curF.frameSprite; // Установка спрайта рамки
                    currentFrameDisplayImage.enabled = true; // Включение рамки
                    currentFrameDisplayImage.color = Color.white; // Белый цвет
                }
            }
        }
    }

    public void SetAvatarButtonInteractable(bool interactable) // Включение/отключение кликабельности кнопки аватара
    {
        if (avatarIconButton != null) // Если кнопка существует
        {
            avatarIconButton.interactable = interactable; // Установка свойства interactable
        }
    }

    public void OnAvatarIconClicked() // Обработка клика по аватарке игрока
    {
        if (avatarPanel != null && avatarPanel.activeSelf) // Если гардероб уже открыт
        {
            return; // Выход
        }

        if (DialogueSystem_Manager.Instance != null && !DialogueSystem_Manager.Instance.CanInteractWithAvatarIcon()) // Проверка блокировки туториалом
        {
            return; // Блокировка открытия во время диалога
        }
        OpenAvatarPanel(); // Открытие окна гардероба
    }

    public void OpenAvatarPanel() // Открытие всплывающей панели гардероба
    {
        if (avatarPanel != null) // Если панель назначена
        {
            avatarPanel.SetActive(true); // Активация окна

            bool isAvatarChosen = PlayerPrefs.GetInt("Tutorial_Avatar_Chosen", 0) == 1; // Проверка прохождения выбора
            if (closeButton != null) // Кнопка крестика
            {
                closeButton.interactable = isAvatarChosen; // Активна только после выбора
                closeButton.gameObject.SetActive(isAvatarChosen); // Видимость крестика
            }

            if (autoAdaptResolution) // Адаптация размеров окна
            {
                RectTransform panelRect = avatarPanel.GetComponent<RectTransform>(); // RectTransform панели
                if (panelRect != null && panelSize.x > 0 && panelSize.y > 0) // Проверка корректности размеров
                {
                    panelRect.sizeDelta = panelSize; // Установка размеров
                }
            }

            BuildAvatarGrid(); // Генерация элементов гардероба
            StartCoroutine(ResetScrollToTopRoutine()); // Прокрутка списка наверх
        }
    }

    private System.Collections.IEnumerator ResetScrollToTopRoutine() // Корутина сброса скролла к началу
    {
        yield return null; // Ожидание кадра верстки
        Canvas.ForceUpdateCanvases(); // Принудительный пересчет позиций

        ScrollRect sr = avatarPanel != null ? avatarPanel.GetComponentInChildren<ScrollRect>() : null; // Поиск ScrollRect
        if (sr != null) // Если найден
        {
            sr.verticalNormalizedPosition = 1f; // Скролл в самый верх (1.0)
            sr.velocity = Vector2.zero; // Обнуление скорости
        }

        if (scrollContent != null) // Контент скролла
        {
            RectTransform crt = scrollContent.GetComponent<RectTransform>(); // RectTransform контента
            if (crt != null) // Если существует
            {
                crt.anchoredPosition = new Vector2(crt.anchoredPosition.x, 0f); // Сброс позиции Y на 0
            }
        }
    }

    public void CloseAvatarPanel() // Закрытие всплывающей панели гардероба
    {
        bool isAvatarChosen = PlayerPrefs.GetInt("Tutorial_Avatar_Chosen", 0) == 1; // Выбран ли уже аватар
        if (!isAvatarChosen) // Если туториал не завершен
        {
            if (closeButton != null) closeButton.interactable = false; // Блокировка крестика
            return; // Запрет закрытия
        }

        if (avatarPanel != null) // Если панель существует
        {
            avatarPanel.SetActive(false); // Скрытие окна гардероба
        }

        if (DialogueSystem_Manager.Instance != null) // Уведомление менеджера диалогов
        {
            DialogueSystem_Manager.Instance.OnAvatarPanelClosed(); // Запуск следующего шага туториала
        }
    }

    private void InitDefaultData() // Инициализация стандартной базы данных рамок и аватарок
    {
        // 14 Рамок профиля (1 Бесплатная, 11 за Золото в магазине, 2 Премиум за Кристаллы)
        if (allFrames.Count == 0) // Если список пуст
        {
            // Базовые 7 рамок
            allFrames.Add(new FrameData { id = 0, frameNameRU = "Стартовая Рамка Ученика", frameNameEN = "Starter Apprentice Frame", frameNameTR = "Başlangıç Çırak Çerçevesi", category = AvatarCategory.Free, isUnlockedByDefault = true }); // Стартовая рамка #0
            allFrames.Add(new FrameData { id = 1, frameNameRU = "Медная Рамка Лавки", frameNameEN = "Copper Shop Frame", frameNameTR = "Bakır Dükkan Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 5, goldPrice = 3000 }); // Медная рамка лавки #1
            allFrames.Add(new FrameData { id = 2, frameNameRU = "Серебряная Рамка Мастера", frameNameEN = "Silver Master Frame", frameNameTR = "Gümüş Usta Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 5, goldPrice = 6000 }); // Серебряная рамка мастера #2
            allFrames.Add(new FrameData { id = 3, frameNameRU = "Золотая Рамка Алхимика", frameNameEN = "Golden Alchemist Frame", frameNameTR = "Altın Simyacı Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 5, goldPrice = 10000 }); // Золотая рамка алхимика #3
            allFrames.Add(new FrameData { id = 4, frameNameRU = "Королевская Изумрудная Рамка", frameNameEN = "Royal Emerald Frame", frameNameTR = "Kraliyet Zümrüt Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 10, goldPrice = 25000 }); // Изумрудная рамка #4
            allFrames.Add(new FrameData { id = 5, frameNameRU = "Астральная Донатная Рамка", frameNameEN = "Astral Premium Frame", frameNameTR = "Astral Özel Çerçeve", category = AvatarCategory.Premium, unlockLevelRequired = 3, crystalPrice = 50 }); // Астральная рамка #5
            allFrames.Add(new FrameData { id = 6, frameNameRU = "Божественная Солнечная Рамка", frameNameEN = "Divine Solar Frame", frameNameTR = "İlahi Güneş Çerçevesi", category = AvatarCategory.Premium, unlockLevelRequired = 5, crystalPrice = 100 }); // Солнечная рамка #6

            // 7 Дополнительных покупных рамок в Магазине (Shop)
            allFrames.Add(new FrameData { id = 7, frameNameRU = "Аметистовая Рамка Травника", frameNameEN = "Herbalist Amethyst Frame", frameNameTR = "Bitkici Ametist Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 6, goldPrice = 12000 }); // Аметистовая рамка #7
            allFrames.Add(new FrameData { id = 8, frameNameRU = "Рубиновая Рамка Пламени", frameNameEN = "Flame Ruby Frame", frameNameTR = "Alev Yakut Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 7, goldPrice = 15000 }); // Рубиновая рамка #8
            allFrames.Add(new FrameData { id = 9, frameNameRU = "Сапфировая Рамка Мороза", frameNameEN = "Frost Sapphire Frame", frameNameTR = "Buz Safir Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 8, goldPrice = 18000 }); // Сапфировая рамка #9
            allFrames.Add(new FrameData { id = 10, frameNameRU = "Нефритовая Рамка Друида", frameNameEN = "Druid Jade Frame", frameNameTR = "Druid Yeşim Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 9, goldPrice = 22000 }); // Нефритовая рамка #10
            allFrames.Add(new FrameData { id = 11, frameNameRU = "Обсидиановая Рамка Теней", frameNameEN = "Shadow Obsidian Frame", frameNameTR = "Gölge Obsidyen Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 11, goldPrice = 30000 }); // Обсидиановая рамка #11
            allFrames.Add(new FrameData { id = 12, frameNameRU = "Небесная Лазурная Рамка", frameNameEN = "Celestial Azure Frame", frameNameTR = "Göksel Azur Çerçevesi", category = AvatarCategory.Shop, unlockLevelRequired = 13, goldPrice = 35000 }); // Лазурная рамка #12
            allFrames.Add(new FrameData { id = 13, frameNameRU = "Древняя Руническая Рамка", frameNameEN = "Ancient Runic Frame", frameNameTR = "Kadim Rünik Çerçeve", category = AvatarCategory.Shop, unlockLevelRequired = 15, goldPrice = 40000 }); // Руническая рамка #13
        }

        // Коллекция Аватарок
        if (allAvatars.Count == 0) // Если список аватарок пуст
        {
            // Стартовые и уровни 1..20 (16 штук)
            int[] earlyLevels = new int[] { 0, 0, 0, 2, 4, 6, 8, 10, 12, 14, 15, 16, 17, 18, 19, 20 }; // Уровни разблокировки начальных аватарок
            for (int i = 0; i < earlyLevels.Length; i++) // Цикл по начальным обликам
            {
                allAvatars.Add(new AvatarData // Создание записи аватарки
                {
                    id = i, // Уникальный ID
                    avatarNameRU = (i < 3) ? $"Стартовый Ученик #{i + 1}" : $"Мастер {earlyLevels[i]} Уровня", // Название на русском
                    category = AvatarCategory.Free, // Категория: бесплатная/по уровню
                    isUnlockedByDefault = (i < 3), // Первые 3 открыты сразу
                    unlockLevelRequired = earlyLevels[i] // Требуемый уровень
                });
            }

            // Гранд-Мастера: 30, 40, 50, 60, 70, 80, 90, 100 уровни (8 штук)
            int[] grandLevels = new int[] { 30, 40, 50, 60, 70, 80, 90, 100 }; // Пороги уровней для гранд-мастеров
            for (int i = 0; i < grandLevels.Length; i++) // Цикл по гранд-мастерам
            {
                allAvatars.Add(new AvatarData // Добавление записи
                {
                    id = 16 + i, // ID в списке
                    avatarNameRU = $"Гранд-Алхимик {grandLevels[i]} Уровня", // Имя гранд-алхимика
                    category = AvatarCategory.Free, // Категория: бесплатная по уровню
                    isUnlockedByDefault = false, // Закрыта до достижения уровня
                    unlockLevelRequired = grandLevels[i] // Требуемый уровень
                });
            }

            // 5 Покупных аватарок (Обычный магазин с 5 уровня)
            for (int i = 0; i < 5; i++) // Цикл магазинных аватарок
            {
                allAvatars.Add(new AvatarData // Добавление магазинной аватарки
                {
                    id = 24 + i, // ID аватарки
                    avatarNameRU = $"Мастер Лавки #{i + 1}", // Название аватарки
                    category = AvatarCategory.Shop, // Категория: Магазин
                    unlockLevelRequired = 5, // Доступна с 5 уровня
                    goldPrice = (i + 1) * 5000 // Цена в золоте (5k, 10k, 15k, 20k, 25k)
                });
            }

            // 5 Премиум аватарок (Премиум магазин с 3 уровня)
            for (int i = 0; i < 5; i++) // Цикл премиум аватарок
            {
                allAvatars.Add(new AvatarData // Добавление премиум аватарки
                {
                    id = 29 + i, // ID премиум аватарки
                    avatarNameRU = $"Астральный Архимаг #{i + 1}", // Название на русском
                    category = AvatarCategory.Premium, // Категория: Премиум
                    unlockLevelRequired = 3, // Доступна с 3 уровня
                    crystalPrice = (i + 1) * 20 // Цена в кристаллах (20, 40, 60, 80, 100)
                });
            }
        }
    }

    private void BuildAvatarGrid() // Генерация сетки карточек в гардеробе
    {
        if (scrollContent == null) return; // Если контейнер не найден

        foreach (Transform child in scrollContent) // Очистка старых элементов
        {
            Destroy(child.gameObject); // Удаление дочернего объекта
        }

        // Переводимые заголовки через Translator (ID 58, 59, 60)
        CreateCategorySection(Translator.GetText(58), AvatarCategory.Free); // Секция бесплатных аватарок
        CreateCategorySection(Translator.GetText(59), AvatarCategory.Shop); // Секция магазинных аватарок
        CreateCategorySection(Translator.GetText(60), AvatarCategory.Premium); // Секция премиум аватарок

        // Секция 14 Волшебных Рамок Профиля (без спецсимволов Юникода для предотвращения предупреждений TextMeshPro)
        CreateFramesSection("ВОЛШЕБНЫЕ РАМКИ ПРОФИЛЯ"); // Секция рамок
    }

    private void CreateCategorySection(string headerTitle, AvatarCategory cat) // Создание секции категории аватарок
    {
        if (categoryHeaderPrefab != null) // Если префаб заголовка назначен
        {
            GameObject headerObj = Instantiate(categoryHeaderPrefab, scrollContent); // Спавн заголовка категории
            TextMeshProUGUI txt = headerObj.GetComponentInChildren<TextMeshProUGUI>(); // Поиск текстового поля
            if (txt != null) // Если найден
            {
                txt.text = headerTitle; // Установка текста заголовка
                txt.color = categoryHeaderColor; // Установка золотистого цвета
            }
        }

        List<AvatarData> catList = allAvatars.FindAll(a => a.category == cat); // Выборка аватарок заданной категории
        if (catList.Count == 0) return; // Если список пуст, выходим

        // Создаем контейнер-сетку с GridLayoutGroup для размещения по 2-3 аватарки по горизонтали
        GameObject gridContainer = new GameObject($"GridSection_{cat}", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); // Создание сетки
        gridContainer.transform.SetParent(scrollContent, false); // Добавление в ScrollContent

        GridLayoutGroup grid = gridContainer.GetComponent<GridLayoutGroup>(); // Компонент сетки
        grid.cellSize = cellSize; // Размер ячейки
        grid.spacing = cellSpacing; // Отступы между ячейками
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // Фиксация числа колонок
        grid.constraintCount = Mathf.Max(1, columnsCount); // Число колонок (по умолчанию 3)
        grid.childAlignment = TextAnchor.UpperCenter; // Выравнивание по верхнему центру
        grid.padding = new RectOffset(8, 8, 8, 16); // Отступы от краев

        ContentSizeFitter fitter = gridContainer.GetComponent<ContentSizeFitter>(); // Авто-подгонка высоты
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // Без горизонтальной подгонки
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Подгонка по содержимому

        foreach (AvatarData data in catList) // Заполнение карточками
        {
            CreateAvatarCell(data, gridContainer.transform); // Спавн ячейки аватарки
        }
    }

    private void CreateAvatarCell(AvatarData data, Transform parentContainer) // Создание отдельной карточки аватарки
    {
        if (avatarItemPrefab == null) return; // Проверка префаба

        Transform targetParent = parentContainer != null ? parentContainer : scrollContent; // Выбор родителя
        GameObject cell = Instantiate(avatarItemPrefab, targetParent); // Спавн ячейки
        cell.name = $"Avatar_{data.id}"; // Именование объекта ячейки

        Image bgImg = cell.GetComponent<Image>(); // Фон ячейки
        if (bgImg != null) // Если компонент есть
        {
            bgImg.color = cellBackgroundColor; // Установка темного фона
        }

        Image iconImg = cell.transform.Find("Avatar_Icon")?.GetComponent<Image>(); // Слой иконки аватара
        Image frameImg = cell.transform.Find("Avatar_Frame")?.GetComponent<Image>(); // Слой рамки аватара
        GameObject lockObj = cell.transform.Find("Lock_Overlay")?.gameObject; // Слой замка блокировки
        TextMeshProUGUI statusText = cell.transform.Find("Status_Text")?.GetComponent<TextMeshProUGUI>(); // Текст статуса карточки
        Button cellBtn = cell.GetComponent<Button>(); // Кнопка клика по карточке

        // Отключаем лишнюю рамку на карточке аватарки, чтобы она не перекрывала изображение сверху
        if (frameImg != null) // Если объект рамки есть
        {
            frameImg.gameObject.SetActive(false); // Выключаем рамку на превью
        }

        bool isUnlocked = IsAvatarUnlocked(data); // Разблокирована ли аватарка
        bool isSelected = (selectedAvatarId == data.id); // Выбрана ли сейчас

        if (iconImg != null) // Если слой иконки найден
        {
            if (data.avatarSprite != null) // Если спрайт прикреплен
            {
                iconImg.sprite = data.avatarSprite; // Установка спрайта
                iconImg.color = Color.white; // Белый цвет без затемнения
                iconImg.enabled = true; // Включение отображения
            }
            else // Если спрайта нет
            {
                // Защита от белого квадрата: если спрайт еще не прикреплен, делаем темный полупрозрачный фон
                iconImg.sprite = null; // Обнуление спрайта
                iconImg.color = new Color(0.15f, 0.15f, 0.22f, 0.4f); // Полупрозрачная заглушка
            }
        }

        if (lockObj != null) // Если объект замка существует
        {
            lockObj.SetActive(!isUnlocked); // Замок активен, если аватарка заблокирована
            Image lockImg = lockObj.GetComponent<Image>(); // Image замка
            if (lockImg != null) // Если есть
            {
                lockImg.color = Color.white; // Белый цвет иконки замка
            }
        }

        if (statusText != null) // Текстовая надпись статуса
        {
            string hexSelected = ColorUtility.ToHtmlStringRGB(selectedStatusColor); // HEX цвет выбранного
            string hexWear = ColorUtility.ToHtmlStringRGB(wearStatusColor); // HEX цвет кнопки Надеть
            string hexLocked = ColorUtility.ToHtmlStringRGB(levelLockedColor); // HEX цвет блокировки
            string hexGold = ColorUtility.ToHtmlStringRGB(shopGoldPriceColor); // HEX цвет золотой цены
            string hexCrystal = ColorUtility.ToHtmlStringRGB(premiumCrystalColor); // HEX цвет кристаллов

            if (isSelected) // Если аватарка выбрана
            {
                statusText.text = $"<color=#{hexSelected}><b>{Translator.GetText(55)}</b></color>"; // Выбрано
            }
            else if (isUnlocked) // Если открыта, но не надета
            {
                statusText.text = $"<color=#{hexWear}>{Translator.GetText(56)}</color>"; // Надеть
            }
            else // Если заблокирована
            {
                if (data.category == AvatarCategory.Free) // Бесплатная по уровню
                {
                    statusText.text = $"<color=#{hexLocked}>{Translator.GetText(54)}{data.unlockLevelRequired}</color>"; // Ур. X
                }
                else if (data.category == AvatarCategory.Shop) // Покупная за золото
                {
                    statusText.text = currentLevel < 5 
                        ? $"<color=#{hexLocked}>{Translator.GetText(62)}</color>" // С 5 Ур.
                        : $"<color=#{hexGold}>{data.goldPrice} G</color>"; // Цена в золоте
                }
                else // Премиум за кристаллы
                {
                    statusText.text = currentLevel < 3 
                        ? $"<color=#{hexCrystal}>{Translator.GetText(63)}</color>" // С 3 Ур.
                        : $"<color=#{hexCrystal}>{data.crystalPrice} C</color>"; // Цена в кристаллах
                }
            }
        }

        if (cellBtn != null) // Если кнопка карточки назначена
        {
            cellBtn.onClick.AddListener(() => OnSelectAvatar(data)); // Назначение выбора аватарки при клике
        }
    }

    private void CreateFramesSection(string headerTitle) // Создание секции волшебных рамок
    {
        if (allFrames == null || allFrames.Count == 0) return; // Проверка наличия рамок

        if (categoryHeaderPrefab != null) // Префаб заголовка
        {
            GameObject headerObj = Instantiate(categoryHeaderPrefab, scrollContent); // Спавн заголовка рамок
            TextMeshProUGUI txt = headerObj.GetComponentInChildren<TextMeshProUGUI>(); // Поиск текста
            if (txt != null) // Если найден
            {
                txt.text = headerTitle; // Текст "ВОЛШЕБНЫЕ РАМКИ ПРОФИЛЯ"
                txt.color = categoryHeaderColor; // Золотой цвет
            }
        }

        GameObject gridContainer = new GameObject("GridSection_Frames", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); // Сетка для рамок
        gridContainer.transform.SetParent(scrollContent, false); // Привязка к скроллу

        GridLayoutGroup grid = gridContainer.GetComponent<GridLayoutGroup>(); // Компонент GridLayoutGroup
        grid.cellSize = cellSize; // Размер ячейки
        grid.spacing = cellSpacing; // Отступы
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // Фиксация числа колонок
        grid.constraintCount = Mathf.Max(1, columnsCount); // Число колонок
        grid.childAlignment = TextAnchor.UpperCenter; // Выравнивание
        grid.padding = new RectOffset(8, 8, 8, 16); // Отступы

        ContentSizeFitter fitter = gridContainer.GetComponent<ContentSizeFitter>(); // Авторазмер
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // По горизонтали
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // По вертикали

        foreach (FrameData frame in allFrames) // Заполнение рамками
        {
            CreateFrameCell(frame, gridContainer.transform); // Спавн ячейки рамки
        }
    }

    private void CreateFrameCell(FrameData data, Transform parentContainer) // Создание карточки рамки профиля
    {
        if (avatarItemPrefab == null) return; // Проверка префаба

        Transform targetParent = parentContainer != null ? parentContainer : scrollContent; // Определение родителя
        GameObject cell = Instantiate(avatarItemPrefab, targetParent); // Спавн карточки
        cell.name = $"Frame_{data.id}"; // Имя карточки рамки

        Image bgImg = cell.GetComponent<Image>(); // Фон ячейки
        if (bgImg != null) // Если есть
        {
            bgImg.color = cellBackgroundColor; // Установка темного фона
        }

        Image iconImg = cell.transform.Find("Avatar_Icon")?.GetComponent<Image>(); // Слой иконки
        Image frameImg = cell.transform.Find("Avatar_Frame")?.GetComponent<Image>(); // Слой рамки
        GameObject lockObj = cell.transform.Find("Lock_Overlay")?.gameObject; // Слой замка
        TextMeshProUGUI statusText = cell.transform.Find("Status_Text")?.GetComponent<TextMeshProUGUI>(); // Текст статуса
        Button cellBtn = cell.GetComponent<Button>(); // Кнопка карточки

        bool isUnlocked = IsFrameUnlocked(data); // Разблокирована ли рамка
        bool isSelected = (selectedFrameId == data.id); // Выбрана ли эта рамка

        if (iconImg != null) // Иконка превью рамки
        {
            if (data.frameSprite != null) // Если спрайт рамки задан
            {
                iconImg.sprite = data.frameSprite; // Отображение рамки
                iconImg.color = Color.white; // Белый цвет
                iconImg.enabled = true; // Включение картинки
            }
            else // Если спрайта нет
            {
                iconImg.sprite = null; // Обнуление спрайта
                iconImg.color = new Color(0.15f, 0.15f, 0.22f, 0.4f); // Темная заглушка
            }
        }

        if (frameImg != null) // Слой второй рамки
        {
            frameImg.gameObject.SetActive(false); // Отключение лишнего слоя
        }

        if (lockObj != null) // Замок
        {
            lockObj.SetActive(!isUnlocked); // Включение замка при блокировке
        }

        if (statusText != null) // Текст статуса карточки рамки
        {
            string hexSelected = ColorUtility.ToHtmlStringRGB(selectedStatusColor); // HEX цвет выбранного
            string hexWear = ColorUtility.ToHtmlStringRGB(wearStatusColor); // HEX цвет "Надеть"
            string hexLocked = ColorUtility.ToHtmlStringRGB(levelLockedColor); // HEX цвет блокировки
            string hexGold = ColorUtility.ToHtmlStringRGB(shopGoldPriceColor); // HEX цвет цены за золото
            string hexCrystal = ColorUtility.ToHtmlStringRGB(premiumCrystalColor); // HEX цвет кристаллов

            if (isSelected) // Если рамка надета
            {
                statusText.text = $"<color=#{hexSelected}><b>{Translator.GetText(55)}</b></color>"; // Выбрано
            }
            else if (isUnlocked) // Если открыта
            {
                statusText.text = $"<color=#{hexWear}>{Translator.GetText(56)}</color>"; // Надеть
            }
            else // Если закрыта
            {
                if (data.category == AvatarCategory.Free) // Бесплатная
                {
                    statusText.text = $"<color=#{hexLocked}>{Translator.GetText(54)}{data.unlockLevelRequired}</color>"; // Ур. X
                }
                else if (data.category == AvatarCategory.Shop) // Магазинная за золото
                {
                    statusText.text = currentLevel < 5 
                        ? $"<color=#{hexLocked}>{Translator.GetText(62)}</color>" // С 5 Ур.
                        : $"<color=#{hexGold}>{data.goldPrice} G</color>"; // Цена в золоте
                }
                else // Премиум за кристаллы
                {
                    statusText.text = currentLevel < 3 
                        ? $"<color=#{hexCrystal}>{Translator.GetText(63)}</color>" // С 3 Ур.
                        : $"<color=#{hexCrystal}>{data.crystalPrice} C</color>"; // Цена в кристаллах
                }
            }
        }

        if (cellBtn != null) // Кнопка клика по рамке
        {
            cellBtn.onClick.AddListener(() => OnSelectFrame(data)); // Обработчик клика
        }
    }

    public bool IsFrameUnlocked(FrameData data) // Проверка: открыта ли рамка профиля
    {
        if (data.isUnlockedByDefault) return true; // Разблокирована по умолчанию
        if (data.id < 1 && data.category == AvatarCategory.Free) return true; // Стартовая рамка всегда открыта
        if (PlayerPrefs.GetInt($"Frame_Unlocked_{data.id}", 0) == 1) return true; // Куплена или разблокирована ранее

        if (data.category == AvatarCategory.Free && currentLevel >= data.unlockLevelRequired) // Достигнут требуемый уровень
        {
            return true; // Открыта по уровню
        }

        return false; // Заблокирована
    }

    private void OnSelectFrame(FrameData data) // Обработчик выбора рамки профиля
    {
        if (!IsFrameUnlocked(data)) // Если рамка заблокирована
        {
            Debug.Log($"[FRAME] {data.frameNameRU} is locked!"); // Лог в консоль
            return; // Запрет выбора
        }

        selectedFrameId = data.id; // Установка выбранного ID рамки
        PlayerPrefs.SetInt("Selected_Frame_Id", selectedFrameId); // Сохранение выбора в PlayerPrefs
        PlayerPrefs.Save(); // Запись на диск

        if (selectSound != null && SettingsManager.Instance != null) // Звуковой эффект
            SettingsManager.Instance.PlaySoundEffect(selectSound); // Воспроизведение звука

        UpdateProfileUI(); // Обновление визуала профиля
        UpdateAllCellStatusTexts(); // Обновление надписей в гардеробе
    }

    public bool IsAvatarUnlocked(AvatarData data) // Проверка: открыта ли аватарка кота
    {
        if (data.isUnlockedByDefault) return true; // Стартовая открыта по умолчанию
        if (data.id < 3 && data.category == AvatarCategory.Free) return true; // Первые 3 стартовые аватарки всегда открыты
        if (PlayerPrefs.GetInt($"Avatar_Unlocked_{data.id}", 0) == 1) return true; // Сохранен статус покупки

        if (data.category == AvatarCategory.Free && currentLevel >= data.unlockLevelRequired) // Достигнут уровень
        {
            return true; // Открыта по уровню
        }

        return false; // Закрыта
    }

    private void OnSelectAvatar(AvatarData data) // Обработчик выбора аватарки
    {
        if (!IsAvatarUnlocked(data)) // Если аватарка заблокирована
        {
            Debug.Log($"[AVATAR] {data.avatarNameRU} is locked!"); // Лог в консоль
            return; // Запрет выбора
        }

        selectedAvatarId = data.id; // Установка выбранного ID аватарки
        PlayerPrefs.SetInt("Selected_Avatar_Id", selectedAvatarId); // Сохранение в PlayerPrefs
        PlayerPrefs.SetInt("Tutorial_Avatar_Chosen", 1); // Флаг: туториал выбора аватарки пройден
        PlayerPrefs.Save(); // Запись на диск

        if (closeButton != null) // Кнопка закрытия гардероба
        {
            closeButton.interactable = true; // Разблокировка кнопки
            closeButton.gameObject.SetActive(true); // Включение видимости кнопки
        }

        if (selectSound != null && SettingsManager.Instance != null) // Звуковой эффект
            SettingsManager.Instance.PlaySoundEffect(selectSound); // Воспроизведение клика

        UpdateProfileUI(); // Обновление визуала профиля
        UpdateAllCellStatusTexts(); // Обновление надписей во всех карточках
    }

    /// <summary>
    /// Быстрое бесшовное обновление надписей "Выбрано / Надеть" без мерцания и без пересоздания GameObjects
    /// </summary>
    private void UpdateAllCellStatusTexts() // Обновление надписей Выбрано/Надеть на карточках
    {
        if (scrollContent == null) return; // Проверка наличия контента

        string hexSelected = ColorUtility.ToHtmlStringRGB(selectedStatusColor); // HEX цвет Выбрано
        string hexWear = ColorUtility.ToHtmlStringRGB(wearStatusColor); // HEX цвет Надеть

        foreach (Transform section in scrollContent) // Цикл по секциям гардероба
        {
            if (!section.name.StartsWith("GridSection_")) continue; // Пропуск заголовков

            foreach (Transform cell in section) // Цикл по карточкам секции
            {
                TextMeshProUGUI statusText = cell.Find("Status_Text")?.GetComponent<TextMeshProUGUI>(); // Текст статуса
                if (statusText == null) continue; // Пропуск карточки, если нет текста

                if (cell.name.StartsWith("Avatar_")) // Если карточка аватарки
                {
                    if (int.TryParse(cell.name.Replace("Avatar_", ""), out int aId)) // Парсинг ID аватарки
                    {
                        AvatarData av = allAvatars.Find(a => a.id == aId); // Поиск данных аватарки
                        if (av != null && IsAvatarUnlocked(av)) // Если открыта
                        {
                            bool isSel = (selectedAvatarId == aId); // Выбрана ли сейчас
                            statusText.text = isSel 
                                ? $"<color=#{hexSelected}><b>{Translator.GetText(55)}</b></color>" // Текст "Выбрано"
                                : $"<color=#{hexWear}>{Translator.GetText(56)}</color>"; // Текст "Надеть"
                        }
                    }
                }
                else if (cell.name.StartsWith("Frame_")) // Если карточка рамки
                {
                    if (int.TryParse(cell.name.Replace("Frame_", ""), out int fId)) // Парсинг ID рамки
                    {
                        FrameData fr = allFrames.Find(f => f.id == fId); // Поиск данных рамки
                        if (fr != null && IsFrameUnlocked(fr)) // Если открыта
                        {
                            bool isSel = (selectedFrameId == fId); // Выбрана ли сейчас
                            statusText.text = isSel 
                                ? $"<color=#{hexSelected}><b>{Translator.GetText(55)}</b></color>" // Текст "Выбрано"
                                : $"<color=#{hexWear}>{Translator.GetText(56)}</color>"; // Текст "Надеть"
                        }
                    }
                }
            }
        }
    }
}
