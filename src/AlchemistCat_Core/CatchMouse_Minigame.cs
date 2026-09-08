using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Мини-игра «Поймай мышку» (Catch The Mouse) для Колеса Мини-Игр Кота-Алхимика.
/// Реализует 3-фазную сюжетно-прогрессивную механику ловли мышей с выбором 3 уровней сложности:
/// - Меню сложности перед стартом: Легкий (x1.0), Средний (x1.5), Сложный (x2.5) с множителем всех наград.
/// - Фаза 1: Поймать 5 золотых мышей. Клик на черную или серебристую мышь сбрасывает счетчик/перезапускает фазу.
/// - Фаза 2: Поймать 10 серебряных мышей. Черных и золотых не трогать.
/// - Фаза 3: Поймать 20 черных (самых быстрых) мышей!
/// - Поддержка покадровой спрайтовой анимации (AutoSprite.io / Sorceress).
/// </summary>
public class CatchMouse_Minigame : MonoBehaviour
{
    public static CatchMouse_Minigame Instance; // Статический синглтон мини-игры "Поймай мышь"

    public enum DifficultyLevel { Easy, Normal, Hard } // Уровни сложности (Легкий, Обычный, Сложный)

    [Header("Главная панель мини-игры")]
    public GameObject gamePanel; // CatchMouse_Game_Panel: Главная панель игры
    public Button closeButton; // Close_Button: Кнопка выхода из мини-игры

    [Header("Меню выбора сложности (Перед игрой)")]
    public GameObject difficultySelectionPanel; // Difficulty_Selection_Panel: Панель выбора сложности
    public Button easyDifficultyButton; // Easy_Button: Кнопка легкой сложности
    public Button normalDifficultyButton; // Normal_Button: Кнопка обычной сложности
    public Button hardDifficultyButton; // Hard_Button: Кнопка сложной сложности

    [Header("Верхняя плашка цели (Задание)")]
    public Image targetMouseDisplayImage; // Target_Mouse_Image: Иконка целевой мыши
    public TextMeshProUGUI targetTitleText; // Target_Title_Text: Текст текущей цели
    public TextMeshProUGUI instructionBannerText; // Текст инструкции/предупреждения
    public TextMeshProUGUI progressCounterText; // Progress_Counter_Text: Текст счетчика пойманных мышей
    public TextMeshProUGUI timerText; // Timer_Text: Текст обратного отсчета времени
    public float roundTime = 15f; // Время на раунд/фазу в секундах

    [Header("Центральное уведомление перехода фаз")]
    public GameObject centralPhaseNoticePanel; // Central_Phase_Notice_Panel: Баннер смены фазы
    public TextMeshProUGUI centralNoticeTitle; // Notice_Title: Заголовок уведомления
    public TextMeshProUGUI centralNoticeBody; // Notice_Body: Текст уведомления

    [Header("Одиночные Спрайты Мышек (Статика/Фоллбэк)")]
    public Sprite goldenMouseSprite; // Спрайт Золотой Мышки
    public Sprite silverMouseSprite; // Спрайт Серебряной Мышки
    public Sprite blackMouseSprite; // Спрайт Черной Теневой Мышки

    [Header("Кадры бега Мышек (Spritesheet Frames)")]
    public Sprite[] goldenMouseFrames; // Golden Mouse Frames (18 кадров)
    public Sprite[] silverMouseFrames; // Silver Mouse Frames (17 кадров)
    public Sprite[] shadowMouseFrames; // Shadow Mouse Frames (14 кадров)
    public float animationFps = 12f; // Частота смены кадров анимации бега

    [Header("Спрайт Норки и Дорожки")]
    public Sprite holeSprite; // Спрайт норки-арки
    public Sprite roadSprite; // Спрайт беговой дорожки

    [Header("5 Норок и Дорожка")]
    public RectTransform[] holes; // Массив трансформов 5 норок (Hole_1 .. Hole_5)
    public RectTransform roadTrack; // Road_Track: Трансформ дорожки
    public RectTransform miceRunningLayer; // Mice_Running_Layer: Слой бега мышей

    [Header("3D-Перспектива и Настройка Дорожки (Road_Track)")]
    public bool autoApplyRoadPerspective = true; // Автоматическое применение угла наклона
    public float roadPerspectiveTiltAngle = -126.083f; // Точный угол наклона дорожки
    public float roadYOffset = -18f; // Смещение дорожки по высоте Y
    public float roadWidth = 960f; // Базовая ширина дорожки под все разрешения
    public float roadHeight = 120f; // Высота дорожки
    public float mouseRunYOffset = 0f; // Подстройка высоты бега мышек

    [Header("Окно Победы и Награды")]
    public GameObject rewardPopupPanel; // Reward_Popup_Panel: Окно победы
    public Image potionRewardIcon; // Potion_Icon: Иконка зелья в награде
    public TextMeshProUGUI rewardDescriptionText; // Reward_Description_Text: Описание награды
    public Button claimRewardButton; // Claim_Reward_Button: Кнопка забрать награду

    [Header("Звуки")]
    public AudioClip winFanfareSound; // Звук победы и фанфар
    public AudioClip phaseCompleteSound; // Звук успешного прохождения фазы
    public AudioClip wrongMouseSound; // Звук ловли неправильной мыши (ошибка)
    public AudioClip mouseRunSound; // Звук шороха бега мышей
    public AudioClip catchMouseSound; // Звук успешного клика/ловли мыши
    public AudioClip clickSound; // Звук нажатия на кнопки

    // Внутреннее состояние игры
    public enum MouseType { Golden, Silver, Black } // Типы мышей

    private DifficultyLevel selectedDifficulty = DifficultyLevel.Normal; // Выбранная сложность
    private int currentPhase = 1; // Номер текущей фазы (1, 2, 3)
    private int miceCaughtInPhase = 0; // Поймано мышей в текущей фазе
    private int targetMiceForCurrentPhase = 5; // Целевое количество мышей
    private MouseType currentTargetType = MouseType.Golden; // Текущий тип целевой мыши

    private float currentTimer; // Текущий таймер раунда
    private bool isGameActive = false; // Флаг: активна ли игра
    private bool isGameWon = false; // Флаг: выиграна ли игра
    private bool isPhaseTransitioning = false; // Флаг: идет ли анимация перехода между фазами

    // Пул активных бегущих мышек
    private List<GameObject> activeMice = new List<GameObject>(); // Список мышей на экране
    private Coroutine spawnCoroutine; // Ссылка на корутину спавна мышей

    private void Awake() // Инициализация экземпляра синглтона при пробуждении объекта
    {
        Instance = this; // Инициализация синглтона при старте
    }

    private void Start() // Стартовая инициализация обработчиков кнопок и панелей
    {
        SetupRoadPerspective(); // Применение 3D-наклона дорожки

        if (closeButton != null) // Если кнопка закрытия назначена
            closeButton.onClick.AddListener(HandleCloseButtonClicked); // Подписка на клик закрытия

        if (claimRewardButton != null) // Если кнопка забрать награду назначена
            claimRewardButton.onClick.AddListener(ClaimRewardAndExit); // Подписка на получение награды

        if (easyDifficultyButton != null) // Если кнопка легкой сложности назначена
            easyDifficultyButton.onClick.AddListener(() => SetDifficultyAndStart(DifficultyLevel.Easy)); // Запуск легкой сложности

        if (normalDifficultyButton != null) // Если кнопка обычной сложности назначена
            normalDifficultyButton.onClick.AddListener(() => SetDifficultyAndStart(DifficultyLevel.Normal)); // Запуск обычной сложности

        if (hardDifficultyButton != null) // Если кнопка сложной сложности назначена
            hardDifficultyButton.onClick.AddListener(() => SetDifficultyAndStart(DifficultyLevel.Hard)); // Запуск сложной сложности

        if (rewardPopupPanel != null) // Если окно наград назначено
            rewardPopupPanel.SetActive(false); // Скрытие окна наград на старте

        if (centralPhaseNoticePanel != null) // Если баннер смены фазы назначен
            centralPhaseNoticePanel.SetActive(false); // Скрытие баннера смены фазы на старте
    }

    private void OnEnable() // Обработка включения панели мини-игры
    {
        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов доступен
        {
            DialogueSystem_Manager.Instance.HideHUDForMinigame(); // Скрытие верхнего HUD (аватарка и кнопки)
        }
        SetupRoadPerspective(); // Обновление геометрии дорожки
        ShowDifficultySelection(); // Показ меню выбора сложности
    }

    private void OnDisable() // Обработка отключения панели мини-игры
    {
        StopAllCoroutines(); // Остановка всех запущенных корутин
        ClearAllMice(); // Очистка всех активных мышей с экрана
        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов доступен
        {
            DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление верхнего HUD
        }
    }

    /// <summary>
    /// Автоматическая настройка дорожки (растяжение текстуры, сброс PreserveAspect, юстировка высоты и норок)
    /// </summary>
    public void SetupRoadPerspective() // Юстировка дорожки и норок в псевдо-3D пространстве
    {
        if (!autoApplyRoadPerspective) return; // Пропуск если автонастройка отключена

        // 1. Настройка дорожки Road_Track
        if (roadTrack != null) // Если трансформ дорожки задан
        {
            // Отключаем PreserveAspect на компоненте Image, чтобы текстура заполнила всю длину 960px!
            Image roadImg = roadTrack.GetComponent<Image>(); // Получение компонента Image дорожки
            if (roadImg != null) // Если компонент найден
            {
                roadImg.preserveAspect = false; // Отключение сохранения пропорций
                roadImg.type = Image.Type.Simple; // Простой тип отображения
            }

            roadTrack.anchorMin = new Vector2(0.5f, 0.5f); // Центровка минимального якоря
            roadTrack.anchorMax = new Vector2(0.5f, 0.5f); // Центровка максимального якоря
            roadTrack.pivot = new Vector2(0.5f, 0.5f); // Центровка точки вращения
            roadTrack.sizeDelta = new Vector2(roadWidth, roadHeight); // Установка размеров полотна дорожки
            roadTrack.anchoredPosition3D = new Vector3(0f, roadYOffset, 24.77593f); // Установка 3D-позиции дорожки
            roadTrack.localEulerAngles = new Vector3(roadPerspectiveTiltAngle, 0f, 0f); // Наклон по оси X для перспективы
        }

        // 2. Проверка контейнера норок Holes_Container (поднять по Y 60, ширина 1000, высота 150)
        if (holes != null && holes.Length > 0 && holes[0] != null && holes[0].parent != null) // Проверка массива норок
        {
            RectTransform holesContainer = holes[0].parent.GetComponent<RectTransform>(); // Родительский контейнер норок
            if (holesContainer != null && holesContainer.name.Contains("Holes_Container")) // Если это Holes_Container
            {
                holesContainer.anchorMin = new Vector2(0.5f, 0.5f); // Центровка якоря
                holesContainer.anchorMax = new Vector2(0.5f, 0.5f); // Центровка якоря
                holesContainer.pivot = new Vector2(0.5f, 0.5f); // Центровка пивота
                holesContainer.anchoredPosition = new Vector2(0f, 60f); // Смещение по высоте над дорожкой
                holesContainer.sizeDelta = new Vector2(1000f, 150f); // Габариты контейнера норок
            }
        }
    }

    public void HandleCloseButtonClicked() // Обработка нажатия кнопки Закрыть/Назад
    {
        if (clickSound != null && SettingsManager.Instance != null) // Если звук клика задан
            SettingsManager.Instance.PlaySoundEffect(clickSound); // Проигрывание звука нажатия

        // Если игрок прямо сейчас играет, не может пройти или открыто окно победы — возвращаем на выбор сложности!
        if (isGameActive || isGameWon || (rewardPopupPanel != null && rewardPopupPanel.activeSelf) || (difficultySelectionPanel != null && !difficultySelectionPanel.activeSelf)) // Условие возврата в меню сложности
        {
            ShowDifficultySelection(); // Возврат на экран выбора сложности
        }
        else // Если уже на экране выбора сложности
        {
            // Если уже на экране выбора сложности — закрываем окно мини-игры полностью
            CloseMinigame(); // Полное закрытие мини-игры
        }
    }

    /// <summary>
    /// Показ экрана выбора сложности перед стартом забега
    /// </summary>
    public void ShowDifficultySelection() // Сброс состояния и отображение меню сложности
    {
        isGameActive = false; // Сброс флага активности игры
        isGameWon = false; // Сброс флага победы
        isPhaseTransitioning = false; // Сброс флага анимации перехода
        ClearAllMice(); // Очистка бегающих мышек

        if (rewardPopupPanel != null) // Если окно наград есть
            rewardPopupPanel.SetActive(false); // Скрытие окна наград

        if (centralPhaseNoticePanel != null) // Если баннер уведомления есть
            centralPhaseNoticePanel.SetActive(false); // Скрытие баннера смены фазы

        if (difficultySelectionPanel != null) // Если панель сложности назначена
            difficultySelectionPanel.SetActive(true); // Включение панели выбора сложности
    }

    /// <summary>
    /// Выбор сложности и запуск 1-й фазы (5 Золотых мышей)
    /// </summary>
    public void SetDifficultyAndStart(DifficultyLevel diff) // Фиксация выбранной сложности и запуск игры
    {
        selectedDifficulty = diff; // Сохранение уровня сложности

        if (clickSound != null && SettingsManager.Instance != null) // Если звук задан
            SettingsManager.Instance.PlaySoundEffect(clickSound); // Воспроизведение звука клика

        if (difficultySelectionPanel != null) // Если панель сложности есть
            difficultySelectionPanel.SetActive(false); // Скрытие меню сложности

        // Старт с Фазы 1
        currentPhase = 1; // Установка 1-й фазы
        StartPhase(1); // Запуск первого этапа
    }

    /// <summary>
    /// Старт конкретной фазы мини-игры (1 = Золото 5 шт, 2 = Серебро 10 шт, 3 = Черные 20 шт)
    /// </summary>
    private void StartPhase(int phase) // Настройка параметров и запуск конкретной фазы
    {
        currentPhase = phase; // Сохранение номера текущей фазы
        miceCaughtInPhase = 0; // Сброс счетчика пойманных мышей
        isPhaseTransitioning = false; // Сброс флага перехода
        isGameWon = false; // Сброс флага победы
        isGameActive = true; // Активация игрового процесса

        switch (currentPhase) // Конфигурация условий фазы
        {
            case 1: // Первая фаза
                currentTargetType = MouseType.Golden; // Цель: Золотые мыши
                targetMiceForCurrentPhase = 5; // Нужно поймать: 5 шт
                currentTimer = roundTime > 0 ? roundTime : 45f; // Установка таймера
                break; // Выход из ветки 1
            case 2: // Вторая фаза
                currentTargetType = MouseType.Silver; // Цель: Серебряные мыши
                targetMiceForCurrentPhase = 10; // Нужно поймать: 10 шт
                currentTimer = (roundTime > 0 ? roundTime : 45f) + 15f; // Больше времени на 10 мышей
                break; // Выход из ветки 2
            case 3: // Третья фаза
                currentTargetType = MouseType.Black; // Цель: Черные теневые мыши
                targetMiceForCurrentPhase = 20; // Нужно поймать: 20 шт
                currentTimer = (roundTime > 0 ? roundTime : 45f) + 35f; // Время на 20 черных быстрых мышей
                break; // Выход из ветки 3
        }

        UpdateTaskUI(); // Обновление текста задач и счетчиков в UI
        ClearAllMice(); // Очистка мышек на экране

        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine); // Остановка старого цикла спавна
        spawnCoroutine = StartCoroutine(MiceSpawnLoop()); // Запуск цикла генерации мышек
    }

    /// <summary>
    /// Обновление текстов рамок и счетчиков в UI
    /// </summary>
    private void UpdateTaskUI() // Синхронизация текстов и иконок текущей цели с интерфейсом
    {
        if (targetMouseDisplayImage != null) // Если иконка цели назначена
        {
            targetMouseDisplayImage.sprite = GetMouseSprite(currentTargetType); // Установка спрайта целевой мыши
            targetMouseDisplayImage.preserveAspect = true; // Сохранение пропорций иконки
        }

        if (targetTitleText != null) // Если заголовок цели задан
        {
            string colorName = currentTargetType == MouseType.Golden ? "<color=#FFD166>ЗОЛОТЫХ</color>" : // Название золотых мышей
                              (currentTargetType == MouseType.Silver ? "<color=#E0E1DD>СЕРЕБРЯНЫХ</color>" : "<color=#A06CD5>ТЕНЕВЫХ ЧЕРНЫХ</color>"); // Название серебряных или черных
            targetTitleText.text = $"ЭТАП {currentPhase}/3: Поймать {targetMiceForCurrentPhase} {colorName} мышей!"; // Форматирование строки цели
        }

        if (instructionBannerText != null) // Если текстовый баннер инструкции задан
        {
            if (currentPhase == 1) // Инструкция для 1-й фазы
            {
                instructionBannerText.text = "<color=#EF476F>⚠ ВНИМАНИЕ:</color> Черных и серебристых не трогать, иначе игра начнется заново!"; // Предупреждение 1 фазы
            }
            else if (currentPhase == 2) // Инструкция для 2-й фазы
            {
                instructionBannerText.text = "<color=#EF476F>⚠ ВНИМАНИЕ:</color> Черных и золотых не трогать, иначе этап сбросится!"; // Предупреждение 2 фазы
            }
            else // Инструкция для 3-й фазы
            {
                instructionBannerText.text = "<color=#FFD166>⚡ ФИНАЛ:</color> Ловите 20 самых быстрых теневых мышей! Золото и серебро не трогать!"; // Предупреждение 3 фазы
            }
        }

        if (progressCounterText != null) // Если текст счетчика пойманных мышей задан
        {
            progressCounterText.text = $"Поймано: <b>{miceCaughtInPhase} / {targetMiceForCurrentPhase}</b>"; // Отображение текущего прогресса
        }
    }

    private void Update() // Покадровый отсчет таймера и обновление UI времени
    {
        if (!isGameActive || isGameWon || isPhaseTransitioning) return; // Пропуск если игра не в активной фазе

        currentTimer -= Time.deltaTime; // Уменьшение времени раунда
        if (currentTimer <= 0f) // Если время вышло
        {
            currentTimer = 0f; // Фиксация нуля
            // Время истекло — сброс текущей фазы
            RestartCurrentPhase("Время вышло! Попробуйте еще раз!"); // Перезапуск раунда с уведомлением
        }

        if (timerText != null) // Если компонент таймера назначен
        {
            int seconds = Mathf.CeilToInt(currentTimer); // Округление секунд вверх
            timerText.text = $"Время: 00:{seconds:D2}"; // Форматирование вывода времени
        }
    }

    /// <summary>
    /// Цикл выбегания мышек из норок
    /// </summary>
    private IEnumerator MiceSpawnLoop() // Корутина бесконечного спавна мышек пока идет фаза
    {
        while (isGameActive && !isGameWon && !isPhaseTransitioning) // Пока идет активная игра
        {
            float spawnDelay = GetSpawnInterval(); // Расчет задержки перед следующим спавном
            yield return new WaitForSeconds(spawnDelay); // Ожидание интервала спавна

            if (holes == null || holes.Length < 2) continue; // Проверка наличия норок для перебежки

            int startHoleIndex = Random.Range(0, holes.Length); // Случайная стартовая норка
            int endHoleIndex = Random.Range(0, holes.Length); // Случайная целевая норка
            while (endHoleIndex == startHoleIndex) // Защита от совпадения норок
            {
                endHoleIndex = Random.Range(0, holes.Length); // Повторный выбор другой норки
            }

            // Вероятность появления мышки целевого типа ~45%
            MouseType spawnType; // Тип появляющейся мышки
            if (Random.value < 0.45f) // 45% шанс целевого типа
            {
                spawnType = currentTargetType; // Спавним нужную мышь
            }
            else // 55% шанс случайной мыши
            {
                int r = Random.Range(0, 3); // Случайный индекс от 0 до 2
                spawnType = (MouseType)r; // Приведение к типу мыши
            }

            SpawnMouseRunner(spawnType, startHoleIndex, endHoleIndex); // Создание бегуна
        }
    }

    private float GetSpawnInterval() // Расчет интервала спавна в зависимости от сложности и фазы
    {
        float baseDelay = 1.2f; // Базовая задержка между спавном

        switch (selectedDifficulty) // Ветвление по уровням сложности
        {
            case DifficultyLevel.Easy: // Легкий уровень сложности
                baseDelay = 1.8f; // Базовая задержка для легкого режима
                if (currentPhase == 2) baseDelay = 1.5f; // Задержка для 2 фазы
                if (currentPhase == 3) baseDelay = 1.3f; // Задержка для 3 фазы
                break; // Выход из легкого режима

            case DifficultyLevel.Normal: // Обычный уровень сложности
                baseDelay = 1.2f; // Базовая задержка обычного режима
                if (currentPhase == 2) baseDelay = 1.0f; // Задержка для 2 фазы
                if (currentPhase == 3) baseDelay = 0.85f; // Задержка для 3 фазы
                break; // Выход из обычного режима

            case DifficultyLevel.Hard: // Сложный уровень сложности
                baseDelay = 0.9f; // Базовая задержка сложного режима
                if (currentPhase == 2) baseDelay = 0.75f; // Задержка для 2 фазы
                if (currentPhase == 3) baseDelay = 0.55f; // Быстрый спавн для 3 фазы
                break; // Выход из сложного режима
        }

        return Random.Range(baseDelay * 0.85f, baseDelay * 1.15f); // Добавление случайного разброса ±15%
    }

    /// <summary>
    /// Создание бегущей мышки с реалистичной 3D-траекторией:
    /// 1. Выбегание из глубины норки (масштаб 0.55 -> 1.0)
    /// 2. Бег по дорожке с пружинящим шагом
    /// 3. Забегание в целевую норку (масштаб 1.0 -> 0.55) и растворение
    /// </summary>
    private void SpawnMouseRunner(MouseType type, int startIdx, int endIdx) // Создание UI-объекта мышки и запуск траектории
    {
        if (miceRunningLayer == null || holes == null || holes.Length == 0) return; // Проверка слоя бега и норок

        GameObject mouseObj = new GameObject($"Mouse_{type}"); // Создание нового игрового объекта мыши
        mouseObj.transform.SetParent(miceRunningLayer, false); // Размещение на слое бега мышей

        RectTransform rt = mouseObj.AddComponent<RectTransform>(); // Добавление RectTransform
        rt.sizeDelta = new Vector2(110f, 70f); // Установка размеров хитбокса мышки

        Image img = mouseObj.AddComponent<Image>(); // Добавление компонента Image
        img.sprite = GetMouseSprite(type); // Назначение спрайта мышки
        img.preserveAspect = true; // Сохранение пропорций тела

        CanvasGroup cg = mouseObj.AddComponent<CanvasGroup>(); // Добавление CanvasGroup для прозрачности

        Button btn = mouseObj.AddComponent<Button>(); // Добавление компонента Button для кликов
        btn.transition = Selectable.Transition.None; // Отключение визуальных эффектов кнопки

        // Вычисляем точные координаты норки относительно слоя бега мышек (Mice_Running_Layer)
        Vector2 startHolePos; // Локальная позиция стартовой норки
        Vector2 endHolePos; // Локальная позиция целевой норки

        if (startIdx < holes.Length && holes[startIdx] != null) // Если стартовая норка доступна
        {
            startHolePos = miceRunningLayer.InverseTransformPoint(holes[startIdx].position); // Преобразование координат
        }
        else // Фоллбэк координата
        {
            startHolePos = new Vector2(-250f + (startIdx * 125f), 30f); // Расчетное положение
        }

        if (endIdx < holes.Length && holes[endIdx] != null) // Если конечная норка доступна
        {
            endHolePos = miceRunningLayer.InverseTransformPoint(holes[endIdx].position); // Преобразование координат
        }
        else // Фоллбэк координата
        {
            endHolePos = new Vector2(-250f + (endIdx * 125f), 30f); // Расчетное положение
        }

        // Вычисляем высоту дорожки относительно слоя бега (верхняя каменная кромка дорожки)
        float roadY; // Позиция Y дорожки
        if (roadTrack != null) // Если дорожка назначена
        {
            Vector2 roadInMice = miceRunningLayer.InverseTransformPoint(roadTrack.position); // Координата дорожки на слое
            roadY = roadInMice.y + 12f + mouseRunYOffset; // Точная высота линии бега
        }
        else // Фоллбэк высоты
        {
            roadY = startHolePos.y - 48f + mouseRunYOffset; // Смещение относительно норок
        }

        bool runRight = endHolePos.x > startHolePos.x; // Направление бега вправо
        float baseScaleX = runRight ? 1f : -1f; // Отражение спрайта по горизонтали

        rt.anchoredPosition = startHolePos; // Начальная позиция мышки
        rt.localScale = new Vector3(baseScaleX * 0.55f, 0.55f, 1f); // Начальный уменьшенный масштаб из норки

        btn.onClick.AddListener(() => OnMouseClicked(mouseObj, type)); // Подписка на клик по мышке

        activeMice.Add(mouseObj); // Добавление мыши в активный список

        // Запуск покадровой анимации если загружены спрайт-массивы
        Sprite[] animFrames = GetAnimationFrames(type); // Получение кадров анимации бега
        if (animFrames != null && animFrames.Length > 1) // Если кадры анимации доступны
        {
            StartCoroutine(AnimateSpriteFrames(mouseObj, img, animFrames)); // Запуск аниматора кадров
        }

        // Длительность перебежки (Черные мыши бегают быстрее всех!)
        float runDuration = GetRunDuration(type); // Расчет скорости перебежки
        StartCoroutine(AnimateMouse3DTrajectory(mouseObj, rt, cg, startHolePos, endHolePos, roadY, baseScaleX, runDuration)); // Запуск перемещения
    }

    private float GetRunDuration(MouseType type) // Расчет длительности перебежки мышки в зависимости от типа и сложности
    {
        // Длительность перебежки мышки (в секундах). Чем больше длительность — тем медленнее и плавнее бежит мышка!
        float baseDuration = 3.2f; // Базовая длительность бега

        switch (selectedDifficulty) // Настройка скорости по сложности
        {
            case DifficultyLevel.Easy: // Легкий уровень: Мышки бегут очень медленно и вальяжно (4.0 - 5.5 сек)
                if (type == MouseType.Golden) baseDuration = 5.2f; // Золотая мышь в легком режиме
                else if (type == MouseType.Silver) baseDuration = 4.6f; // Серебряная мышь в легком режиме
                else if (type == MouseType.Black) baseDuration = 3.8f; // Черная мышь в легком режиме
                break; // Выход из легкого режима

            case DifficultyLevel.Normal: // Средний уровень: Комфортный ритм (2.5 - 3.5 сек)
                if (type == MouseType.Golden) baseDuration = 3.4f; // Золотая мышь в обычном режиме
                else if (type == MouseType.Silver) baseDuration = 2.9f; // Серебряная мышь в обычном режиме
                else if (type == MouseType.Black) baseDuration = 2.3f; // Черная мышь в обычном режиме
                break; // Выход из обычного режима

            case DifficultyLevel.Hard: // Сложный уровень: Быстрые и вертлявые мышки (1.0 - 2.0 сек)
                if (type == MouseType.Golden) baseDuration = 2.0f; // Золотая мышь в сложном режиме
                else if (type == MouseType.Silver) baseDuration = 1.6f; // Серебряная мышь в сложном режиме
                else if (type == MouseType.Black) baseDuration = 1.15f; // Черная мышь в сложном режиме
                break; // Выход из сложного режима
        }

        return Random.Range(baseDuration * 0.9f, baseDuration * 1.1f); // Небольшой случайный разброс скорости
    }

    private IEnumerator AnimateSpriteFrames(GameObject mouseObj, Image img, Sprite[] frames) // Корутина покадровой смены спрайтов бега
    {
        int frameIndex = 0; // Индекс текущего кадра
        float frameTime = 1f / Mathf.Max(1f, animationFps); // Интервал показа кадра

        while (mouseObj != null && img != null) // Пока объект мыши жив
        {
            img.sprite = frames[frameIndex]; // Смена спрайта на текущий кадр
            frameIndex = (frameIndex + 1) % frames.Length; // Циклический инкремент индекса
            yield return new WaitForSeconds(frameTime); // Ожидание до следующего кадра
        }
    }

    /// <summary>
    /// 3D-анимация появления из арки норки, движения по дорожке и забегания в целевую норку
    /// </summary>
    private IEnumerator AnimateMouse3DTrajectory(GameObject mouseObj, RectTransform rt, CanvasGroup cg, Vector2 startHole, Vector2 endHole, float roadY, float baseScaleX, float duration) // Корутина 3D-траектории бега мыши
    {
        float elapsed = 0f; // Таймер перемещения

        while (elapsed < duration && mouseObj != null) // Цикл перемещения по времени
        {
            elapsed += Time.deltaTime; // Накопление времени
            float t = Mathf.Clamp01(elapsed / duration); // Нормализация времени от 0 до 1

            Vector2 currentPos; // Вычисленная позиция
            float currentScale; // Вычисленный масштаб
            float currentAlpha; // Вычисленная прозрачность

            if (t < 0.22f) // Фаза 1: Выход из норки
            {
                // ЭТАП 1: Выход из норки на дорожку (по диагонали вниз, приближение к камере)
                float exitT = t / 0.22f; // Нормализация времени выхода
                float easeOut = Mathf.Sin(exitT * Mathf.PI * 0.5f); // Плавное замедление

                currentPos = Vector2.Lerp(startHole, new Vector2(startHole.x, roadY), easeOut); // Спуск на дорожку
                currentScale = Mathf.Lerp(0.55f, 1.0f, easeOut); // Увеличение масштаба до 100%
                currentAlpha = Mathf.Lerp(0.3f, 1.0f, easeOut); // Плавное проявление
            }
            else if (t < 0.78f) // Фаза 2: Бег по дорожке
            {
                // ЭТАП 2: Основной бег по дорожке слева направо / справа налево по верхней каменной кромке
                float roadT = (t - 0.22f) / (0.78f - 0.22f); // Прогресс бега по дорожке
                float jumpOffset = Mathf.Abs(Mathf.Sin(roadT * Mathf.PI * 8f)) * 4f; // Плавный естественный пружинящий шаг

                currentPos = new Vector2(Mathf.Lerp(startHole.x, endHole.x, roadT), roadY + jumpOffset); // Перемещение по X с подпрыгиванием
                currentScale = 1.0f; // Полный масштаб
                currentAlpha = 1.0f; // Полная видимость
            }
            else // Фаза 3: Забегание в норку
            {
                // ЭТАП 3: Забегание в целевую норку (вверх, отдаление вглубь 3D и исчезновение)
                float enterT = (t - 0.78f) / (1.0f - 0.78f); // Прогресс входа в норку
                float easeIn = enterT * enterT; // Ускорение при входе

                currentPos = Vector2.Lerp(new Vector2(endHole.x, roadY), endHole, easeIn); // Подъем в норку
                currentScale = Mathf.Lerp(1.0f, 0.55f, easeIn); // Уменьшение масштаба вглубь
                currentAlpha = Mathf.Lerp(1.0f, 0.0f, easeIn); // Растворение в темноте норки
            }

            if (rt != null) // Если RectTransform доступен
            {
                rt.anchoredPosition = currentPos; // Обновление позиции мышки
                rt.localScale = new Vector3(baseScaleX * currentScale, currentScale, 1f); // Обновление масштаба
            }

            if (cg != null) // Если CanvasGroup доступен
            {
                cg.alpha = currentAlpha; // Обновление прозрачности
            }

            yield return null; // Ожидание следующего кадра
        }

        if (mouseObj != null) // Если мышка завершила путь и не была поймана
        {
            activeMice.Remove(mouseObj); // Удаление из списка активных
            Destroy(mouseObj); // Уничтожение игрового объекта мыши
        }
    }

    /// <summary>
    /// Обработка клика по мышке с проверкой правильности цели
    /// </summary>
    public void OnMouseClicked(GameObject mouseObj, MouseType type) // Обработка попытки поймать мышь кликом
    {
        if (!isGameActive || isGameWon || isPhaseTransitioning) return; // Игнорирование кликов при неактивной игре

        if (type == currentTargetType) // Если поймана нужная мышь
        {
            // ПРАВИЛЬНАЯ МЫШКА!
            miceCaughtInPhase++; // Увеличение счетчика пойманных мышей
            if (catchMouseSound != null && SettingsManager.Instance != null) // Если звук поимки задан
                SettingsManager.Instance.PlaySoundEffect(catchMouseSound); // Воспроизведение звука ловли

            // Удаляем пойманную мышку
            if (mouseObj != null) // Если объект мыши существует
            {
                activeMice.Remove(mouseObj); // Удаление из списка активных
                Destroy(mouseObj); // Уничтожение пойманной мышки
            }

            UpdateTaskUI(); // Обновление счетчика в интерфейсе

            // Проверка завершения текущей фазы
            if (miceCaughtInPhase >= targetMiceForCurrentPhase) // Если набрано нужное количество
            {
                OnPhaseCompleted(); // Завершение фазы и переход дальше
            }
        }
        else // Если поймана неправильная мышь
        {
            // ОШИБКА: ИГРОК НАЖАЛ НА ЗАПРЕЩЕННУЮ МЫШКУ!
            if (wrongMouseSound != null && SettingsManager.Instance != null) // Если звук ошибки задан
                SettingsManager.Instance.PlaySoundEffect(wrongMouseSound); // Воспроизведение звука ошибки

            // Сброс текущей фазы и перезапуск заново согласно правилам
            string errorReason = $"Ой! Вы поймали не ту мышь! Этап {currentPhase} сброшен заново!"; // Сообщение об ошибке
            RestartCurrentPhase(errorReason); // Перезапуск этапа
        }
    }

    /// <summary>
    /// Перезапуск текущей фазы при ошибке
    /// </summary>
    private void RestartCurrentPhase(string notice) // Сброс этапа и отображение баннера предупреждения
    {
        ClearAllMice(); // Удаление всех бегающих мышек
        miceCaughtInPhase = 0; // Сброс счетчика пойманных мышей
        UpdateTaskUI(); // Обновление счетчика в UI

        StartCoroutine(ShowCentralNoticeRoutine("ОШИБКА!", notice, () => // Показ баннера ошибки
        {
            StartPhase(currentPhase); // Повторный старт текущего этапа
        }));
    }

    /// <summary>
    /// Успешное прохождение фазы и переход к следующей
    /// </summary>
    private void OnPhaseCompleted() // Обработка успешного завершения раунда/фазы
    {
        ClearAllMice(); // Очистка мышек с поля

        if (phaseCompleteSound != null && SettingsManager.Instance != null) // Если звук завершения задан
            SettingsManager.Instance.PlaySoundEffect(phaseCompleteSound); // Проигрывание звука успеха

        if (currentPhase == 1) // Если пройдена 1-я фаза
        {
            // Переход к Фазе 2: Серебряные (10)
            StartCoroutine(ShowCentralNoticeRoutine( // Показ баннера перехода на 2 этап
                "ОТЛИЧНО! НО ЭТО ЕЩЕ НЕ ВСЁ!", // Заголовок уведомления
                "Вы поймали 5 золотых мышей! Теперь поймайте <b>10 серебряных</b>.\nЧерных и золотых не трогать!", // Текст задачи
                () => StartPhase(2) // Запуск 2-й фазы
            ));
        }
        else if (currentPhase == 2) // Если пройдена 2-я фаза
        {
            // Переход к Фазе 3: Черные (20)
            StartCoroutine(ShowCentralNoticeRoutine( // Показ баннера перехода на 3 этап
                "МОЛОДЕЦ! ФИНАЛЬНЫЙ РЫВОК!", // Заголовок уведомления
                "Вы поймали 10 серебряных мышей! Теперь усиленно ловите <b>20 самых быстрых черных мышей</b>!", // Текст задачи
                () => StartPhase(3) // Запуск 3-й фазы
            ));
        }
        else if (currentPhase == 3) // Если пройдена финальная 3-я фаза
        {
            // ПОЛНАЯ ПОБЕДА ВО ВСЕХ 3 ЭТАПАХ!
            isGameWon = true; // Установка флага победы
            isGameActive = false; // Отключение активности игры

            if (winFanfareSound != null && SettingsManager.Instance != null) // Если звук фанфар задан
                SettingsManager.Instance.PlaySoundEffect(winFanfareSound); // Проигрывание победного звука

            ShowVictoryPopup(); // Показ окна триумфа и наград
        }
    }

    private IEnumerator ShowCentralNoticeRoutine(string title, string body, System.Action onComplete) // Корутина отображения центрального баннера фазы
    {
        isPhaseTransitioning = true; // Блокировка управления на время перехода

        if (centralPhaseNoticePanel != null) // Если панель баннера есть
        {
            centralPhaseNoticePanel.SetActive(true); // Включение панели уведомления
            if (centralNoticeTitle != null) centralNoticeTitle.text = title; // Установка заголовка
            if (centralNoticeBody != null) centralNoticeBody.text = body; // Установка описания
        }

        yield return new WaitForSeconds(3.0f); // Пауза отображения баннера 3 секунды

        if (centralPhaseNoticePanel != null) // Если панель баннера есть
            centralPhaseNoticePanel.SetActive(false); // Скрытие баннера

        isPhaseTransitioning = false; // Снятие блокировки управления
        onComplete?.Invoke(); // Вызов коллбэка продолжения
    }

    public void GetDifficultyRewards(DifficultyLevel diff, out int gold, out int stones, out int scrolls, out int xp, out bool givePotion) // Расчет наград за пройденную сложность
    {
        switch (diff) // Ветвление наград по уровням сложности
        {
            case DifficultyLevel.Easy: // Награды за легкий уровень
                gold = 1000; // 1000 золота
                stones = 5; // 5 камней
                scrolls = 0; // 0 свитков
                xp = 20; // 20 опыта
                givePotion = false; // Без зелья
                break; // Выход из легкого режима
            case DifficultyLevel.Normal: // Награды за обычный уровень
                gold = 2000; // 2000 золота
                stones = 10; // 10 камней
                scrolls = 1; // 1 свиток
                xp = 50; // 50 опыта
                givePotion = false; // Без зелья
                break; // Выход из обычного режима
            case DifficultyLevel.Hard: // Награды за сложный уровень
            default: // По умолчанию для сложного
                gold = 0; // 0 золота
                stones = 20; // 20 камней
                scrolls = 3; // 3 свитка
                xp = 100; // 100 опыта
                givePotion = true; // +1 шт Зелье Опыта Мастерства (+100 XP)
                break; // Выход из сложного режима
        }
    }

    /// <summary>
    /// Отображение окна победы с точными наградами за выбранный уровень сложности
    /// </summary>
    private void ShowVictoryPopup() // Отображение победного окна с перечислением полученных наград
    {
        if (rewardPopupPanel != null) // Если панель победы есть
        {
            rewardPopupPanel.SetActive(true); // Включение окна победы
        }

        GetDifficultyRewards(selectedDifficulty, out int finalGold, out int finalStones, out int finalScrolls, out int finalXP, out bool givePotion); // Получение значений наград

        string diffName = selectedDifficulty == DifficultyLevel.Easy ? "Легкий" : // Имя легкого уровня
                         (selectedDifficulty == DifficultyLevel.Normal ? "Средний" : "Сложный"); // Имя обычного или сложного

        if (rewardDescriptionText != null) // Если текстовое поле наград задано
        {
            string goldStr = finalGold > 0 ? $"<b><color=#FFD166>+{finalGold:N0} Золота</color></b> | " : ""; // Строка золота
            string scrollsStr = finalScrolls > 0 ? $" | <b><color=#CDB4DB>+{finalScrolls} Свиток</color></b>" : ""; // Строка свитков
            string potionStr = givePotion ? $"\n<b><color=#F72585>+1 Зелье Опыта Мастерства (+100 XP)</color></b>" : ""; // Строка бонусного зелья

            rewardDescriptionText.text = $"Уровень: <b><color=#FFD166>{diffName}</color></b>\n" + // Сборка итогового описания награды
                                         $"{goldStr}<b><color=#A0C4FF>+{finalStones} Камней</color></b>{scrollsStr}\n" + // Строка камней и свитков
                                         $"<b><color=#80FFDB>+{finalXP} Опыта Игрока</color></b>" + // Строка опыта игрока
                                         potionStr; // Прикрепление строки зелья

            RectTransform txtRt = rewardDescriptionText.GetComponent<RectTransform>(); // Трансформ текстового блока
            if (txtRt != null) // Если компонент есть
            {
                // Размещаем текст компактно над кнопкой
                txtRt.anchoredPosition = new Vector2(0f, -40f); // Позиционирование по Y
            }
        }

        if (claimRewardButton != null) // Если кнопка забрать назначена
        {
            RectTransform btnRt = claimRewardButton.GetComponent<RectTransform>(); // Трансформ кнопки забрать
            if (btnRt != null) // Если компонент есть
            {
                // Смещаем кнопку вниз, чтобы она не перекрывала список наград
                btnRt.anchoredPosition = new Vector2(0f, -190f); // Позиционирование кнопки по Y
            }

            TextMeshProUGUI btnText = claimRewardButton.GetComponentInChildren<TextMeshProUGUI>(); // Текст на кнопке
            if (btnText != null) // Если компонент текста найден
            {
                int lang = PlayerPrefs.GetInt("SelectedLanguage", 0); // Получение выбранного языка
                string claimLabel = (lang == 1) ? "Claim" : ((lang == 2) ? "Al" : "Забрать"); // Локализация надписи
                btnText.text = claimLabel; // Применение текста на кнопке
                btnText.enableAutoSizing = true; // Включение авторазмера шрифта
                btnText.fontSizeMin = 14f; // Минимальный размер шрифта
                btnText.fontSizeMax = 24f; // Максимальный размер шрифта
                btnText.alignment = TextAlignmentOptions.Center; // Центровка текста
                btnText.color = new Color(1f, 0.95f, 0.69f, 1f); // #FFF3B0 Яркий золотистый цвет текста
            }
        }
    }

    /// <summary>
    /// Выдача всех наград игроку, сохранение, блокировка колеса игр и запуск победного диалога Кота
    /// </summary>
    public void ClaimRewardAndExit() // Зачисление наград на баланс игрока и закрытие окна
    {
        if (clickSound != null && SettingsManager.Instance != null) // Если звук клика задан
            SettingsManager.Instance.PlaySoundEffect(clickSound); // Проигрывание звука кнопки

        GetDifficultyRewards(selectedDifficulty, out int finalGold, out int finalStones, out int finalScrolls, out int finalXP, out bool givePotion); // Расчет наград

        // 1. Начисление ресурсов в PlayerPrefs
        int gold = PlayerPrefs.GetInt("Player_Gold", 0) + finalGold; // Начисление золота
        int stones = PlayerPrefs.GetInt("Player_Stones", 0) + finalStones; // Начисление камней
        int scrolls = PlayerPrefs.GetInt("Player_Scrolls", 0) + finalScrolls; // Начисление свитков

        PlayerPrefs.SetInt("Player_Gold", gold); // Сохранение золота в PlayerPrefs
        PlayerPrefs.SetInt("Player_Stones", stones); // Сохранение камней в PlayerPrefs
        PlayerPrefs.SetInt("Player_Scrolls", scrolls); // Сохранение свитков в PlayerPrefs
        PlayerPrefs.Save(); // Запись на диск

        // 2. Добавление зелья в инвентарь при сложном уровне
        if (givePotion && RecipeCrafting_Manager.Instance != null) // Если положено зелье и менеджер крафта доступен
        {
            RecipeCrafting_Manager.Instance.AddPotionToFirstEmptySlot("Player_Potion_XP_Mastery", "Зелье Мастерства (+100 XP)"); // Добавление зелья в слот
        }

        // 3. Обновление ресурсов в верхнем UI
        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов доступен
        {
            DialogueSystem_Manager.Instance.RefreshResourceDisplay(); // Обновление счетчиков в шапке
        }

        // 4. Начисление опыта аватара
        if (Avatar_Manager.Instance != null) // Если менеджер аватаров доступен
        {
            Avatar_Manager.Instance.GainPlayerExperience(finalXP); // Начисление опыта персонажу
        }

        // 5. Блокировка колеса мини-игр
        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов доступен
        {
            DialogueSystem_Manager.Instance.SetMinigamesButtonInteractable(false); // Деактивация кнопки колеса игр
        }

        CloseMinigame(); // Закрытие окна мини-игры

        // 6. Запуск диалога Кота с подарком и предложением «Алхимической Рыбалки»
        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов доступен
        {
            DialogueSystem_Manager.Instance.StartPostMinigameFishingDialogue(); // Запуск сюжетного диалога после мини-игры
        }
    }

    public void CloseMinigame() // Полное закрытие панели игры и очистка поля
    {
        ClearAllMice(); // Очистка мышек
        if (gamePanel != null) // Если панель игры назначена
            gamePanel.SetActive(false); // Скрытие панели игры
        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов доступен
            DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление видимости HUD
    }

    private void ClearAllMice() // Удаление всех активных мышей из сцены и очистка списка
    {
        foreach (var m in activeMice) // Перебор всех объектов в списке
        {
            if (m != null) Destroy(m); // Уничтожение каждого объекта мыши
        }
        activeMice.Clear(); // Очистка списка ссылок
    }

    private Sprite GetMouseSprite(MouseType type) // Получение спрайта для соответствующего типа мыши
    {
        switch (type) // Ветвление по типу мыши
        {
            case MouseType.Golden: return goldenMouseSprite; // Спрайт золотой мыши
            case MouseType.Silver: return silverMouseSprite; // Спрайт серебряной мыши
            case MouseType.Black: return blackMouseSprite; // Спрайт черной мыши
            default: return goldenMouseSprite; // Фоллбэк: золотая мышь
        }
    }

    private Sprite[] GetAnimationFrames(MouseType type) // Получение массива кадров бега для типа мыши
    {
        switch (type) // Ветвление по типу мыши
        {
            case MouseType.Golden: return goldenMouseFrames; // Кадры золотой мыши
            case MouseType.Silver: return silverMouseFrames; // Кадры серебряной мыши
            case MouseType.Black: return shadowMouseFrames; // Кадры теневой мыши
            default: return null; // Отсутствие анимации
        }
    }
}
