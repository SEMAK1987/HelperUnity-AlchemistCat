using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        public Sprite backgroundSprite; // Фоновое изображение локации
        public Button locationCardButton; // Кнопка выбора локации
        public List<Sprite> itemsPool = new List<Sprite>(); // Набор предметов для поиска в локации

        [Header("3 Уровня сложности локации")]
        public List<DifficultyConfig> difficulties = new List<DifficultyConfig>(); // Настройки 3 сложностей

        [Header("Статус прохождения")]
        public bool isEasyCompleted; // Пройден ли легкий уровень
        public bool isNormalCompleted; // Пройден ли нормальный уровень
        public bool isHardCompleted; // Пройден ли сложный уровень
        public bool IsFullyCompleted => isEasyCompleted && isNormalCompleted && isHardCompleted; // Полностью ли зачищена локация
    }

    [Header("=== Главные панели ===")]
    public GameObject hiddenObjectPanel; // Главная панель игры поиска предметов
    public GameObject locationSelectPopup; // Попап выбора локации
    public GameObject difficultySelectPopup; // Попап выбора сложности
    public Button closeGameButton; // Кнопка закрытия игры

    [Header("=== Кнопки выбора сложности ===")]
    public Button buttonEasy; // Кнопка легкой сложности
    public Button buttonNormal; // Кнопка обычной сложности
    public Button buttonHard; // Кнопка сложной сложности
    public TextMeshProUGUI difficultyPopupTitleText; // Текст заголовка выбора сложности

    [Header("=== Viewport и Зум/Панорамирование ===")]
    public RectTransform viewportContainer; // Окно просмотра локации (Viewport)
    public RectTransform backgroundContentRoot; // Корневой контейнер с фоном и предметами
    public Image backgroundLocationImage; // Спрайт фона локации
    public AspectRatioFitter backgroundAspect; // Компонент сохранения пропорций
    public float minZoom = 1.0f; // Минимальный масштаб
    public float maxZoom = 2.8f; // Максимальный зум
    public float zoomSpeed = 0.5f; // Скорость зумирования колесиком/жестом

    [Header("=== Нижняя панель целей (Target Items Bar) ===")]
    public RectTransform targetIconsContainer; // Контейнер иконок искомых предметов
    public GameObject targetItemSlotPrefab; // Префаб ячейки искомого предмета
    public TextMeshProUGUI itemsRemainingText; // Текст "Осталось: X"
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

    private void Awake() // Инициализация синглтона при пробуждении объекта
    {
        if (Instance == null) Instance = this; // Инициализация синглтона
        else Destroy(gameObject); // Уничтожение дубликата
    }

    private void Start() // Стартовая настройка конфигурации, кнопок и сохранений
    {
        InitializeDefaultConfigurationsIfEmpty(); // Инициализация локаций и предметов по умолчанию
        SetupButtons(); // Настройка кликов кнопок интерфейса
        LoadCompletionProgress(); // Загрузка сохраненного прогресса прохождения локаций
    }

    private void SetupButtons() // Назначение слушателей событий нажатия на все кнопки интерфейса
    {
        if (closeGameButton) closeGameButton.onClick.AddListener(CloseGame); // Кнопка закрытия игры
        if (claimRewardsAndBackButton) claimRewardsAndBackButton.onClick.AddListener(OnClaimRewardsClicked); // Кнопка забрать награды
        if (hintCatButton) hintCatButton.onClick.AddListener(UseHint); // Кнопка подсказки Кота

        // Кнопки сложностей
        if (buttonEasy) buttonEasy.onClick.AddListener(() => StartGameWithDifficulty(0)); // Выбор легкой сложности
        if (buttonNormal) buttonNormal.onClick.AddListener(() => StartGameWithDifficulty(1)); // Выбор обычной сложности
        if (buttonHard) buttonHard.onClick.AddListener(() => StartGameWithDifficulty(2)); // Выбор сложной сложности

        // Кнопки рекордов
        if (catDialogContinueButton) catDialogContinueButton.onClick.AddListener(OpenRecordModeSelection); // Переход к выбору испытания рекордов
        if (recordTimeRushButton) recordTimeRushButton.onClick.AddListener(() => StartRecordMode(RecordModeType.TimeRush)); // Старт блиц-испытания
        if (recordItemSurgeButton) recordItemSurgeButton.onClick.AddListener(() => StartRecordMode(RecordModeType.BigCountSurge)); // Старт лавины предметов
        if (recordFlickerSpawnButton) recordFlickerSpawnButton.onClick.AddListener(() => StartRecordMode(RecordModeType.FlickerSpawn)); // Старт мерцания
        if (closeRecordPopupButton) closeRecordPopupButton.onClick.AddListener(() => { // Закрытие окна рекордов
            if (recordModeSelectPopup) recordModeSelectPopup.SetActive(false); // Скрытие окна рекордов
            if (locationSelectPopup) locationSelectPopup.SetActive(true); // Возврат к выбору локаций
        });

        // Карточки локаций
        for (int i = 0; i < locations.Count; i++) // Перебор списка всех локаций
        {
            int locIndex = i; // Сохранение индекса локации для замыкания
            if (locations[i].locationCardButton != null) // Если кнопка карточки локации задана
            {
                locations[i].locationCardButton.onClick.RemoveAllListeners(); // Очистка предыдущих слушателей
                locations[i].locationCardButton.onClick.AddListener(() => OpenLocationDifficultySelect(locIndex)); // Подписка на клик
            }
        }
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
        if (difficultySelectPopup) difficultySelectPopup.SetActive(true); // Показ окна выбора сложности
    }

    /// <summary>
    /// Старт локации с выбранной сложностью (0 - Легкий, 1 - Нормальный, 2 - Сложный)
    /// </summary>
    public void StartGameWithDifficulty(int difficultyIndex) // Запуск игры в локации с выбранной сложностью
    {
        if (currentLocation == null) return; // Проверка наличия локации
        if (difficultyIndex < 0 || difficultyIndex >= currentLocation.difficulties.Count) return; // Проверка индекса сложности

        currentDifficulty = currentLocation.difficulties[difficultyIndex]; // Установка параметров сложности

        if (difficultySelectPopup) difficultySelectPopup.SetActive(false); // Скрытие попапа сложности
        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(true); // Включение главного игрового экрана

        isRecordModeActive = false; // Отключение режима рекордов
        currentRoundIndex = 1; // Установка 1-го раунда
        availableHints = 3; // Выдача 3 подсказок

        StartRound(); // Запуск раунда
    }

    private void StartRound() // Старт очередного раунда поиска предметов
    {
        itemsFoundInCurrentRound = 0; // Сброс найденных предметов
        roundTimer = currentDifficulty.timeLimitPerRound; // Установка таймера раунда
        isGameRunning = true; // Активация игрового цикла

        if (backgroundLocationImage && currentLocation.backgroundSprite != null) // Если фон назначен
        {
            backgroundLocationImage.sprite = currentLocation.backgroundSprite; // Установка фона локации
        }

        if (locationTitleText) // Если заголовок задан
        {
            locationTitleText.text = $"{currentLocation.locationName} — {currentDifficulty.tierName}"; // Отображение имени локации и сложности
        }

        currentZoom = 1.0f; // Сброс масштаба к 100%
        panOffset = Vector2.zero; // Сброс смещения
        ApplyZoomAndPan(); // Применение трансформаций камеры

        UpdateUI(); // Обновление счетчиков и текстов
        SpawnTargetItemsForRound(); // Генерация предметов на поле и в панели целей
    }

    private void SpawnTargetItemsForRound() // Спавн иконок целей и кликабельных предметов на поле
    {
        ClearActiveItems(); // Очистка старых предметов

        if (targetIconsContainer == null || currentLocation.itemsPool.Count == 0) return; // Проверка пула предметов

        int totalToFind = currentDifficulty.itemsPerRound; // Количество предметов в раунде
        for (int i = 0; i < totalToFind; i++) // Цикл создания целей
        {
            Sprite itemSprite = currentLocation.itemsPool[i % currentLocation.itemsPool.Count]; // Получение спрайта предмета

            // Создание иконки цели в нижней панели
            GameObject slotUi = null; // Ссылка на UI-слот
            if (targetItemSlotPrefab != null) // Если префаб слота назначен
            {
                slotUi = Instantiate(targetItemSlotPrefab, targetIconsContainer); // Инстанцирование слота в панели целей
                Image img = slotUi.GetComponentInChildren<Image>(); // Поиск Image внутри слота
                if (img) img.sprite = itemSprite; // Назначение спрайта цели
            }

            // Спавн кликабельного предмета на фоне
            SpawnClickableItemOnBackground(itemSprite, slotUi); // Создание интерактивного предмета на сцене
        }
    }

    private void SpawnClickableItemOnBackground(Sprite itemSprite, GameObject slotUi) // Размещение кликабельного предмета на фоне локации
    {
        if (backgroundContentRoot == null) return; // Проверка корневого контейнера фона

        GameObject clickable = new GameObject("HiddenItem_" + itemSprite.name, typeof(RectTransform), typeof(Image), typeof(Button)); // Создание объекта предмета
        clickable.transform.SetParent(backgroundContentRoot, false); // Размещение внутри контейнера фона
        activeClickableItems.Add(clickable); // Добавление в список активных предметов

        RectTransform rt = clickable.GetComponent<RectTransform>(); // Получение RectTransform
        rt.sizeDelta = new Vector2(75, 75); // Установка размеров предмета 75x75

        // Случайные координаты в пределах фоновой сцены
        float posX = UnityEngine.Random.Range(-550f, 550f); // Случайная координата X
        float posY = UnityEngine.Random.Range(-320f, 320f); // Случайная координата Y
        rt.anchoredPosition = new Vector2(posX, posY); // Установка позиции предмета

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

        if (itemsFoundInCurrentRound >= currentDifficulty.itemsPerRound) // Если все предметы раунда найдены
        {
            if (currentRoundIndex < currentDifficulty.roundsRequired) // Если остались еще раунды
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

        if (currentDifficulty.tier == DifficultyTier.Easy) currentLocation.isEasyCompleted = true; // Отметка легкого уровня
        if (currentDifficulty.tier == DifficultyTier.Normal) currentLocation.isNormalCompleted = true; // Отметка нормального уровня
        if (currentDifficulty.tier == DifficultyTier.Hard) currentLocation.isHardCompleted = true; // Отметка сложного уровня

        SaveCompletionProgress(); // Сохранение прогресса на диск
        GrantRewards(); // Выдача наград в инвентарь и профиль

        if (victoryPopupPanel) // Если панель победы задана
        {
            victoryPopupPanel.SetActive(true); // Показ окна победы
            if (victoryTitleText) // Если заголовок победы есть
                victoryTitleText.text = $"Победа: {currentLocation.locationName} ({currentDifficulty.tierName})!"; // Текст победы

            if (victoryRewardsText) // Если текстовый блок наград назначен
            {
                string rewardsSummary = ""; // Формирование списка наград
                if (currentDifficulty.rewardStones > 0) rewardsSummary += $"💎 Камни: +{currentDifficulty.rewardStones}  "; // Камни
                if (currentDifficulty.rewardScrolls > 0) rewardsSummary += $"📜 Свитки: +{currentDifficulty.rewardScrolls}\n"; // Свитки
                if (currentDifficulty.rewardExpPotion100 > 0) rewardsSummary += $"🧪 Зелье Опыта (+100 XP): x{currentDifficulty.rewardExpPotion100}\n"; // Зелье опыта 100
                if (currentDifficulty.rewardExpPotion500 > 0) rewardsSummary += $"🧪 Зелье Опыта (+500 XP): x{currentDifficulty.rewardExpPotion500}\n"; // Зелье опыта 500
                if (currentDifficulty.rewardMasteryPotion100 > 0) rewardsSummary += $"✨ Зелье Мастерства (+100 XP): x{currentDifficulty.rewardMasteryPotion100}\n"; // Зелье мастерства 100
                if (currentDifficulty.rewardMasteryPotion500 > 0) rewardsSummary += $"✨ Зелье Мастерства (+500 XP): x{currentDifficulty.rewardMasteryPotion500}\n"; // Зелье мастерства 500

                victoryRewardsText.text = rewardsSummary; // Применение текста наград
            }
        }
    }

    private void GrantRewards() // Начисление камней, свитков, золота и зелий игроку
    {
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

    private void OnClaimRewardsClicked() // Обработка закрытия окна победы и проверка открытия эндгейм-режима
    {
        if (victoryPopupPanel) victoryPopupPanel.SetActive(false); // Скрытие окна победы
        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(false); // Скрытие игрового поля

        // Проверка: пройдены ли все 3 локации на всех сложностях?
        bool allCompleted = true; // Флаг полной зачистки
        foreach (var loc in locations) // Перебор локаций
        {
            if (!loc.IsFullyCompleted) { allCompleted = false; break; } // Если хоть одна не завершена
        }

        if (allCompleted) // Если зачищены все локации на всех сложностях
        {
            bool recordDialogShown = PlayerPrefs.GetInt("CatRecordDialogShown", 0) == 1; // Проверка показа диалога Кота
            if (!recordDialogShown) // Если еще не показывали
            {
                PlayerPrefs.SetInt("CatRecordDialogShown", 1); // Сохранение факта показа
                PlayerPrefs.Save(); // Запись на диск
                TriggerCatRecordUnlockDialog(); // Запуск диалога открытия рекордов
                return; // Выход
            }
        }

        if (locationSelectPopup) locationSelectPopup.SetActive(true); // Возврат к выбору локации
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

        for (int i = 0; i < count; i++) // Спавн нужного числа предметов
        {
            Sprite itemSprite = combinedPool[i % combinedPool.Count]; // Выбор спрайта
            GameObject slotUi = null; // Слот цели
            if (targetItemSlotPrefab != null) // Если префаб назначен
            {
                slotUi = Instantiate(targetItemSlotPrefab, targetIconsContainer); // Инстанцирование слота в панели целей
                Image img = slotUi.GetComponentInChildren<Image>(); // Компонент Image
                if (img) img.sprite = itemSprite; // Назначение спрайта цели
            }
            SpawnClickableItemOnBackground(itemSprite, slotUi); // Спавн на поле
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
            rt.sizeDelta = new Vector2(75, 75); // Размеры 75x75
            rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-550f, 550f), UnityEngine.Random.Range(-320f, 320f)); // Случайная позиция

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

            // Предмет исчезает через 1.2 секунды
            StartCoroutine(DisappearAfterTime(clickable, 1.2f)); // Запуск авто-исчезновения
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
            case RecordModeType.TimeRush: // Блиц-поиск
                crystalsReward = 1; // +1 Кристалл
                break; // Выход
            case RecordModeType.BigCountSurge: // Лавина предметов
                crystalsReward = UnityEngine.Random.Range(1, 6); // +1..5 Кристаллов
                break; // Выход
            case RecordModeType.FlickerSpawn: // Мерцание
                crystalsReward = UnityEngine.Random.Range(1, 11); // +1..10 Кристаллов
                break; // Выход
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
                victoryRewardsText.text = $"Вы одолели тяжелейшее испытание месяца!\n💎 Получено Кристаллов: +{crystalsReward}"; // Описание полученных кристаллов
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
            timerText.text = $"⏳ {min:00}:{sec:00}"; // Отображение времени раунда
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
        if (hintCountText) hintCountText.text = availableHints.ToString(); // Обновление счетчика на кнопке

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

    private void HandleTouchAndMouseZoomPan() // Обработка колесика мыши и мультитач-жестов для приближения сцены
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel"); // Колесико мыши
        if (Mathf.Abs(scroll) > 0.01f) // Если колесико крутится
        {
            currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed * 3f, minZoom, maxZoom); // Ограничение зума
            ApplyZoomAndPan(); // Применение зума
        }

        if (Input.touchCount == 2) // Если 2 пальца на экране (Pinch-to-zoom)
        {
            Touch touchZero = Input.GetTouch(0); // Первый палец
            Touch touchOne = Input.GetTouch(1); // Второй палец

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition; // Прошлая позиция пальца 1
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition; // Прошлая позиция пальца 2

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude; // Прошлое расстояние
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude; // Текущее расстояние

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag; // Разница расстояний
            currentZoom = Mathf.Clamp(currentZoom - deltaMagnitudeDiff * 0.005f, minZoom, maxZoom); // Корректировка зума
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
            hintCountText.text = $"💡 Подсказка: {availableHints}"; // Количество подсказок
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

    public void CloseGame() // Закрытие игрового окна и возврат к выбору локаций
    {
        isGameRunning = false; // Остановка игрового процесса
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine); // Остановка корутины мерцания
        ClearActiveItems(); // Удаление активных предметов

        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(false); // Скрытие игрового экрана
        if (difficultySelectPopup) difficultySelectPopup.SetActive(false); // Скрытие окна сложностей
        if (recordModeSelectPopup) recordModeSelectPopup.SetActive(false); // Скрытие окна рекордов
        if (locationSelectPopup) locationSelectPopup.SetActive(true); // Включение окна выбора локации
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

    public void InitializeDefaultConfigurationsIfEmpty() // Инициализация 3 локаций по умолчанию, если список в инспекторе пуст
    {
        if (locations.Count == 0) // Если локации еще не созданы
        {
            // 1. Локация: Лавка Алхимика
            LocationConfig shop = new LocationConfig { locationId = "Shop", locationName = "Лавка Алхимика" }; // Создание лавки
            shop.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Easy, tierName = "Легкий", itemsPerRound = 5, roundsRequired = 3, timeLimitPerRound = 90f, rewardStones = 3, rewardScrolls = 1 }); // Легкая сложность
            shop.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Normal, tierName = "Нормальный", itemsPerRound = 10, roundsRequired = 3, timeLimitPerRound = 90f, rewardStones = 6, rewardScrolls = 3, rewardExpPotion100 = 1 }); // Нормальная сложность
            shop.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Hard, tierName = "Сложный", itemsPerRound = 10, roundsRequired = 5, timeLimitPerRound = 90f, rewardStones = 15, rewardScrolls = 10, rewardExpPotion500 = 1, rewardMasteryPotion100 = 1 }); // Сложная сложность
            locations.Add(shop); // Добавление лавки

            // 2. Локация: Дом Алхимика
            LocationConfig house = new LocationConfig { locationId = "House", locationName = "Дом Алхимика" }; // Создание дома
            house.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Easy, tierName = "Легкий", itemsPerRound = 6, roundsRequired = 3, timeLimitPerRound = 90f, rewardStones = 5, rewardScrolls = 1 }); // Легкая сложность
            house.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Normal, tierName = "Нормальный", itemsPerRound = 12, roundsRequired = 3, timeLimitPerRound = 90f, rewardStones = 9, rewardScrolls = 3, rewardExpPotion100 = 2 }); // Нормальная сложность
            house.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Hard, tierName = "Сложный", itemsPerRound = 15, roundsRequired = 5, timeLimitPerRound = 90f, rewardStones = 20, rewardScrolls = 12, rewardExpPotion500 = 2, rewardMasteryPotion100 = 1 }); // Сложная сложность
            locations.Add(house); // Добавление дома

            // 3. Локация: Магический Рынок
            LocationConfig market = new LocationConfig { locationId = "Market", locationName = "Магический Рынок" }; // Создание рынка
            market.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Easy, tierName = "Легкий", itemsPerRound = 6, roundsRequired = 3, timeLimitPerRound = 90f, rewardExpPotion100 = 3 }); // Легкая сложность
            market.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Normal, tierName = "Нормальный", itemsPerRound = 12, roundsRequired = 3, timeLimitPerRound = 90f, rewardExpPotion100 = 6, rewardMasteryPotion100 = 3 }); // Нормальная сложность
            market.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Hard, tierName = "Сложный", itemsPerRound = 15, roundsRequired = 5, timeLimitPerRound = 90f, rewardExpPotion500 = 3, rewardMasteryPotion500 = 1 }); // Сложная сложность
            locations.Add(market); // Добавление рынка
        }
    }
}
