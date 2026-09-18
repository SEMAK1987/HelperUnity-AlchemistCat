using System; // Базовые системные типы
using System.Collections; // Корутины и перечисления
using System.Collections.Generic; // Списки и коллекции
using UnityEngine; // Игровой движок Unity
using UnityEngine.UI; // Компоненты стандартного интерфейса Unity
using TMPro; // TextMeshPro для текстовых полей

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Мини-игра: Поиск предметов (Hidden Object Minigame)
/// 
/// Логика игры:
/// 1. Три основные локации (Лавка Алхимика, Дом Алхимика, Магический Рынок)
/// 2. Каждая локация имеет 3 уровня сложности (Легкий, Нормальный, Сложный) с поэтапными раундами и точными наградами
/// 3. После прохождения всех локаций Кот-Алхимик объявляет открытие эндгейм-режима «Становление Рекорда»
/// 4. Режим «Становление Рекорда» содержит 3 ежемесячных хардкорных испытания с наградами в Кристаллах:
///    - Блиц-поиск на время (+1 Кристалл)
///    - Лавина предметов 20-30 (+1..5 Кристаллов)
///    - Мерцающее появление (+1..10 Кристаллов)
/// 5. Полная отказоустойчивость: автогенерация интерфейса, процедурные фоны и спрайты при отсутствии ссылок в инспекторе.
/// </summary>
public class HiddenObject_Minigame : MonoBehaviour
{
    public static HiddenObject_Minigame Instance { get; private set; } // Статический синглтон мини-игры поиска предметов

    public enum DifficultyTier { Easy, Normal, Hard } // Уровни сложности (Легкий, Средний, Сложный)
    public enum RecordModeType { TimeRush, BigCountSurge, FlickerSpawn } // Режимы испытаний рекордов

    [System.Serializable]
    public class DifficultyConfig
    {
        public DifficultyTier tier; // Уровень сложности
        public string tierName = "Легкий"; // Название уровня сложности
        public int itemsPerRound = 5; // Количество предметов за 1 раунд
        public int roundsRequired = 3; // Количество раундов (попыток) для победы
        public float timeLimitPerRound = 90f; // Время на 1 раунд в секундах

        [Header("Награды за завершение")]
        public int rewardGold = 1000; // Награда золотом
        public int rewardStones = 3; // Награда камнями
        public int rewardScrolls = 1; // Награда свитками
        public int rewardExpPotion100 = 0; // Кол-во зелий опыта +100 XP
        public int rewardExpPotion500 = 0; // Кол-во зелий опыта +500 XP
        public int rewardMasteryPotion100 = 0; // Кол-во зелий мастерства +100 XP
        public int rewardMasteryPotion500 = 0; // Кол-во зелий мастерства +500 XP
    }

    [System.Serializable]
    public class LocationConfig
    {
        public string locationId = "Shop"; // Идентификатор локации
        public string locationName = "Лавка Алхимика"; // Название локации
        public string locationDescription = "Полки с древними снадобьями, ретортами и корешками"; // Краткое описание локации
        public Sprite backgroundSprite; // Фоновое изображение локации
        public Button locationCardButton; // Кнопка выбора локации
        public List<Sprite> itemsPool = new List<Sprite>(); // Набор предметов для поиска в локации

        [Header("3 Уровня сложности локации")]
        public List<DifficultyConfig> difficulties = new List<DifficultyConfig>(); // Настройки 3 сложностей

        [Header("Точки спавна (опционально: если оставить пустым, предметы размещаются автоматически)")]
        public List<Transform> customSpawnPoints = new List<Transform>(); // Ручные Transform-точки размещения предметов на сцене
        public List<Vector2> customSpawnPositions = new List<Vector2>(); // Ручные 2D координаты размещения на фоне

        [Header("Статус прохождения")]
        public bool isEasyCompleted; // Пройден ли легкий уровень
        public bool isNormalCompleted; // Пройден ли нормальный уровень
        public bool isHardCompleted; // Пройден ли сложный уровень
        public bool IsFullyCompleted => isEasyCompleted && isNormalCompleted && isHardCompleted; // Полностью ли зачищена локация
    }

    [Header("=== Главные панели ===")]
    public GameObject hiddenObjectPanel; // Главная панель игры поиска предметов (игровой экран)
    public GameObject locationSelectPopup; // Попап выбора локации (Location_Selection_Popup)
    public GameObject difficultySelectPopup; // Попап выбора сложности (Difficulty_Selection_Popup)
    public GameObject topStatusBar; // Верхняя статусная панель (Top_Status_Bar)
    public GameObject bottomTargetsBar; // Нижняя панель целей (Bottom_Targets_Bar)
    public Button closeGameButton; // Кнопка закрытия игры

    [Header("=== Кнопки локаций и блокировка ===")]
    public List<Button> locationButtons = new List<Button>(); // Кнопки 3 локаций (Location_1_Button, Location_2_Button, Location_3_Button)
    public List<GameObject> locationLockOverlays = new List<GameObject>(); // Оверлеи/иконки замков для закрытых комнат

    [Header("=== Кнопки выбора сложности ===")]
    public Button buttonEasy; // Кнопка легкой сложности
    public Button buttonNormal; // Кнопка обычной сложности
    public Button buttonHard; // Кнопка сложной сложности
    public Button buttonBackToLocations; // Кнопка возврата к списку локаций
    public TextMeshProUGUI difficultyPopupTitleText; // Текст заголовка выбора сложности

    [Header("=== Viewport и Зум/Панорамирование ===")]
    public RectTransform viewportContainer; // Окно просмотра локации (Fullscreen_Viewport_Container)
    public RectTransform backgroundContentRoot; // Корневой контейнер с фоном (Background_Content_Root)
    public RectTransform interactiveClickTargetsContainer; // Контейнер для кликабельных предметов (Interactive_Click_Targets)
    public Image backgroundLocationImage; // Спрайт фона локации (Background_8K_Image)
    public AspectRatioFitter backgroundAspect; // Компонент сохранения пропорций
    public float minZoom = 1.0f; // Минимальный масштаб
    public float maxZoom = 2.8f; // Максимальный зум
    public float zoomSpeed = 0.5f; // Скорость зумирования колесиком/жестом
    public Button zoomInButton; // Кнопка приближения [+]
    public Button zoomOutButton; // Кнопка отдаления [-]
    public Button zoomResetButton; // Кнопка сброса зума [1x]

    [Header("=== Нижняя панель целей (Target Items Bar) ===")]
    public RectTransform targetIconsContainer; // Контейнер иконок искомых предметов (Target_Icons_Holder / Bottom_Targets_Bar)
    public GameObject targetItemSlotPrefab; // Префаб ячейки искомого предмета
    public TextMeshProUGUI itemsRemainingText; // Текст "Осталось: X" (ItemsRemainingText)
    public TextMeshProUGUI locationTitleText; // Текст названия локации
    public TextMeshProUGUI currentRoundText; // Текст текущего раунда

    [Header("=== Кнопки подсказок и таймер ===")]
    public Button hintCatButton; // Кнопка вызова подсказки Кота
    public TextMeshProUGUI hintCountText; // Счетчик оставшихся подсказок
    public TextMeshProUGUI timerText; // Текст таймера раунда
    public int availableHints = 3; // Доступное количество подсказок

    [Header("=== Окно победы локации ===")]
    public GameObject victoryPopupPanel; // Окно победы
    public TextMeshProUGUI victoryTitleText; // Заголовок победы
    public TextMeshProUGUI victoryRewardsText; // Текст полученных наград
    public Button claimRewardsAndBackButton; // Кнопка сбора наград и возврата

    [Header("=== Кот и Режим 'Становление Рекорда' ===")]
    public GameObject catRecordUnlockedDialog; // Окно диалога с Котом об открытии рекордов
    public TextMeshProUGUI catSpeechText; // Текст речи Кота
    public Button catDialogContinueButton; // Кнопка продолжить в диалоге
    public GameObject recordModeSelectPopup; // Попап с 3 хардкорными испытаниями
    public Button recordTimeRushButton; // Блиц на время (+1 Кристалл)
    public Button recordItemSurgeButton; // Лавина предметов (+1..5 Кристаллов)
    public Button recordFlickerSpawnButton; // Мерцание (+1..10 Кристаллов)
    public Button closeRecordPopupButton; // Кнопка закрытия попапа рекордов

    [Header("=== Переход к Защите Котлов и Диалоги ===")]
    public GameObject cauldronDefenseGamePanel; // Ссылка на панель игры Защита Котлов (CauldronDefense_Game_Panel)
    public GameObject dialogueContainer; // Панель диалога с Котом
    public TextMeshProUGUI dialogueText; // Текстовое поле реплики Кота

    [Header("=== Спрайты зелий для выдачи в инвентарь ===")]
    public Sprite expPotion100Sprite; // Спрайт зелья опыта +100 XP
    public Sprite expPotion500Sprite; // Спрайт зелья опыта +500 XP
    public Sprite masteryPotion100Sprite; // Спрайт зелья мастерства +100 XP
    public Sprite masteryPotion500Sprite; // Спрайт зелья мастерства +500 XP
    public Sprite stoneSprite; // Спрайт камня
    public Sprite scrollSprite; // Спрайт свитка

    [Header("=== Конфигурация 3 Локаций ===")]
    public List<LocationConfig> locations = new List<LocationConfig>(); // Список 3 локаций с предметами

    // Внутреннее состояние игры
    private LocationConfig currentLocation; // Текущая выбранная локация
    private DifficultyConfig currentDifficulty; // Текущая сложность
    private int currentRoundIndex = 1; // Номер раунда
    private int itemsFoundInCurrentRound = 0; // Найдено предметов в раунде
    private float roundTimer = 0f; // Таймер времени раунда
    private bool isGameRunning = false; // Флаг: идет ли игра
    private bool isRecordModeActive = false; // Флаг: активен ли хардкорный режим рекордов
    private RecordModeType currentRecordMode = RecordModeType.TimeRush; // Выбранный тип рекорда
    private int recordTargetCount = 0; // Целевое количество для рекорда
    private float currentZoom = 1.0f; // Текущий коэффициент масштаба
    private Vector2 panOffset = Vector2.zero; // Смещение панорамирования
    private List<GameObject> activeClickableItems = new List<GameObject>(); // Список интерактивных предметов на экране
    private Coroutine flickerCoroutine; // Ссылка на корутину мерцания
    private bool isUIHierarchyBuilt = false; // Флаг: инициализирован ли интерфейс

    private void Awake() // Инициализация синглтона при пробуждении объекта
    {
        if (Instance == null) Instance = this; // Инициализация синглтона
        else if (Instance != this) { Destroy(gameObject); return; } // Уничтожение дубликата

        InitializeDefaultConfigurationsIfEmpty(); // Инициализация локаций и дефолтных предметов
        EnsureUIHierarchy(); // Проверка и связка иерархии UI панелей
        SetupButtons(); // Настройка подписчиков на кнопки
    }

    private void Start() // Стартовая настройка конфигурации, кнопок и сохранений
    {
        InitializeDefaultConfigurationsIfEmpty(); // Повторная проверка локаций
        EnsureUIHierarchy(); // Повторная проверка UI
        SetupButtons(); // Настройка кликов кнопок интерфейса
        LoadCompletionProgress(); // Загрузка сохраненного прогресса прохождения локаций
    }

    private void OnEnable() // При включении объекта игры
    {
        EnsureUIHierarchy(); // Гарантированная готовность всех элементов
        if (!isGameRunning) // Если раунд не идет
        {
            ShowLocationSelectionScreen(); // Открытие меню выбора комнат
        }
    }

    /// <summary>
    /// Открытие окна мини-игры «Поиск Предметов» с активацией меню локаций
    /// </summary>
    public void OpenMinigame()
    {
        // 1. Гарантированно отключаем другие мини-игры (Мышей и Рыбалку)
        if (CatchMouse_Minigame.Instance != null) CatchMouse_Minigame.Instance.CloseMinigame();
        if (AlchemyFishing_Minigame.Instance != null && AlchemyFishing_Minigame.Instance.rootFishingGamePanel != null)
            AlchemyFishing_Minigame.Instance.rootFishingGamePanel.SetActive(false);

        GameObject mousePanel = GameObject.Find("CatchMouse_Game_Panel");
        if (mousePanel != null) mousePanel.SetActive(false);
        GameObject fishingPanel = GameObject.Find("AlchemyFishing_Game_Panel");
        if (fishingPanel != null) fishingPanel.SetActive(false);

        gameObject.SetActive(true); // Активация объекта игры
        Transform p = transform.parent; // Поиск родителя
        while (p != null) // Включение всех родителей
        {
            p.gameObject.SetActive(true); // Включение родительской панели
            p = p.parent; // Подъем выше по иерархии
        }

        if (DialogueSystem_Manager.Instance != null) // Скрытие HUD интерфейса
        {
            DialogueSystem_Manager.Instance.HideHUDForMinigame(); // Прячем аватарку и верхние кнопки
        }

        EnsureUIHierarchy(); // Проверка UI
        ShowLocationSelectionScreen(); // Показ выбора локаций
    }

    /// <summary>
    /// Показ стартового меню выбора локаций (3 карточки комнат: 1-я открыта, 2-я и 3-я заблокированы до прохождения)
    /// </summary>
    public void ShowLocationSelectionScreen()
    {
        // 1. Гарантируем активность корневого объекта и всех его родителей
        gameObject.SetActive(true); // Включение самого скрипта / объекта
        Transform p = transform.parent; // Поиск родительского узла
        while (p != null) // Обход всех родителей вверх по иерархии
        {
            p.gameObject.SetActive(true); // Включение родителя
            p = p.parent; // Переход выше
        }

        EnsureUIHierarchy(); // Гарантированная проверка ссылок перед переключением

        isGameRunning = false; // Остановка таймеров геймплея
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine); // Остановка мерцания

        // 2. Скрываем все всплывающие окна и оверлеи геймплея
        if (difficultySelectPopup != null) difficultySelectPopup.SetActive(false); // Скрытие меню сложности
        if (victoryPopupPanel != null) victoryPopupPanel.SetActive(false); // Скрытие окна победы
        if (recordModeSelectPopup != null) recordModeSelectPopup.SetActive(false); // Скрытие окна рекордов
        if (catRecordUnlockedDialog != null) catRecordUnlockedDialog.SetActive(false); // Скрытие диалога кота

        // 3. Скрываем элементы активного поиска предметов (чтобы не оставалось пустого фона или игрового экрана)
        if (viewportContainer != null) viewportContainer.gameObject.SetActive(false); // Скрытие полотна поиска
        if (topStatusBar != null) topStatusBar.SetActive(false); // Скрытие верхнего бара
        if (bottomTargetsBar != null) bottomTargetsBar.SetActive(false); // Скрытие нижней панели целей
        ClearActiveItems(); // Очистка найденных и оставшихся предметов

        // 4. Включаем главную панель игры (если она является родителем для меню)
        if (hiddenObjectPanel != null)
        {
            hiddenObjectPanel.SetActive(true); // Включение главной панели
        }

        // 5. Включаем и выводим на передний план попап выбора 3 комнат
        if (locationSelectPopup != null)
        {
            locationSelectPopup.SetActive(true); // Включение меню локаций
            locationSelectPopup.transform.SetAsLastSibling(); // Вывод на передний план
        }

        RefreshLocationCardsUI(); // Обновление состояния блокировок и подсветки 3 комнат
    }

    private void SetupButtons() // Назначение слушателей событий нажатия на все кнопки интерфейса
    {
        if (closeGameButton) // Кнопка закрытия игры (в Top_Status_Bar или инспекторе)
        {
            closeGameButton.onClick.RemoveAllListeners(); // Очистка старых
            closeGameButton.onClick.AddListener(CloseGame); // Подписка
        }

        // Кнопка выхода в окне выбора локации Location_Selection_Popup
        if (locationSelectPopup != null)
        {
            Button locCloseBtn = locationSelectPopup.transform.Find("Close_Button")?.GetComponent<Button>() ?? locationSelectPopup.transform.Find("Exit_Button")?.GetComponent<Button>();
            if (locCloseBtn != null)
            {
                locCloseBtn.onClick.RemoveAllListeners(); // Очистка
                locCloseBtn.onClick.AddListener(CloseGame); // Выход из мини-игры
            }
        }

        if (claimRewardsAndBackButton) // Кнопка забрать награды
        {
            claimRewardsAndBackButton.onClick.RemoveAllListeners(); // Очистка
            claimRewardsAndBackButton.onClick.AddListener(OnClaimRewardsClicked); // Подписка
        }

        if (hintCatButton) // Кнопка подсказки Кота
        {
            hintCatButton.onClick.RemoveAllListeners(); // Очистка
            hintCatButton.onClick.AddListener(UseHint); // Подписка
        }

        if (buttonBackToLocations) // Кнопка возврата из меню сложности
        {
            buttonBackToLocations.onClick.RemoveAllListeners(); // Очистка
            buttonBackToLocations.onClick.AddListener(ShowLocationSelectionScreen); // Возврат к локациям
        }

        // Кнопка возврата внутри Difficulty_Selection_Popup
        if (difficultySelectPopup != null)
        {
            Button diffCloseBtn = difficultySelectPopup.transform.Find("Button_Back_To_Locations")?.GetComponent<Button>() ?? 
                                  difficultySelectPopup.transform.Find("Close_Button")?.GetComponent<Button>() ?? 
                                  difficultySelectPopup.transform.Find("Back_Button")?.GetComponent<Button>();
            if (diffCloseBtn != null && diffCloseBtn != buttonBackToLocations)
            {
                diffCloseBtn.onClick.RemoveAllListeners(); // Очистка
                diffCloseBtn.onClick.AddListener(ShowLocationSelectionScreen); // Возврат к локациям
            }
        }

        if (zoomInButton) // Кнопка зума [+]
        {
            zoomInButton.onClick.RemoveAllListeners(); // Очистка
            zoomInButton.onClick.AddListener(() => ChangeZoom(0.35f)); // Увеличение масштаба
        }

        if (zoomOutButton) // Кнопка зума [-]
        {
            zoomOutButton.onClick.RemoveAllListeners(); // Очистка
            zoomOutButton.onClick.AddListener(() => ChangeZoom(-0.35f)); // Уменьшение масштаба
        }

        if (zoomResetButton) // Кнопка сброса зума [1x]
        {
            zoomResetButton.onClick.RemoveAllListeners(); // Очистка
            zoomResetButton.onClick.AddListener(ResetZoomAndPan); // Сброс масштаба
        }

        // Кнопки сложностей
        if (buttonEasy) // Легкий уровень
        {
            buttonEasy.onClick.RemoveAllListeners(); // Очистка
            buttonEasy.onClick.AddListener(() => StartGameWithDifficulty(0)); // Запуск
        }
        if (buttonNormal) // Обычный уровень
        {
            buttonNormal.onClick.RemoveAllListeners(); // Очистка
            buttonNormal.onClick.AddListener(() => StartGameWithDifficulty(1)); // Запуск
        }
        if (buttonHard) // Сложный уровень
        {
            buttonHard.onClick.RemoveAllListeners(); // Очистка
            buttonHard.onClick.AddListener(() => StartGameWithDifficulty(2)); // Запуск
        }

        // Кнопки рекордов
        if (catDialogContinueButton) // Кнопка продолжить в диалоге рекордов
        {
            catDialogContinueButton.onClick.RemoveAllListeners(); // Очистка
            catDialogContinueButton.onClick.AddListener(OpenRecordModeSelection); // Переход к испытаниям
        }
        if (recordTimeRushButton) // Блиц-поиск
        {
            recordTimeRushButton.onClick.RemoveAllListeners(); // Очистка
            recordTimeRushButton.onClick.AddListener(() => StartRecordMode(RecordModeType.TimeRush)); // Старт
        }
        if (recordItemSurgeButton) // Лавина предметов
        {
            recordItemSurgeButton.onClick.RemoveAllListeners(); // Очистка
            recordItemSurgeButton.onClick.AddListener(() => StartRecordMode(RecordModeType.BigCountSurge)); // Старт
        }
        if (recordFlickerSpawnButton) // Мерцание
        {
            recordFlickerSpawnButton.onClick.RemoveAllListeners(); // Очистка
            recordFlickerSpawnButton.onClick.AddListener(() => StartRecordMode(RecordModeType.FlickerSpawn)); // Старт
        }
        if (closeRecordPopupButton) // Закрытие рекордов
        {
            closeRecordPopupButton.onClick.RemoveAllListeners(); // Очистка
            closeRecordPopupButton.onClick.AddListener(ShowLocationSelectionScreen); // Возврат к локациям
        }

        // Привязка кнопок карточек комнат
        SetupLocationButtonsListeners(); // Настройка кликов по 3 комнатам
    }

    private void SetupLocationButtonsListeners() // Привязка событий клика к 3 кнопкам комнат
    {
        for (int i = 0; i < 3; i++) // Перебор 3 комнат
        {
            int locIndex = i; // Замыкание индекса
            Button btn = null; // Переменная кнопки

            if (locationButtons != null && i < locationButtons.Count && locationButtons[i] != null) // Если задано в списке
            {
                btn = locationButtons[i]; // Получение из списка
            }
            else if (i < locations.Count && locations[i].locationCardButton != null) // Если задано в локации
            {
                btn = locations[i].locationCardButton; // Получение из локации
            }

            if (btn != null) // Если кнопка найдена
            {
                btn.onClick.RemoveAllListeners(); // Очистка старых подписчиков
                btn.onClick.AddListener(() => OnLocationCardClicked(locIndex)); // Подписка на клик
            }
        }
    }

    /// <summary>
    /// Обработка клика по карточке локации (проверка на блокировку)
    /// </summary>
    public void OnLocationCardClicked(int locationIndex)
    {
        if (locationIndex < 0 || locationIndex >= locations.Count) return; // Проверка диапазона

        bool isUnlocked = IsLocationUnlocked(locationIndex); // Проверка доступности
        if (!isUnlocked) // Если закрыто
        {
            Debug.Log($"Локация {locationIndex + 1} заблокирована! Сначала пройдите предыдущую локацию."); // Лог
            return; // Выход
        }

        OpenLocationDifficultySelect(locationIndex); // Открытие выбора сложности
    }

    /// <summary>
    /// Проверка, открыта ли конкретная локация:
    /// - Комната 1 (Лавка Алхимика): открыта изначально. После завершения блокируется.
    /// - Комната 2 (Дом Алхимика): открывается ПОСЛЕ прохождения Лавки Алхимика. После завершения блокируется.
    /// - Комната 3 (Магический Рынок): открывается ПОСЛЕ прохождения Дома Алхимика.
    /// </summary>
    public bool IsLocationUnlocked(int index)
    {
        if (locations == null || locations.Count == 0) return index == 0; // Защита от пустых списков

        bool loc0Completed = locations.Count > 0 && (locations[0].isEasyCompleted || locations[0].isNormalCompleted || locations[0].isHardCompleted || locations[0].IsFullyCompleted); // Пройдена ли Лавка
        bool loc1Completed = locations.Count > 1 && (locations[1].isEasyCompleted || locations[1].isNormalCompleted || locations[1].isHardCompleted || locations[1].IsFullyCompleted); // Пройден ли Дом
        bool loc2Completed = locations.Count > 2 && (locations[2].isEasyCompleted || locations[2].isNormalCompleted || locations[2].isHardCompleted || locations[2].IsFullyCompleted); // Пройден ли Рынок

        if (index == 0) // Комната 1: Лавка Алхимика
        {
            // Открыта в самом начале, пока игрок не прошел в ней хотя бы один уровень сложности.
            // После прохождения Лавка блокируется, уступая место Дому Алхимика.
            return !loc0Completed;
        }
        else if (index == 1) // Комната 2: Дом Алхимика
        {
            // Разблокируется только после завершения Лавки Алхимика.
            // Блокируется после того, как игрок завершит сам Дом Алхимика, передавая эстафету Рынку.
            return loc0Completed && !loc1Completed;
        }
        else if (index == 2) // Комната 3: Магический / Антикварный Рынок
        {
            // Разблокируется только после завершения Дома Алхимика.
            return loc1Completed;
        }

        return false;
    }

    /// <summary>
    /// Открытие окна выбора сложности для выбранной локации
    /// </summary>
    public void OpenLocationDifficultySelect(int locationIndex) // Открытие меню сложности для выбранной локации
    {
        if (locationIndex < 0 || locationIndex >= locations.Count) return; // Проверка валидности индекса
        currentLocation = locations[locationIndex]; // Установка текущей локации

        if (difficultyPopupTitleText) // Если текстовый заголовок задан
            difficultyPopupTitleText.text = $"Сложность: {currentLocation.locationName}"; // Установка названия локации

        if (locationSelectPopup) locationSelectPopup.SetActive(false); // Скрытие окна локаций
        if (difficultySelectPopup)
        {
            difficultySelectPopup.SetActive(true); // Показ окна выбора сложности
            difficultySelectPopup.transform.SetAsLastSibling(); // На передний план
        }
    }

    /// <summary>
    /// Старт локации с выбранной сложностью (0 - Легкий, 1 - Нормальный, 2 - Сложный)
    /// </summary>
    public void StartGameWithDifficulty(int difficultyIndex) // Запуск игры в локации с выбранной сложностью
    {
        if (currentLocation == null) // Если локация не выбрана
        {
            if (locations.Count > 0) currentLocation = locations[0]; // Выбираем первую по умолчанию
            else return; // Выход
        }

        if (difficultyIndex < 0 || difficultyIndex >= currentLocation.difficulties.Count) return; // Проверка индекса сложности
        currentDifficulty = currentLocation.difficulties[difficultyIndex]; // Установка параметров сложности

        if (difficultySelectPopup) difficultySelectPopup.SetActive(false); // Скрытие попапа сложности
        if (locationSelectPopup) locationSelectPopup.SetActive(false); // Скрытие попапа локаций

        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(true); // Включение главного игрового экрана
        if (viewportContainer) viewportContainer.gameObject.SetActive(true); // Включение Viewport
        if (topStatusBar) topStatusBar.SetActive(true); // Включение верхнего бара
        if (bottomTargetsBar) bottomTargetsBar.SetActive(true); // Включение нижней панели целей

        isRecordModeActive = false; // Отключение режима рекордов
        currentRoundIndex = 1; // Установка 1-го раунда
        availableHints = 3; // Выдача 3 подсказок

        StartRound(); // Запуск раунда
    }

    private void StartRound() // Старт очередного раунда поиска предметов
    {
        itemsFoundInCurrentRound = 0; // Сброс найденных предметов
        roundTimer = currentDifficulty != null ? currentDifficulty.timeLimitPerRound : 90f; // Установка таймера раунда
        isGameRunning = true; // Активация игрового цикла

        if (backgroundLocationImage && currentLocation != null && currentLocation.backgroundSprite != null) // Если фон назначен
        {
            backgroundLocationImage.sprite = currentLocation.backgroundSprite; // Установка фона локации
        }

        if (locationTitleText) // Если заголовок задан
        {
            string locName = currentLocation != null ? currentLocation.locationName : "Лавка Алхимика"; // Имя локации
            string diffName = currentDifficulty != null ? currentDifficulty.tierName : "Легкий"; // Имя сложности
            locationTitleText.text = $"{locName} — {diffName}"; // Отображение имени локации и сложности
        }

        ResetZoomAndPan(); // Сброс масштаба к 100%
        UpdateUI(); // Обновление счетчиков и текстов
        SpawnTargetItemsForRound(); // Генерация предметов на поле и в панели целей
    }

    private void SpawnTargetItemsForRound() // Спавн иконок целей и кликабельных предметов на поле
    {
        ClearActiveItems(); // Очистка старых предметов

        if (currentLocation == null || currentLocation.itemsPool.Count == 0) // Проверка пула предметов
        {
            EnsureDefaultSpritesForLocation(currentLocation); // Гарантируем наличие спрайтов
        }

        int totalToFind = currentDifficulty != null ? currentDifficulty.itemsPerRound : 5; // Количество предметов в раунде
        int poolCount = currentLocation.itemsPool.Count; // Размер пула спрайтов

        // 1. Сбор и подготовка пользовательских точек спавна (если заданы в инспекторе)
        List<Vector2> customPoints = new List<Vector2>(); // Список готовых координат точек
        if (currentLocation.customSpawnPoints != null && currentLocation.customSpawnPoints.Count > 0) // Если назначены Transform-точки
        {
            foreach (Transform pt in currentLocation.customSpawnPoints) // Перебор точек
            {
                if (pt != null) // Проверка на null
                {
                    RectTransform prt = pt.GetComponent<RectTransform>(); // Проверка RectTransform
                    if (prt != null) customPoints.Add(prt.anchoredPosition); // Добавление anchoredPosition
                    else customPoints.Add(new Vector2(pt.localPosition.x, pt.localPosition.y)); // Добавление localPosition
                }
            }
        }
        if (currentLocation.customSpawnPositions != null && currentLocation.customSpawnPositions.Count > 0) // Если заданы координаты Vector2
        {
            customPoints.AddRange(currentLocation.customSpawnPositions); // Добавление координат
        }

        // Перемешивание ручных точек для непредсказуемости поиска
        for (int p = 0; p < customPoints.Count; p++) // Перемешивание точек
        {
            int rnd = UnityEngine.Random.Range(p, customPoints.Count); // Случайный индекс
            Vector2 tmp = customPoints[p]; // Временная переменная
            customPoints[p] = customPoints[rnd]; // Смена
            customPoints[rnd] = tmp; // Завершение обмена
        }

        List<Vector2> usedSpawnPositions = new List<Vector2>(); // Список занятых позиций для предотвращения наложения

        for (int i = 0; i < totalToFind; i++) // Цикл создания целей
        {
            Sprite itemSprite = currentLocation.itemsPool[i % poolCount]; // Получение спрайта предмета

            // Создание иконки цели в нижней панели
            GameObject slotUi = null; // Ссылка на UI-слот
            if (targetIconsContainer != null) // Если контейнер целей задан
            {
                if (targetItemSlotPrefab != null) // Если есть префаб
                {
                    slotUi = Instantiate(targetItemSlotPrefab, targetIconsContainer); // Инстанцирование слота в панели целей
                }
                else // Динамическое создание слота цели
                {
                    slotUi = CreateDefaultTargetSlot(itemSprite); // Создание стандартного слота
                }

                Image img = slotUi.GetComponentInChildren<Image>(); // Поиск Image внутри слота
                if (img) img.sprite = itemSprite; // Назначение спрайта цели
            }

            // Вычисление позиции: из ручных точек или процедурно без наложения
            Vector2 spawnPos; // Координата спавна
            if (i < customPoints.Count) // Если есть свободная ручная точка
            {
                spawnPos = customPoints[i]; // Используем ручную точку
            }
            else // Иначе умное автоматическое размещение
            {
                spawnPos = GetSmartNonOverlappingPosition(usedSpawnPositions); // Поиск безопасной координаты
            }
            usedSpawnPositions.Add(spawnPos); // Запоминаем позицию

            // Спавн кликабельного предмета на фоне
            SpawnClickableItemOnBackground(itemSprite, slotUi, spawnPos); // Создание интерактивного предмета на сцене
        }
    }

    private Vector2 GetSmartNonOverlappingPosition(List<Vector2> usedPositions) // Генерация случайной позиции без слипания предметов
    {
        const float minDistance = 85f; // Минимальная дистанция между центрами предметов
        const float boundX = 460f; // Граница по горизонтали
        const float boundY = 240f; // Граница по вертикали

        for (int attempt = 0; attempt < 35; attempt++) // До 35 попыток поиска свободного места
        {
            Vector2 candidate = new Vector2(UnityEngine.Random.Range(-boundX, boundX), UnityEngine.Random.Range(-boundY, boundY)); // Кандидат координат
            bool isOverlap = false; // Флаг пересечения

            foreach (var pos in usedPositions) // Проверка дистанции до всех уже размещенных предметов
            {
                if (Vector2.Distance(candidate, pos) < minDistance) // Если слишком близко
                {
                    isOverlap = true; // Отметка пересечения
                    break; // Прерывание
                }
            }

            if (!isOverlap) return candidate; // Найдено свободное место
        }

        // Если поле плотно заполнено — возвращаем случайную позицию в безопасных границах
        return new Vector2(UnityEngine.Random.Range(-boundX, boundX), UnityEngine.Random.Range(-boundY, boundY)); // Возврат позиции
    }

    private GameObject CreateDefaultTargetSlot(Sprite itemSprite) // Создание UI-слота цели в нижней панели
    {
        GameObject slotObj = new GameObject("TargetSlot", typeof(RectTransform), typeof(Image)); // Создание объекта слота
        slotObj.transform.SetParent(targetIconsContainer, false); // Вложение в контейнер целей

        RectTransform rt = slotObj.GetComponent<RectTransform>(); // RectTransform
        rt.sizeDelta = new Vector2(64f, 64f); // Размер слота цели 64x64

        Image bgImg = slotObj.GetComponent<Image>(); // Фон рамки слота
        bgImg.color = new Color(0.20f, 0.16f, 0.12f, 0.90f); // Темный фон слота

        GameObject iconObj = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image)); // Объект иконки
        iconObj.transform.SetParent(slotObj.transform, false); // Вложение в слот

        RectTransform iconRt = iconObj.GetComponent<RectTransform>(); // RectTransform иконки
        iconRt.anchorMin = new Vector2(0.1f, 0.1f); // Отступы внутри рамки
        iconRt.anchorMax = new Vector2(0.9f, 0.9f);
        iconRt.sizeDelta = Vector2.zero;

        Image iconImg = iconObj.GetComponent<Image>(); // Компонент Image
        iconImg.sprite = itemSprite; // Спрайт
        iconImg.preserveAspect = true; // Сохранение пропорций

        return slotObj; // Возврат готового слота
    }

    private void SpawnClickableItemOnBackground(Sprite itemSprite, GameObject slotUi, Vector2 position) // Размещение кликабельного предмета на фоне локации
    {
        Transform spawnParent = interactiveClickTargetsContainer != null ? interactiveClickTargetsContainer : backgroundContentRoot; // Выбор контейнера целей
        if (spawnParent == null) return; // Проверка корневого контейнера фона

        GameObject clickable = new GameObject("HiddenItem_" + (itemSprite != null ? itemSprite.name : "Item"), typeof(RectTransform), typeof(Image), typeof(Button)); // Создание объекта предмета
        clickable.transform.SetParent(spawnParent, false); // Размещение внутри интерактивного контейнера
        activeClickableItems.Add(clickable); // Добавление в список активных предметов

        RectTransform rt = clickable.GetComponent<RectTransform>(); // Получение RectTransform
        rt.sizeDelta = new Vector2(68f, 68f); // Установка размеров предмета 68x68
        rt.anchoredPosition = position; // Установка точной вычисленной позиции
        rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-8f, 8f)); // Легкий случайный наклон для живости сцены
        float randomScale = UnityEngine.Random.Range(0.92f, 1.05f); // Случайное легкое масштабирование
        rt.localScale = new Vector3(randomScale, randomScale, 1f); // Применение масштаба

        Image img = clickable.GetComponent<Image>(); // Компонент Image
        img.sprite = itemSprite; // Установка спрайта предмета
        img.preserveAspect = true; // Сохранение пропорций

        Button btn = clickable.GetComponent<Button>(); // Компонент Button для клика
        btn.onClick.AddListener(() => // Подписка на нажатие
        {
            activeClickableItems.Remove(clickable); // Удаление из активного списка
            Destroy(clickable); // Уничтожение объекта на сцене
            if (slotUi != null) Destroy(slotUi); // Уничтожение слота из панели целей
            OnItemFound(); // Обработка нахождения предмета
        });
    }

    private void OnItemFound() // Обработка нахождения одного предмета игроком
    {
        itemsFoundInCurrentRound++; // Увеличение счетчика найденных предметов
        UpdateUI(); // Обновление текста интерфейса

        if (isRecordModeActive) // Если активен режим рекордов
        {
            if (itemsFoundInCurrentRound >= recordTargetCount) // Если набрано нужное количество в рекорде
            {
                CompleteRecordModeVictory(); // Завершение рекордного испытания победой
            }
            return; // Выход
        }

        int targetRequired = currentDifficulty != null ? currentDifficulty.itemsPerRound : 5; // Требуемое число
        if (itemsFoundInCurrentRound >= targetRequired) // Если все предметы раунда найдены
        {
            int maxRounds = currentDifficulty != null ? currentDifficulty.roundsRequired : 3; // Всего раундов
            if (currentRoundIndex < maxRounds) // Если остались еще раунды
            {
                currentRoundIndex++; // Переход к следующему раунду
                StartRound(); // Запуск следующего раунда
            }
            else // Если все раунды этапа завершены
            {
                CompleteCurrentDifficultyStage(); // Фиксация победы в локации
            }
        }
    }

    private void CompleteCurrentDifficultyStage() // Завершение этапа сложности с выдачей наград
    {
        isGameRunning = false; // Остановка игрового цикла

        if (currentLocation != null && currentDifficulty != null) // Если локация и сложность выбраны
        {
            if (currentDifficulty.tier == DifficultyTier.Easy) currentLocation.isEasyCompleted = true; // Отметка легкого уровня
            if (currentDifficulty.tier == DifficultyTier.Normal) currentLocation.isNormalCompleted = true; // Отметка нормального уровня
            if (currentDifficulty.tier == DifficultyTier.Hard) currentLocation.isHardCompleted = true; // Отметка сложного уровня
        }

        SaveCompletionProgress(); // Сохранение прогресса на диск
        GrantRewards(); // Выдача наград в инвентарь и профиль

        if (victoryPopupPanel) // Если панель победы задана
        {
            victoryPopupPanel.SetActive(true); // Показ окна победы
            if (victoryTitleText) // Если заголовок победы есть
            {
                string locName = currentLocation != null ? currentLocation.locationName : "Локация"; // Имя локации
                string diffName = currentDifficulty != null ? currentDifficulty.tierName : ""; // Имя сложности
                victoryTitleText.text = $"Победа: {locName} ({diffName})!"; // Текст победы
            }

            if (victoryRewardsText && currentDifficulty != null) // Если текстовый блок наград назначен
            {
                string rewardsSummary = ""; // Формирование списка наград
                if (currentDifficulty.rewardGold > 0) rewardsSummary += $"Золото: +{currentDifficulty.rewardGold:N0}   "; // Золото без эмодзи
                if (currentDifficulty.rewardStones > 0) rewardsSummary += $"Камни: +{currentDifficulty.rewardStones}   "; // Камни без эмодзи
                if (currentDifficulty.rewardScrolls > 0) rewardsSummary += $"Свитки: +{currentDifficulty.rewardScrolls}\n"; // Свитки без эмодзи
                if (currentDifficulty.rewardExpPotion100 > 0) rewardsSummary += $"Зелье Опыта (+100 XP): x{currentDifficulty.rewardExpPotion100}\n"; // Зелье опыта 100
                if (currentDifficulty.rewardExpPotion500 > 0) rewardsSummary += $"Зелье Опыта (+500 XP): x{currentDifficulty.rewardExpPotion500}\n"; // Зелье опыта 500
                if (currentDifficulty.rewardMasteryPotion100 > 0) rewardsSummary += $"Зелье Мастерства (+100 XP): x{currentDifficulty.rewardMasteryPotion100}\n"; // Зелье мастерства 100
                if (currentDifficulty.rewardMasteryPotion500 > 0) rewardsSummary += $"Зелье Мастерства (+500 XP): x{currentDifficulty.rewardMasteryPotion500}\n"; // Зелье мастерства 500

                victoryRewardsText.text = rewardsSummary; // Применение текста наград
            }
        }
    }

    private void GrantRewards() // Начисление камней, свитков, золота и зелий игроку
    {
        if (currentDifficulty == null) return; // Проверка

        if (Avatar_Manager.Instance != null) // Если менеджер аватара активен
        {
            if (currentDifficulty.rewardStones > 0) Avatar_Manager.Instance.AddStones(currentDifficulty.rewardStones); // Начисление камней
            if (currentDifficulty.rewardScrolls > 0) Avatar_Manager.Instance.AddScrolls(currentDifficulty.rewardScrolls); // Начисление свитков
            if (currentDifficulty.rewardGold > 0) Avatar_Manager.Instance.AddGold(currentDifficulty.rewardGold); // Начисление золота
        }

        if (Inventory_Manager.Instance != null) // Если менеджер инвентаря активен
        {
            if (currentDifficulty.rewardExpPotion100 > 0) // Выдача зелья опыта +100
                Inventory_Manager.Instance.AddItem("pot_exp_100", "Зелье Опыта (+100 XP)", currentDifficulty.rewardExpPotion100, 100, expPotion100Sprite, Color.cyan);
            if (currentDifficulty.rewardExpPotion500 > 0) // Выдача зелья опыта +500
                Inventory_Manager.Instance.AddItem("pot_exp_500", "Зелье Опыта (+500 XP)", currentDifficulty.rewardExpPotion500, 500, expPotion500Sprite, Color.magenta);
            if (currentDifficulty.rewardMasteryPotion100 > 0) // Выдача зелья мастерства +100
                Inventory_Manager.Instance.AddItem("pot_mastery_100", "Зелье Мастерства (+100 XP)", currentDifficulty.rewardMasteryPotion100, 100, masteryPotion100Sprite, Color.green);
            if (currentDifficulty.rewardMasteryPotion500 > 0) // Выдача зелья мастерства +500
                Inventory_Manager.Instance.AddItem("pot_mastery_500", "Зелье Мастерства (+500 XP)", currentDifficulty.rewardMasteryPotion500, 500, masteryPotion500Sprite, Color.yellow);
        }
    }

    private void OnClaimRewardsClicked() // Обработка закрытия окна победы и переход по цепочке локаций к Коту
    {
        if (victoryPopupPanel) victoryPopupPanel.SetActive(false); // Скрытие окна победы

        string playerName = PlayerPrefs.GetString("PlayerName", PlayerPrefs.GetString("Player_Name", "Алхимик")); // Чтение имени игрока

        // Проверка: пройдена ли последняя локация (Магический Рынок, индекс 2)?
        bool marketCompleted = locations != null && locations.Count > 2 && 
                              (locations[2].isEasyCompleted || locations[2].isNormalCompleted || locations[2].isHardCompleted || locations[2].IsFullyCompleted);

        // Если игрок прошел 3-ю комнату (Магический / Антикварный Рынок) — сюжетный переход к Диалогу с Котом и Защите Котлов!
        if (marketCompleted)
        {
            Debug.Log($"Все 3 комнаты пройдены игроком {playerName}! Завершение поиска предметов и переход к диалогу с Котом (Защита Котлов)."); // Лог
            
            // Закрываем окно мини-игры полностью
            if (locationSelectPopup != null) locationSelectPopup.SetActive(false); // Скрытие выбора комнат
            gameObject.SetActive(false); // Отключение панели мини-игры Поиска Предметов

            // Запуск диалога Кота с предложением сыграть в Защиту Котлов через DialogueSystem_Manager
            if (DialogueSystem_Manager.Instance != null) // Если центральный менеджер диалогов доступен
            {
                DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление интерфейса
                DialogueSystem_Manager.Instance.StartPostHiddenObjectDialogue(); // Запуск диалога Кота о дожде и Защите Котлов
                return; // Выход
            }

            // Фоллбэк: если назначена панель Защиты Котлов напрямую
            if (cauldronDefenseGamePanel != null) // Если игра защиты котлов подключена
            {
                cauldronDefenseGamePanel.SetActive(true); // Включение игры Защиты Котлов
                CauldronDefense_Minigame cd = cauldronDefenseGamePanel.GetComponent<CauldronDefense_Minigame>(); // Компонент игры
                if (cd != null) cd.OpenMinigame(); // Запуск игры защиты котлов
                return; // Выход
            }
        }

        // Если завершена только 1-я или 2-я комната — возвращаем игрока к выбору комнат:
        // предыдущая завершенная комната блокируется, а следующая комната открывается!
        ShowLocationSelectionScreen(); // Возврат к экрану выбора комнат с обновленными кнопками и замками
        Debug.Log($"Награды получены игроком {playerName}! Возврат к меню комнат: следующая комната разблокирована, пройденная заблокирована."); // Лог
    }

    private void TriggerCatRecordUnlockDialog() // Отображение диалогового окна Кота об открытии режима рекордов
    {
        if (catRecordUnlockedDialog) // Если диалоговое окно задано
        {
            catRecordUnlockedDialog.SetActive(true); // Включение окна
            if (catSpeechText) // Если текст речи Кота есть
            {
                catSpeechText.text = // Реплика Кота
                    "Мяу! Невероятно, Мастер! Ты блестяще преодолел все три локации на всех сложностях!\n\n" +
                    "Теперь тебе открывается высшее испытание: «Становление Рекорда»!\n" +
                    "Испытай свои силы: Блиц-поиск на время, Лавина предметов (20-30 шт) и Мерцающие вспышки с наградами от 1 до 10 Кристаллов раз в месяц!";
            }
        }
    }

    public void OpenRecordModeSelection() // Открытие меню выбора испытания рекордов
    {
        if (catRecordUnlockedDialog) catRecordUnlockedDialog.SetActive(false); // Скрытие диалога Кота
        if (recordModeSelectPopup) recordModeSelectPopup.SetActive(true); // Включение меню испытаний
    }

    public void StartRecordMode(RecordModeType mode) // Запуск выбранного испытания из режима «Становление Рекорда»
    {
        isRecordModeActive = true; // Активация флага рекордов
        currentRecordMode = mode; // Сохранение типа испытания

        if (recordModeSelectPopup) recordModeSelectPopup.SetActive(false); // Скрытие попапа выбора
        if (locationSelectPopup) locationSelectPopup.SetActive(false); // Скрытие выбора локаций
        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(true); // Включение игрового поля

        ClearActiveItems(); // Очистка предметов
        itemsFoundInCurrentRound = 0; // Сброс счетчика найденных
        isGameRunning = true; // Запуск игры

        if (locations.Count > 0 && locations[0].backgroundSprite != null) // Если есть фон
            backgroundLocationImage.sprite = locations[UnityEngine.Random.Range(0, locations.Count)].backgroundSprite; // Случайный фон

        switch (mode) // Ветвление по режимам испытаний
        {
            case RecordModeType.TimeRush: // Испытание: Блиц-поиск на время
                recordTargetCount = 15; // 15 предметов
                roundTimer = 40f; // Экстремальное время на 15 предметов (40 секунд)
                if (locationTitleText) locationTitleText.text = "★ СТАНОВЛЕНИЕ РЕКОРДА: БЛИЦ ★"; // Заголовок блица
                SpawnRecordItems(recordTargetCount); // Спавн предметов
                break; // Выход

            case RecordModeType.BigCountSurge: // Испытание: Лавина предметов
                recordTargetCount = UnityEngine.Random.Range(20, 31); // 20-30 предметов
                roundTimer = 150f; // 150 секунд
                if (locationTitleText) locationTitleText.text = $"★ СТАНОВЛЕНИЕ РЕКОРДА: ЛАВИНА ({recordTargetCount} шт) ★"; // Заголовок лавины
                SpawnRecordItems(recordTargetCount); // Спавн предметов
                break; // Выход

            case RecordModeType.FlickerSpawn: // Испытание: Мерцающее появление
                recordTargetCount = UnityEngine.Random.Range(20, 31); // 20-30 предметов
                roundTimer = 180f; // 180 секунд
                if (locationTitleText) locationTitleText.text = $"★ СТАНОВЛЕНИЕ РЕКОРДА: МЕРЦАНИЕ ({recordTargetCount} шт) ★"; // Заголовок мерцания
                if (flickerCoroutine != null) StopCoroutine(flickerCoroutine); // Остановка старого мерцания
                flickerCoroutine = StartCoroutine(FlickerSpawnRoutine(recordTargetCount)); // Запуск корутины мерцания
                break; // Выход
        }

        UpdateUI(); // Обновление счетчиков UI
    }

    private void SpawnRecordItems(int count) // Генерация предметов для рекордных испытаний из общего пула всех локаций
    {
        List<Sprite> combinedPool = new List<Sprite>(); // Объединенный пул спрайтов
        foreach (var loc in locations) combinedPool.AddRange(loc.itemsPool); // Сбор всех спрайтов предметов
        if (combinedPool.Count == 0) return; // Проверка на пустоту

        List<Vector2> usedPositions = new List<Vector2>(); // Список занятых позиций для исключения перекрытий

        for (int i = 0; i < count; i++) // Спавн нужного числа предметов
        {
            Sprite itemSprite = combinedPool[i % combinedPool.Count]; // Выбор спрайта
            GameObject slotUi = null; // Слот цели
            if (targetIconsContainer != null) // Если контейнер есть
            {
                if (targetItemSlotPrefab != null) slotUi = Instantiate(targetItemSlotPrefab, targetIconsContainer); // Инстанцирование слота в панели целей
                else slotUi = CreateDefaultTargetSlot(itemSprite); // Создание базового слота
            }

            Vector2 spawnPos = GetSmartNonOverlappingPosition(usedPositions); // Расчет непересекающейся позиции для рекорда
            usedPositions.Add(spawnPos); // Фиксация позиции в списке

            SpawnClickableItemOnBackground(itemSprite, slotUi, spawnPos); // Спавн интерактивного предмета на поле
        }
    }

    private IEnumerator FlickerSpawnRoutine(int targetCount) // Корутина последовательного появления и исчезновения мерцающих предметов
    {
        List<Sprite> combinedPool = new List<Sprite>(); // Объединенный пул спрайтов
        foreach (var loc in locations) combinedPool.AddRange(loc.itemsPool); // Сбор всех предметов
        if (combinedPool.Count == 0) yield break; // Проверка пула

        for (int i = 0; i < targetCount; i++) // Поочередный спавн предметов
        {
            if (!isGameRunning || !isRecordModeActive) yield break; // Прерывание если игра остановлена

            yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 1.8f)); // Случайная пауза перед появлением

            Sprite itemSprite = combinedPool[UnityEngine.Random.Range(0, combinedPool.Count)]; // Случайный спрайт предмета
            GameObject clickable = new GameObject("FlickerItem_" + i, typeof(RectTransform), typeof(Image), typeof(Button)); // Создание объекта
            clickable.transform.SetParent(backgroundContentRoot, false); // Размещение на фоне
            activeClickableItems.Add(clickable); // Добавление в список активных

            RectTransform rt = clickable.GetComponent<RectTransform>(); // RectTransform
            rt.sizeDelta = new Vector2(68f, 68f); // Размеры 68x68
            rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-480f, 480f), UnityEngine.Random.Range(-260f, 260f)); // Случайная позиция

            Image img = clickable.GetComponent<Image>(); // Image
            img.sprite = itemSprite; // Спрайт
            img.preserveAspect = true; // Пропорции

            Button btn = clickable.GetComponent<Button>(); // Button
            btn.onClick.AddListener(() => // Подписка на клик
            {
                activeClickableItems.Remove(clickable); // Удаление из активных
                Destroy(clickable); // Уничтожение
                OnItemFound(); // Обработка нахождения
            });

            StartCoroutine(DisappearAfterTime(clickable, 1.4f)); // Запуск авто-исчезновения через 1.4 секунды
        }
    }

    private IEnumerator DisappearAfterTime(GameObject obj, float delay) // Корутина исчезновения ненайденного мерцающего предмета
    {
        yield return new WaitForSeconds(delay); // Пауза жизни предмета
        if (obj != null) // Если объект еще не был нажат игроком
        {
            activeClickableItems.Remove(obj); // Удаление из активного списка
            Destroy(obj); // Уничтожение объекта
        }
    }

    private void CompleteRecordModeVictory() // Завершение рекордного испытания с начислением кристаллов
    {
        isGameRunning = false; // Остановка таймера
        int crystalsReward = 1; // Базовая награда кристаллами

        switch (currentRecordMode) // Расчет кристаллов по типу рекорда
        {
            case RecordModeType.TimeRush: crystalsReward = 1; break; // Блиц (+1)
            case RecordModeType.BigCountSurge: crystalsReward = UnityEngine.Random.Range(1, 6); break; // Лавина (+1..5)
            case RecordModeType.FlickerSpawn: crystalsReward = UnityEngine.Random.Range(1, 11); break; // Мерцание (+1..10)
        }

        if (Avatar_Manager.Instance != null) // Если менеджер аватара активен
        {
            Avatar_Manager.Instance.AddCrystals(crystalsReward); // Начисление кристаллов игроку
        }

        if (victoryPopupPanel) // Если окно победы задано
        {
            victoryPopupPanel.SetActive(true); // Включение окна победы
            if (victoryTitleText) victoryTitleText.text = "★ ВЕЛИКИЙ РЕКОРД УСТАНОВЛЕН! ★"; // Заголовок победы рекорда
            if (victoryRewardsText) // Текст наград
            {
                victoryRewardsText.text = $"Вы одолели тяжелейшее испытание месяца!\nПолучено Кристаллов: +{crystalsReward}"; // Описание полученных кристаллов без эмодзи
            }
        }
    }

    private void Update() // Покадровый таймер и обработка зумирования/перемещения камеры
    {
        if (!isGameRunning) return; // Пропуск если игра не активна

        roundTimer -= Time.deltaTime; // Отсчет времени раунда
        if (timerText) // Если текст таймера назначен
        {
            int min = Mathf.FloorToInt(Mathf.Max(0, roundTimer) / 60f); // Минуты
            int sec = Mathf.FloorToInt(Mathf.Max(0, roundTimer) % 60f); // Секунды
            timerText.text = $"{min:00}:{sec:00}"; // Отображение времени раунда без эмодзи
            if (roundTimer < 15f) timerText.color = new Color(1f, 0.3f, 0.3f, 1f); // Красное предупреждение
            else timerText.color = new Color(1f, 0.9f, 0.4f, 1f); // Золотисто-желтый
        }

        if (roundTimer <= 0) // Если время вышло
        {
            GameOverTimeout(); // Завершение раунда по таймауту
        }

        HandleTouchAndMouseZoomPan(); // Обработка жестов зума и мыши
    }

    private void GameOverTimeout() // Обработка окончания времени на поиск предметов
    {
        isGameRunning = false; // Остановка игры
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine); // Остановка корутины мерцания
        ClearActiveItems(); // Очистка предметов

        if (victoryPopupPanel) // Если окно уведомления задано
        {
            victoryPopupPanel.SetActive(true); // Показ окна
            if (victoryTitleText) victoryTitleText.text = "Время вышло!"; // Заголовок проигрыша
            if (victoryRewardsText) victoryRewardsText.text = "Попробуйте снова преодолеть это испытание!"; // Описание
        }
    }

    private void UseHint() // Использование подсказки Кота для подсветки предмета
    {
        if (availableHints <= 0 || activeClickableItems.Count == 0) return; // Проверка остатка подсказок и предметов
        availableHints--; // Уменьшение счетчика подсказок
        if (hintCountText) hintCountText.text = $"Подсказка: {availableHints}"; // Обновление счетчика на кнопке без эмодзи

        GameObject itemToHighlight = activeClickableItems[0]; // Выбор первого активного предмета
        if (itemToHighlight != null) // Если предмет доступен
        {
            StartCoroutine(PulseHintEffect(itemToHighlight)); // Запуск эффекта пульсации
        }
    }

    private IEnumerator PulseHintEffect(GameObject obj) // Корутина 3-кратной пульсации подсказанного предмета
    {
        Transform t = obj.transform; // Трансформ объекта
        Vector3 originalScale = t.localScale; // Запоминание базового масштаба
        for (int i = 0; i < 3; i++) // 3 пульсирующих цикла
        {
            if (obj == null) yield break; // Проверка на уничтожение
            t.localScale = originalScale * 1.5f; // Увеличение в 1.5 раза
            yield return new WaitForSeconds(0.2f); // Пауза 0.2 сек
            t.localScale = originalScale; // Возврат в норму
            yield return new WaitForSeconds(0.2f); // Пауза 0.2 сек
        }
    }

    private void ChangeZoom(float delta) // Изменение масштаба кнопками интерфейса
    {
        currentZoom = Mathf.Clamp(currentZoom + delta, minZoom, maxZoom); // Ограничение масштаба
        ApplyZoomAndPan(); // Применение к сцене
    }

    private void ResetZoomAndPan() // Сброс приближения и положения камеры
    {
        currentZoom = 1.0f; // Масштаб 100%
        panOffset = Vector2.zero; // Сброс смещения
        ApplyZoomAndPan(); // Применение
    }

    private void HandleTouchAndMouseZoomPan() // Безопасная обработка зума для нового и старого Input System
    {
        float scroll = 0f; // Величина прокрутки
        
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null) // Новый Input System
        {
            scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y * 0.01f; // Чтение колесика
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            scroll = Input.GetAxis("Mouse ScrollWheel"); // Старый Input Manager
        }
        catch { }
#endif

        if (Mathf.Abs(scroll) > 0.01f) // Если колесико крутится
        {
            currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed * 3f, minZoom, maxZoom); // Ограничение зума
            ApplyZoomAndPan(); // Применение зума
        }
    }

    private void ApplyZoomAndPan() // Применение параметров масштабирования и панорамирования к корневому объекту сцены
    {
        if (backgroundContentRoot) // Если корневой контейнер назначен
        {
            backgroundContentRoot.localScale = new Vector3(currentZoom, currentZoom, 1f); // Установка масштаба
            backgroundContentRoot.anchoredPosition = panOffset; // Установка смещения
        }
    }

    private void UpdateUI() // Обновление счетчиков оставшихся предметов, раундов и подсказок в интерфейсе
    {
        if (itemsRemainingText) // Если текст остатка задан
        {
            int total = isRecordModeActive ? recordTargetCount : (currentDifficulty != null ? currentDifficulty.itemsPerRound : 0); // Общее число
            itemsRemainingText.text = $"Осталось: {Mathf.Max(0, total - itemsFoundInCurrentRound)}"; // Отображение остатка
        }

        if (currentRoundText) // Если текст раунда задан
        {
            if (isRecordModeActive) // В режиме рекордов
                currentRoundText.text = "Рекордный раунд"; // Текст рекорда
            else if (currentDifficulty != null) // В обычном режиме
                currentRoundText.text = $"Этап {currentRoundIndex} из {currentDifficulty.roundsRequired}"; // Номер раунда
        }

        if (hintCountText) // Если текст подсказок есть
            hintCountText.text = $"Подсказка: {availableHints}"; // Количество подсказок без эмодзи
    }

    private void ClearActiveItems() // Очистка всех созданных интерактивных предметов и иконок целей
    {
        foreach (var item in activeClickableItems) // Перебор активных предметов
        {
            if (item != null) Destroy(item); // Уничтожение каждого предмета
        }
        activeClickableItems.Clear(); // Очистка списка

        if (targetIconsContainer != null) // Если контейнер иконок целей назначен
        {
            foreach (Transform child in targetIconsContainer) // Перебор всех дочерних слотов
            {
                Destroy(child.gameObject); // Уничтожение каждого слота
            }
        }
    }

    public void CloseGame() // Закрытие игрового окна или всей мини-игры
    {
        isGameRunning = false; // Остановка игрового процесса
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine); // Остановка корутины мерцания
        ClearActiveItems(); // Удаление активных предметов

        // Если вызов произошел из экрана выбора локаций — закрываем мини-игру полностью
        if (locationSelectPopup != null && locationSelectPopup.activeSelf)
        {
            locationSelectPopup.SetActive(false); // Скрытие меню локаций
            if (hiddenObjectPanel != null && hiddenObjectPanel != gameObject) hiddenObjectPanel.SetActive(false); // Скрытие игрового экрана
            gameObject.SetActive(false); // Отключение объекта мини-игры
            
            if (DialogueSystem_Manager.Instance != null) // Восстановление интерфейса
            {
                DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление кнопок и аватарки
            }
            return;
        }

        // Если вызов произошел во время геймплея поиска — возвращаемся к выбору локаций
        if (hiddenObjectPanel != null && hiddenObjectPanel != gameObject) hiddenObjectPanel.SetActive(false); // Скрытие игрового экрана
        if (viewportContainer != null) viewportContainer.gameObject.SetActive(false); // Скрытие вьюпорта
        if (topStatusBar != null) topStatusBar.SetActive(false); // Скрытие верхнего бара
        if (bottomTargetsBar != null) bottomTargetsBar.SetActive(false); // Скрытие нижней панели целей
        if (difficultySelectPopup != null) difficultySelectPopup.SetActive(false); // Скрытие окна сложностей
        if (recordModeSelectPopup != null) recordModeSelectPopup.SetActive(false); // Скрытие окна рекордов
        if (victoryPopupPanel != null) victoryPopupPanel.SetActive(false); // Скрытие окна победы

        ShowLocationSelectionScreen(); // Возврат к экрану выбора 3 комнат
    }

    private void SaveCompletionProgress() // Сохранение статуса прохождения 3 локаций и сложностей в PlayerPrefs
    {
        for (int i = 0; i < locations.Count; i++) // Перебор всех локаций
        {
            PlayerPrefs.SetInt($"Loc_{i}_Easy", locations[i].isEasyCompleted ? 1 : 0); // Сохранение легкой сложности
            PlayerPrefs.SetInt($"Loc_{i}_Norm", locations[i].isNormalCompleted ? 1 : 0); // Сохранение нормальной сложности
            PlayerPrefs.SetInt($"Loc_{i}_Hard", locations[i].isHardCompleted ? 1 : 0); // Сохранение сложной сложности
        }
        PlayerPrefs.Save(); // Запись на диск
    }

    private void LoadCompletionProgress() // Загрузка статуса завершения локаций из PlayerPrefs
    {
        for (int i = 0; i < locations.Count; i++) // Перебор всех локаций
        {
            locations[i].isEasyCompleted = PlayerPrefs.GetInt($"Loc_{i}_Easy", 0) == 1; // Загрузка легкой сложности
            locations[i].isNormalCompleted = PlayerPrefs.GetInt($"Loc_{i}_Norm", 0) == 1; // Загрузка нормальной сложности
            locations[i].isHardCompleted = PlayerPrefs.GetInt($"Loc_{i}_Hard", 0) == 1; // Загрузка сложной сложности
        }
    }

    public void RefreshLocationCardsUI() // Обновление текста, интерактивности и иконок замков для 3 комнат
    {
        string[] defaultNames = { "ЛАВКА АЛХИМИКА", "ДОМ АЛХИМИКА", "МАГИЧЕСКИЙ РЫНОК" }; // Названия 3 комнат в соответствии с визуальным стилем

        for (int i = 0; i < 3; i++) // Перебор 3 комнат
        {
            Button btn = null; // Ссылка на кнопку
            if (locationButtons != null && i < locationButtons.Count && locationButtons[i] != null)
                btn = locationButtons[i];
            else if (i < locations.Count && locations[i].locationCardButton != null)
                btn = locations[i].locationCardButton;

            if (btn == null && locationSelectPopup != null)
            {
                string btnName = $"Location_{i + 1}_Button";
                string[] possibleNames = { 
                    $"Location_{i + 1}_Button", $"Card_{i + 1}", $"Location_{i + 1}", $"Btn_Loc_{i + 1}",
                    i == 0 ? "Shop_Button" : (i == 1 ? "House_Button" : "Market_Button"),
                    i == 0 ? "Button_Shop" : (i == 1 ? "Button_House" : "Button_Market"),
                    i == 0 ? "Лавка Алхимика" : (i == 1 ? "Дом Алхимика" : "Магический Рынок")
                };

                foreach (var name in possibleNames)
                {
                    Transform t = locationSelectPopup.transform.Find(name);
                    if (t != null)
                    {
                        btn = t.GetComponent<Button>() ?? t.GetComponentInChildren<Button>();
                        if (btn != null) break;
                    }
                }
            }

            bool isUnlocked = IsLocationUnlocked(i); // Проверка доступности комнаты: Лавка (0) -> Дом (1) -> Рынок (2)

            if (btn != null)
            {
                btn.interactable = isUnlocked; // Включение/отключение интерактивности кнопки

                // Поддержка CanvasGroup для плавного затемнения заблокированных комнат
                CanvasGroup cg = btn.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = isUnlocked ? 1.0f : 0.45f; // Полноценная яркость для открытой комнаты, приглушенная для закрытых
                    cg.interactable = isUnlocked; // Разрешение кликов
                    cg.blocksRaycasts = isUnlocked; // Блокировка лучей
                }

                // Обновление всех графических элементов кнопки (рамок, кристаллов, фона)
                Image[] allImages = btn.GetComponentsInChildren<Image>(true);
                foreach (var img in allImages)
                {
                    if (img == null) continue;
                    string imgLower = img.gameObject.name.ToLower();
                    if (imgLower.Contains("lock") || imgLower.Contains("padlock") || imgLower.Contains("замок"))
                    {
                        img.gameObject.SetActive(!isUnlocked); // Замок отображается только при блокировке
                    }
                    else
                    {
                        img.color = isUnlocked ? Color.white : new Color(0.48f, 0.48f, 0.52f, 0.75f); // Затемнение для закрытых комнат
                    }
                }

                // Поиск и переключение оверлея замка в списке инспектора
                if (locationLockOverlays != null && i < locationLockOverlays.Count && locationLockOverlays[i] != null)
                {
                    locationLockOverlays[i].SetActive(!isUnlocked); // Переключение оверлея из инспектора
                }

                // Получение базового названия комнаты
                string locTitle = (i < locations.Count && !string.IsNullOrEmpty(locations[i].locationName)) ? locations[i].locationName : defaultNames[i];

                // Обновление текста
                TextMeshProUGUI statusTxt = btn.transform.Find("Status_Text")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI mainTmp = btn.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>() ?? btn.GetComponentInChildren<TextMeshProUGUI>();

                bool isCompleted = (i < locations.Count) && (locations[i].isEasyCompleted || locations[i].isNormalCompleted || locations[i].isHardCompleted || locations[i].IsFullyCompleted);

                if (statusTxt != null) // Если есть отдельная строка для статуса
                {
                    if (isCompleted)
                    {
                        statusTxt.text = "<color=#99D98C>[Пройдено] Заблокировано</color>"; // Пройдена и заблокирована по сюжету
                    }
                    else if (!isUnlocked)
                    {
                        statusTxt.text = $"<color=#FF7B7B>[Закрыто] Пройдите комн. {i}</color>"; // Заблокирована до прохождения предыдущей
                    }
                    else
                    {
                        statusTxt.text = "<color=#FFE66D>[Открыто] Доступно</color>"; // Текущая активная комната
                    }
                }
                else if (mainTmp != null) // Если на кнопке единый Text (TMP)
                {
                    // Сохраняем дизайнерский текст и регулируем прозрачность
                    mainTmp.alpha = isUnlocked ? 1.0f : 0.5f; // Яркость шрифта
                }
            }
        }
    }

    public void InitializeDefaultConfigurationsIfEmpty() // Инициализация 3 локаций по умолчанию, если список в инспекторе пуст
    {
        if (locations.Count == 0) // Если локации еще не созданы
        {
            // 1. Локация: Алхимическая лавка Кота
            LocationConfig shop = new LocationConfig { locationId = "Shop", locationName = "Алхимическая лавка Кота", locationDescription = "Полки с древними снадобьями, ретортами и корешками" }; // Создание лавки
            shop.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Easy, tierName = "Легкий", itemsPerRound = 5, roundsRequired = 3, timeLimitPerRound = 90f, rewardGold = 1000, rewardStones = 3, rewardScrolls = 1 }); // Легкая сложность
            shop.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Normal, tierName = "Нормальный", itemsPerRound = 10, roundsRequired = 3, timeLimitPerRound = 90f, rewardGold = 2500, rewardStones = 6, rewardScrolls = 3, rewardExpPotion100 = 1 }); // Нормальная сложность
            shop.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Hard, tierName = "Сложный", itemsPerRound = 15, roundsRequired = 5, timeLimitPerRound = 90f, rewardGold = 5000, rewardStones = 15, rewardScrolls = 10, rewardExpPotion500 = 1, rewardMasteryPotion100 = 1 }); // Сложная сложность
            EnsureDefaultSpritesForLocation(shop); // Создание процедурных спрайтов
            locations.Add(shop); // Добавление лавки

            // 2. Локация: Старый заброшенный дом
            LocationConfig house = new LocationConfig { locationId = "House", locationName = "Старый заброшенный дом", locationDescription = "Уютные запыленные комнаты с сундуками и книгами тайн" }; // Создание дома
            house.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Easy, tierName = "Легкий", itemsPerRound = 6, roundsRequired = 3, timeLimitPerRound = 90f, rewardGold = 1500, rewardStones = 5, rewardScrolls = 1 }); // Легкая сложность
            house.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Normal, tierName = "Нормальный", itemsPerRound = 12, roundsRequired = 3, timeLimitPerRound = 90f, rewardGold = 3500, rewardStones = 9, rewardScrolls = 3, rewardExpPotion100 = 2 }); // Нормальная сложность
            house.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Hard, tierName = "Сложный", itemsPerRound = 15, roundsRequired = 5, timeLimitPerRound = 90f, rewardGold = 7000, rewardStones = 20, rewardScrolls = 12, rewardExpPotion500 = 2, rewardMasteryPotion100 = 1 }); // Сложная сложность
            EnsureDefaultSpritesForLocation(house); // Создание спрайтов
            locations.Add(house); // Добавление дома

            // 3. Локация: Антикварный рынок
            LocationConfig market = new LocationConfig { locationId = "Market", locationName = "Антикварный рынок", locationDescription = "Шумная площадь магов, полная редких артефактов и самоцветов" }; // Создание рынка
            market.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Easy, tierName = "Легкий", itemsPerRound = 6, roundsRequired = 3, timeLimitPerRound = 90f, rewardGold = 2000, rewardStones = 8, rewardScrolls = 2, rewardExpPotion100 = 3 }); // Легкая сложность
            market.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Normal, tierName = "Нормальный", itemsPerRound = 12, roundsRequired = 3, timeLimitPerRound = 90f, rewardGold = 5000, rewardStones = 15, rewardScrolls = 5, rewardExpPotion100 = 6, rewardMasteryPotion100 = 3 }); // Нормальная сложность
            market.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Hard, tierName = "Сложный", itemsPerRound = 15, roundsRequired = 5, timeLimitPerRound = 90f, rewardGold = 10000, rewardStones = 30, rewardScrolls = 15, rewardExpPotion500 = 3, rewardMasteryPotion500 = 1 }); // Сложная сложность
            EnsureDefaultSpritesForLocation(market); // Создание спрайтов
            locations.Add(market); // Добавление рынка
        }
        else
        {
            // Синхронизация названий существующих локаций с кнопками
            if (locations.Count > 0 && (string.IsNullOrEmpty(locations[0].locationName) || locations[0].locationName == "Shop")) locations[0].locationName = "Алхимическая лавка Кота";
            if (locations.Count > 1 && (string.IsNullOrEmpty(locations[1].locationName) || locations[1].locationName == "House")) locations[1].locationName = "Старый заброшенный дом";
            if (locations.Count > 2 && (string.IsNullOrEmpty(locations[2].locationName) || locations[2].locationName == "Market")) locations[2].locationName = "Антикварный рынок";

            foreach (var loc in locations) // Перебор существующих локаций
            {
                if (loc.itemsPool.Count == 0) EnsureDefaultSpritesForLocation(loc); // Добавление спрайтов если пуст
            }
        }
    }

    private void EnsureDefaultSpritesForLocation(LocationConfig loc) // Создание красивых процедурных спрайтов предметов, если спрайты не назначены
    {
        if (loc.itemsPool == null) loc.itemsPool = new List<Sprite>(); // Создание списка

        if (loc.backgroundSprite == null) // Создание атмосферного процедурного фона
        {
            Color bgColor = loc.locationId == "Shop" ? new Color(0.14f, 0.10f, 0.07f) : (loc.locationId == "House" ? new Color(0.08f, 0.12f, 0.16f) : new Color(0.15f, 0.08f, 0.18f)); // Цвет фона
            loc.backgroundSprite = CreateGradientSprite(bgColor, Color.black, 512, 384); // Генерация спрайта фона
        }

        if (loc.itemsPool.Count == 0) // Если пул предметов пуст
        {
            string[] names = { "Herb", "Crystal", "RuneStone", "PotionFlask", "Scroll", "GoldenKey", "MysticGem", "Feather", "Hourglass", "StarDust", "DragonClaw", "MoonAmulet" }; // Названия
            Color[] colors = { new Color(0.3f, 0.9f, 0.4f), new Color(0.2f, 0.7f, 1f), new Color(0.9f, 0.6f, 0.2f), new Color(1f, 0.25f, 0.4f), new Color(0.85f, 0.8f, 0.6f), new Color(1f, 0.85f, 0.2f), new Color(0.7f, 0.3f, 1f), new Color(0.95f, 0.95f, 0.95f), new Color(0.6f, 0.9f, 0.9f), new Color(1f, 0.9f, 0.1f), new Color(0.9f, 0.3f, 0.2f), new Color(0.4f, 0.5f, 1f) }; // Цвета предметов

            for (int i = 0; i < names.Length; i++) // Генерация 12 спрайтов
            {
                Sprite sp = CreateProceduralIconSprite(names[i], colors[i % colors.Length]); // Создание спрайта
                loc.itemsPool.Add(sp); // Добавление в пул
            }
        }
    }

    private Sprite CreateGradientSprite(Color c1, Color c2, int width, int height) // Генерация фонового градиента
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false); // Создание текстуры
        for (int y = 0; y < height; y++) // По высоте
        {
            float t = (float)y / height; // Коэффициент смешивания
            Color rowColor = Color.Lerp(c1, c2, t); // Интерполяция цвета
            for (int x = 0; x < width; x++) // По ширине
            {
                tex.SetPixel(x, y, rowColor); // Запись пикселя
            }
        }
        tex.Apply(); // Применение изменений
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f)); // Создание спрайта
    }

    private Sprite CreateProceduralIconSprite(string name, Color mainColor) // Создание иконки предмета с цветным ядром и золотой каймой
    {
        int size = 64; // Размер 64x64
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false); // Создание текстуры
        Vector2 center = new Vector2(size / 2f, size / 2f); // Центр
        float radius = size * 0.42f; // Радиус формы

        for (int y = 0; y < size; y++) // По строкам
        {
            for (int x = 0; x < size; x++) // По столбцам
            {
                float dist = Vector2.Distance(new Vector2(x, y), center); // Дистанция до центра
                if (dist < radius) // Внутри круга
                {
                    float edge = radius - dist; // Близость к краю
                    if (edge < 3f) tex.SetPixel(x, y, new Color(1f, 0.85f, 0.3f, 1f)); // Золотистый контур
                    else tex.SetPixel(x, y, mainColor); // Основной цвет предмета
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear); // Прозрачный фон
                }
            }
        }
        tex.Apply(); // Применение
        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f)); // Спрайт
        s.name = name; // Имя спрайта
        return s; // Возврат
    }

    /// <summary>
    /// Автоматический поиск существующих UI объектов или генерация недостающих панелей
    /// </summary>
    public void EnsureUIHierarchy()
    {
        if (isUIHierarchyBuilt) return; // Пропуск если уже построено

        // 1. Поиск существующих дочерних объектов по именам
        FindExistingHierarchyElements();

        // 2. Если панели отсутствуют — автосоздание надежной структуры
        CreateMissingUIPanels();

        isUIHierarchyBuilt = true; // Отметка сборки
    }

    private void FindExistingHierarchyElements() // Поиск ссылок среди дочерних объектов
    {
        if (locationSelectPopup == null) 
            locationSelectPopup = transform.Find("Location_Selection_Popup")?.gameObject ?? transform.Find("Location_Select_Popup")?.gameObject ?? transform.Find("LocationSelectPopup")?.gameObject ?? transform.Find("LocationSelectPanel")?.gameObject;
        
        if (difficultySelectPopup == null) 
            difficultySelectPopup = transform.Find("Difficulty_Selection_Popup")?.gameObject ?? transform.Find("Difficulty_Select_Popup")?.gameObject ?? transform.Find("DifficultySelectPopup")?.gameObject ?? transform.Find("Difficulty_Selection_Panel")?.gameObject;
        
        if (hiddenObjectPanel == null) 
            hiddenObjectPanel = transform.Find("HiddenObject_Panel")?.gameObject ?? transform.Find("Game_Play_Panel")?.gameObject ?? transform.Find("Active_Stage_Panel")?.gameObject ?? gameObject;
        
        if (victoryPopupPanel == null) 
            victoryPopupPanel = transform.Find("Victory_Location_Popup")?.gameObject ?? transform.Find("Victory_Popup_Panel")?.gameObject ?? transform.Find("VictoryPopupPanel")?.gameObject ?? transform.Find("Result_Summary_Popup_Panel")?.gameObject;
        
        if (recordModeSelectPopup == null) 
            recordModeSelectPopup = transform.Find("Record_Mode_Selection_Popup")?.gameObject ?? transform.Find("Record_Mode_Select_Popup")?.gameObject ?? transform.Find("RecordModePopup")?.gameObject;
        
        if (catRecordUnlockedDialog == null) 
            catRecordUnlockedDialog = transform.Find("Cat_Record_Unlocked_Dialog")?.gameObject ?? transform.Find("CatRecordDialog")?.gameObject;

        if (topStatusBar == null)
            topStatusBar = transform.Find("Top_Status_Bar")?.gameObject ?? transform.Find("Top_Bar")?.gameObject;

        if (bottomTargetsBar == null)
            bottomTargetsBar = transform.Find("Bottom_Targets_Bar")?.gameObject ?? transform.Find("Target_Icons_Bar")?.gameObject;

        // Поиск кнопок комнат внутри Location_Selection_Popup
        if (locationSelectPopup != null && (locationButtons == null || locationButtons.Count == 0))
        {
            locationButtons = new List<Button>();
            for (int i = 1; i <= 3; i++)
            {
                Transform bTrans = locationSelectPopup.transform.Find($"Location_{i}_Button") ?? locationSelectPopup.transform.Find($"Card_{i}") ?? locationSelectPopup.transform.Find($"Location_{i}");
                if (bTrans != null)
                {
                    Button b = bTrans.GetComponent<Button>() ?? bTrans.GetComponentInChildren<Button>();
                    if (b != null) locationButtons.Add(b);
                }
            }
        }

        // Поиск Viewport Container и Background
        if (viewportContainer == null)
        {
            viewportContainer = transform.Find("Fullscreen_Viewport_Container")?.GetComponent<RectTransform>() ?? transform.Find("Viewport_Container")?.GetComponent<RectTransform>();
            if (viewportContainer == null && hiddenObjectPanel != null && hiddenObjectPanel != gameObject)
            {
                viewportContainer = hiddenObjectPanel.transform.Find("Fullscreen_Viewport_Container")?.GetComponent<RectTransform>() ?? hiddenObjectPanel.transform.Find("Viewport_Container")?.GetComponent<RectTransform>();
            }
        }

        if (viewportContainer != null)
        {
            if (backgroundContentRoot == null)
                backgroundContentRoot = viewportContainer.Find("Background_Content_Root")?.GetComponent<RectTransform>() ?? viewportContainer.Find("Background_Root")?.GetComponent<RectTransform>() ?? viewportContainer.Find("Content")?.GetComponent<RectTransform>();

            if (backgroundContentRoot != null)
            {
                if (backgroundLocationImage == null)
                    backgroundLocationImage = backgroundContentRoot.Find("Background_8K_Image")?.GetComponent<Image>() ?? backgroundContentRoot.GetComponent<Image>();

                if (interactiveClickTargetsContainer == null)
                    interactiveClickTargetsContainer = backgroundContentRoot.Find("Interactive_Click_Targets")?.GetComponent<RectTransform>() ?? backgroundContentRoot;
            }
        }

        // Поиск элементов Top_Status_Bar
        if (topStatusBar != null)
        {
            if (timerText == null) timerText = topStatusBar.transform.Find("TimerText")?.GetComponent<TextMeshProUGUI>() ?? topStatusBar.transform.Find("Timer_Text")?.GetComponent<TextMeshProUGUI>();
            if (hintCatButton == null) hintCatButton = topStatusBar.transform.Find("Hint_Cat_Button")?.GetComponent<Button>() ?? topStatusBar.transform.Find("Hint_Button")?.GetComponent<Button>();
            if (closeGameButton == null) closeGameButton = topStatusBar.transform.Find("Close_Button")?.GetComponent<Button>() ?? topStatusBar.transform.Find("CloseGameButton")?.GetComponent<Button>();
        }

        // Поиск кнопки закрытия внутри Location_Selection_Popup
        if (locationSelectPopup != null && closeGameButton == null)
        {
            closeGameButton = locationSelectPopup.transform.Find("Close_Button")?.GetComponent<Button>() ?? locationSelectPopup.transform.Find("Exit_Button")?.GetComponent<Button>();
        }

        // Поиск элементов Difficulty_Selection_Popup
        if (difficultySelectPopup != null)
        {
            if (difficultyPopupTitleText == null)
                difficultyPopupTitleText = difficultySelectPopup.transform.Find("Difficulty_Title_Text")?.GetComponent<TextMeshProUGUI>() ?? difficultySelectPopup.transform.Find("Title_Text")?.GetComponent<TextMeshProUGUI>();

            if (buttonEasy == null)
                buttonEasy = difficultySelectPopup.transform.Find("Button_Easy")?.GetComponent<Button>() ?? difficultySelectPopup.transform.Find("Easy_Button")?.GetComponent<Button>();

            if (buttonNormal == null)
                buttonNormal = difficultySelectPopup.transform.Find("Button_Normal")?.GetComponent<Button>() ?? difficultySelectPopup.transform.Find("Normal_Button")?.GetComponent<Button>();

            if (buttonHard == null)
                buttonHard = difficultySelectPopup.transform.Find("Button_Hard")?.GetComponent<Button>() ?? difficultySelectPopup.transform.Find("Hard_Button")?.GetComponent<Button>();

            if (buttonBackToLocations == null)
                buttonBackToLocations = difficultySelectPopup.transform.Find("Button_Back_To_Locations")?.GetComponent<Button>() ?? 
                                         difficultySelectPopup.transform.Find("Close_Button")?.GetComponent<Button>() ?? 
                                         difficultySelectPopup.transform.Find("Back_Button")?.GetComponent<Button>() ??
                                         difficultySelectPopup.transform.Find("Button_Back")?.GetComponent<Button>();
        }

        // Поиск элементов Bottom_Targets_Bar
        if (bottomTargetsBar != null)
        {
            if (itemsRemainingText == null) itemsRemainingText = bottomTargetsBar.transform.Find("ItemsRemainingText")?.GetComponent<TextMeshProUGUI>() ?? bottomTargetsBar.transform.Find("Remaining_Text")?.GetComponent<TextMeshProUGUI>();
            if (targetIconsContainer == null) targetIconsContainer = bottomTargetsBar.transform.Find("Target_Icons_Holder")?.GetComponent<RectTransform>() ?? bottomTargetsBar.transform.Find("Container")?.GetComponent<RectTransform>() ?? bottomTargetsBar.GetComponent<RectTransform>();
        }
    }

    private void CreateMissingUIPanels() // Создание недостающих панелей на лету
    {
        // 1. Создание Location Select Popup если отсутствует
        if (locationSelectPopup == null)
        {
            locationSelectPopup = new GameObject("Location_Select_Popup", typeof(RectTransform), typeof(Image)); // Создание объекта
            locationSelectPopup.transform.SetParent(transform, false); // Вложение в корень

            RectTransform rt = locationSelectPopup.GetComponent<RectTransform>(); // RectTransform
            rt.anchorMin = Vector2.zero; // Растяжение на весь экран
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bg = locationSelectPopup.GetComponent<Image>(); // Фон попапа
            bg.color = new Color(0.06f, 0.05f, 0.08f, 0.95f); // Глубокий темный фон

            // Заголовок
            CreateTMPText(locationSelectPopup.transform, "Title", "<b>★ ПОИСК ПРЕДМЕТОВ: ВЫБОР КОМНАТЫ ★</b>", new Vector2(0f, 220f), new Vector2(800f, 60f), 28f, new Color(1f, 0.85f, 0.3f)); // Заголовок

            // 3 Карточки комнат
            float[] posXs = { -280f, 0f, 280f }; // Координаты X карточек
            for (int i = 0; i < locations.Count && i < 3; i++) // Создание 3 карточек
            {
                int idx = i; // Индекс
                GameObject card = CreateLocationCardUI(locationSelectPopup.transform, locations[i], new Vector2(posXs[i], -10f)); // Карточка
                Button btn = card.GetComponentInChildren<Button>(); // Кнопка карточки
                locations[i].locationCardButton = btn; // Привязка
                if (btn != null) btn.onClick.AddListener(() => OpenLocationDifficultySelect(idx)); // Клик
            }

            // Кнопка закрытия
            if (closeGameButton == null)
            {
                closeGameButton = CreateSimpleButton(locationSelectPopup.transform, "Close_Button", "<b>Закрыть</b>", new Vector2(0f, -230f), new Vector2(220f, 50f), new Color(0.6f, 0.2f, 0.2f)); // Кнопка закрытия
                closeGameButton.onClick.AddListener(CloseGame); // Подписка
            }
        }

        // 2. Создание Difficulty Select Popup если отсутствует
        if (difficultySelectPopup == null)
        {
            difficultySelectPopup = new GameObject("Difficulty_Select_Popup", typeof(RectTransform), typeof(Image)); // Создание объекта
            difficultySelectPopup.transform.SetParent(transform, false); // Вложение

            RectTransform rt = difficultySelectPopup.GetComponent<RectTransform>(); // RectTransform
            rt.anchorMin = Vector2.zero; // Растяжение
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bg = difficultySelectPopup.GetComponent<Image>(); // Фон
            bg.color = new Color(0.06f, 0.05f, 0.08f, 0.96f); // Темный фон

            // Заголовок сложности
            difficultyPopupTitleText = CreateTMPText(difficultySelectPopup.transform, "Diff_Title", "<b>ВЫБЕРИТЕ УРОВЕНЬ СЛОЖНОСТИ</b>", new Vector2(0f, 180f), new Vector2(700f, 60f), 26f, new Color(1f, 0.85f, 0.3f)); // Заголовок

            // Кнопки 3 сложностей
            buttonEasy = CreateSimpleButton(difficultySelectPopup.transform, "Btn_Easy", "<b>Легкий</b> (5 предм.)\n<size=70%>Золото + Камни + Свитки</size>", new Vector2(0f, 70f), new Vector2(360f, 60f), new Color(0.2f, 0.6f, 0.3f)); // Легкий
            buttonNormal = CreateSimpleButton(difficultySelectPopup.transform, "Btn_Normal", "<b>Обычный</b> (10 предм.)\n<size=70%>+ Зелье Опыта (+100 XP)</size>", new Vector2(0f, -5f), new Vector2(360f, 60f), new Color(0.2f, 0.45f, 0.8f)); // Нормальный
            buttonHard = CreateSimpleButton(difficultySelectPopup.transform, "Btn_Hard", "<b>Сложный</b> (15 предм.)\n<size=70%>+ Зелье Опыта (+500 XP) + Зелье Мастерства</size>", new Vector2(0f, -80f), new Vector2(360f, 60f), new Color(0.7f, 0.2f, 0.2f)); // Сложный

            buttonEasy.onClick.AddListener(() => StartGameWithDifficulty(0)); // Подписка 0
            buttonNormal.onClick.AddListener(() => StartGameWithDifficulty(1)); // Подписка 1
            buttonHard.onClick.AddListener(() => StartGameWithDifficulty(2)); // Подписка 2

            // Кнопка «Назад к локациям»
            buttonBackToLocations = CreateSimpleButton(difficultySelectPopup.transform, "Btn_Back", "<b><< Назад к комнатам</b>", new Vector2(0f, -170f), new Vector2(260f, 46f), new Color(0.35f, 0.30f, 0.40f)); // Кнопка назад
            buttonBackToLocations.onClick.AddListener(ShowLocationSelectionScreen); // Возврат

            difficultySelectPopup.SetActive(false); // Прячем по умолчанию
        }

        // 3. Создание Главного Игрового Экрана (hiddenObjectPanel) если отсутствует
        if (hiddenObjectPanel == null)
        {
            hiddenObjectPanel = new GameObject("HiddenObject_Panel", typeof(RectTransform), typeof(Image)); // Объект игрового поля
            hiddenObjectPanel.transform.SetParent(transform, false); // Вложение

            RectTransform rt = hiddenObjectPanel.GetComponent<RectTransform>(); // RectTransform
            rt.anchorMin = Vector2.zero; // Растяжение
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bg = hiddenObjectPanel.GetComponent<Image>(); // Фон
            bg.color = new Color(0.04f, 0.03f, 0.05f, 1f); // Темный цвет

            // Верхняя плашка информации
            GameObject topBar = new GameObject("Top_Bar", typeof(RectTransform), typeof(Image)); // Верхний бар
            topBar.transform.SetParent(hiddenObjectPanel.transform, false); // Вложение
            RectTransform topRt = topBar.GetComponent<RectTransform>(); // RectTransform
            topRt.anchorMin = new Vector2(0f, 1f); // Сверху
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.sizeDelta = new Vector2(0f, 70f); // Высота 70px
            topBar.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.15f, 0.95f); // Цвет плашки

            locationTitleText = CreateTMPText(topBar.transform, "Loc_Title", "Лавка Алхимика — Легкий", new Vector2(-220f, 0f), new Vector2(380f, 50f), 20f, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Left); // Название
            currentRoundText = CreateTMPText(topBar.transform, "Round_Text", "Этап 1 из 3", new Vector2(40f, 0f), new Vector2(180f, 50f), 18f, Color.white, TextAlignmentOptions.Center); // Раунд
            timerText = CreateTMPText(topBar.transform, "Timer_Text", "01:30", new Vector2(200f, 0f), new Vector2(140f, 50f), 22f, new Color(1f, 0.9f, 0.4f), TextAlignmentOptions.Center); // Таймер

            // Кнопка подсказки Кота
            hintCatButton = CreateSimpleButton(topBar.transform, "Hint_Button", "Подсказка: 3", new Vector2(340f, 0f), new Vector2(150f, 42f), new Color(0.85f, 0.60f, 0.15f)); // Кнопка подсказки
            hintCatButton.onClick.AddListener(UseHint); // Подписка
            hintCountText = hintCatButton.GetComponentInChildren<TextMeshProUGUI>(); // Текст подсказок

            // Viewport с фоном и интерактивными предметами
            GameObject vpObj = new GameObject("Viewport_Container", typeof(RectTransform), typeof(RectMask2D)); // Окно маски
            vpObj.transform.SetParent(hiddenObjectPanel.transform, false); // Вложение
            viewportContainer = vpObj.GetComponent<RectTransform>(); // RectTransform
            viewportContainer.anchorMin = new Vector2(0.02f, 0.18f); // Растяжение в центре
            viewportContainer.anchorMax = new Vector2(0.98f, 0.88f);
            viewportContainer.sizeDelta = Vector2.zero;

            GameObject bgRoot = new GameObject("Background_Root", typeof(RectTransform), typeof(Image)); // Контейнер фона
            bgRoot.transform.SetParent(viewportContainer, false); // Вложение в viewport
            backgroundContentRoot = bgRoot.GetComponent<RectTransform>(); // RectTransform
            backgroundContentRoot.anchorMin = Vector2.zero; // Центровка
            backgroundContentRoot.anchorMax = Vector2.one;
            backgroundContentRoot.sizeDelta = Vector2.zero;

            backgroundLocationImage = bgRoot.GetComponent<Image>(); // Image фона
            backgroundLocationImage.color = Color.white; // Белый

            // Нижняя панель целей
            GameObject botBar = new GameObject("Target_Icons_Bar", typeof(RectTransform), typeof(Image)); // Нижняя панель
            botBar.transform.SetParent(hiddenObjectPanel.transform, false); // Вложение
            RectTransform botRt = botBar.GetComponent<RectTransform>(); // RectTransform
            botRt.anchorMin = Vector2.zero; // Снизу
            botRt.anchorMax = new Vector2(1f, 0f);
            botRt.pivot = new Vector2(0.5f, 0f);
            botRt.sizeDelta = new Vector2(0f, 90f); // Высота 90px
            botBar.GetComponent<Image>().color = new Color(0.10f, 0.08f, 0.12f, 0.95f); // Цвет

            itemsRemainingText = CreateTMPText(botBar.transform, "Remaining_Text", "Осталось: 5", new Vector2(-360f, 0f), new Vector2(180f, 60f), 20f, new Color(0.5f, 1f, 0.8f), TextAlignmentOptions.Left); // Остаток

            GameObject targetCont = new GameObject("Container", typeof(RectTransform), typeof(HorizontalLayoutGroup)); // Контейнер иконок
            targetCont.transform.SetParent(botBar.transform, false); // Вложение
            targetIconsContainer = targetCont.GetComponent<RectTransform>(); // RectTransform
            targetIconsContainer.anchorMin = new Vector2(0.25f, 0.1f); // Размещение по центру
            targetIconsContainer.anchorMax = new Vector2(0.95f, 0.9f);
            targetIconsContainer.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup hlg = targetCont.GetComponent<HorizontalLayoutGroup>(); // Сетка горизонтальная
            hlg.spacing = 10f; // Отступ между слотами
            hlg.childAlignment = TextAnchor.MiddleCenter; // Центровка
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            hiddenObjectPanel.SetActive(false); // Прячем по умолчанию
        }

        // 4. Создание Victory Popup Panel если отсутствует
        if (victoryPopupPanel == null)
        {
            victoryPopupPanel = new GameObject("Victory_Popup_Panel", typeof(RectTransform), typeof(Image)); // Панель победы
            victoryPopupPanel.transform.SetParent(transform, false); // Вложение

            RectTransform rt = victoryPopupPanel.GetComponent<RectTransform>(); // RectTransform
            rt.anchorMin = Vector2.zero; // Растяжение
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bg = victoryPopupPanel.GetComponent<Image>(); // Фон
            bg.color = new Color(0.04f, 0.03f, 0.06f, 0.96f); // Темный

            // Рамка победы
            GameObject frame = new GameObject("Victory_Frame", typeof(RectTransform), typeof(Image)); // Золотая рамка
            frame.transform.SetParent(victoryPopupPanel.transform, false); // Вложение
            RectTransform frameRt = frame.GetComponent<RectTransform>(); // RectTransform
            frameRt.sizeDelta = new Vector2(560f, 380f); // Размер рамки
            frame.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.08f, 0.98f); // Фон рамки

            victoryTitleText = CreateTMPText(frame.transform, "Vic_Title", "<b>★ ПОБЕДА: КОМНАТА ЗАЧИЩЕНА! ★</b>", new Vector2(0f, 130f), new Vector2(500f, 50f), 24f, new Color(1f, 0.85f, 0.3f)); // Заголовок
            victoryRewardsText = CreateTMPText(frame.transform, "Vic_Rewards", "+1 000 Золота   +3 Камня   +1 Свиток", new Vector2(0f, 20f), new Vector2(500f, 130f), 18f, Color.white); // Награды

            claimRewardsAndBackButton = CreateSimpleButton(frame.transform, "Claim_Btn", "<b>Забрать награды и продолжить >></b>", new Vector2(0f, -120f), new Vector2(400f, 56f), new Color(0.95f, 0.70f, 0.15f)); // Кнопка сбора
            claimRewardsAndBackButton.onClick.AddListener(OnClaimRewardsClicked); // Подписка

            victoryPopupPanel.SetActive(false); // Прячем
        }
    }

    private GameObject CreateLocationCardUI(Transform parent, LocationConfig loc, Vector2 pos) // Создание красивой карточки комнаты
    {
        GameObject card = new GameObject("Card_" + loc.locationId, typeof(RectTransform), typeof(Image)); // Карточка
        card.transform.SetParent(parent, false); // Вложение

        RectTransform rt = card.GetComponent<RectTransform>(); // RectTransform
        rt.anchoredPosition = pos; // Позиция
        rt.sizeDelta = new Vector2(250f, 320f); // Размер карточки 250x320

        Image img = card.GetComponent<Image>(); // Фон карточки
        img.color = new Color(0.14f, 0.11f, 0.18f, 0.95f); // Фиолетово-темный

        CreateTMPText(card.transform, "Title", $"<b>{loc.locationName}</b>", new Vector2(0f, 115f), new Vector2(230f, 40f), 18f, new Color(1f, 0.85f, 0.3f)); // Имя комнаты
        CreateTMPText(card.transform, "Desc", loc.locationDescription, new Vector2(0f, 40f), new Vector2(220f, 90f), 13f, new Color(0.8f, 0.8f, 0.85f)); // Описание

        TextMeshProUGUI statusTxt = CreateTMPText(card.transform, "Status_Text", "<color=#CCCCCC>Не пройдено</color>", new Vector2(0f, -45f), new Vector2(220f, 30f), 14f, Color.white); // Статус
        statusTxt.name = "Status_Text";

        Button btn = CreateSimpleButton(card.transform, "Select_Btn", "<b>Исследовать</b>", new Vector2(0f, -105f), new Vector2(190f, 44f), new Color(0.85f, 0.60f, 0.15f)); // Кнопка

        return card; // Возврат карточки
    }

    private Button CreateSimpleButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor) // Хелпер создания UI-кнопки
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // Объект кнопки
        btnObj.transform.SetParent(parent, false); // Вложение

        RectTransform rt = btnObj.GetComponent<RectTransform>(); // RectTransform
        rt.anchoredPosition = pos; // Позиция
        rt.sizeDelta = size; // Размер

        Image img = btnObj.GetComponent<Image>(); // Фон кнопки
        img.color = bgColor; // Цвет

        Button btn = btnObj.GetComponent<Button>(); // Button
        ColorBlock cb = btn.colors; // Цветовая палитра
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.3f); // Подсветка
        cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.3f); // Нажатие
        btn.colors = cb; // Применение

        CreateTMPText(btnObj.transform, "Label", label, Vector2.zero, size, 16f, Color.white); // Текст

        return btn; // Возврат кнопки
    }

    private TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center) // Хелпер создания TextMeshPro
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); // Объект текста
        textObj.transform.SetParent(parent, false); // Вложение

        RectTransform rt = textObj.GetComponent<RectTransform>(); // RectTransform
        rt.anchoredPosition = pos; // Позиция
        rt.sizeDelta = size; // Размер

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>(); // TextMeshProUGUI
        tmp.text = text; // Текст
        tmp.fontSize = fontSize; // Кегль
        tmp.color = color; // Цвет
        tmp.alignment = align; // Выравнивание
#pragma warning disable CS0618 // Отключение предупреждения об устаревшем свойстве в разных версиях Unity TextMeshPro
        tmp.textWrappingMode = TextWrappingModes.Normal; // Включение переноса строк для современных версий TextMeshPro
        tmp.enableWordWrapping = true; // Поддержка для обратной совместимости старых сборок
#pragma warning restore CS0618
        tmp.overflowMode = TextOverflowModes.Ellipsis; // Обрезка многоточием

        return tmp; // Возврат
    }
}
