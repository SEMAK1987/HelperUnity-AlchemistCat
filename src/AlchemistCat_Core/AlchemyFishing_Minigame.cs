using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum FishingDifficulty
{
    Easy,   // Легкий уровень: 10 попыток, скорость 0.30, Зона 4: 35%
    Medium, // Средний уровень: 10 попыток, скорость 0.50, Зона 4: 22%
    Hard    // Сложный уровень: 12 попыток, скорость 0.90, Зона 4: 12%
}

/// <summary>
/// Алхимическая Рыбалка: 3 уровня сложности (Легкий - 10 попыток, Норма - 10, Сложный - 12),
/// настраиваемые скорости расхождения (0.3 / 0.5 / 0.9), боковая панель текущих целей/улова,
/// всплывающий тост единичного улова, разделители зон горизонтальной шкалы
/// и справочник улова с кнопкой рыбки.
/// </summary>
public class AlchemyFishing_Minigame : MonoBehaviour
{
    public static AlchemyFishing_Minigame Instance; // Синглтон мини-игры рыбалки

    [Header("=== Главные панели ===")]
    public GameObject difficultySelectPanel; // Панель выбора сложности (Легко / Средне / Сложно)
    public GameObject activeFishingStagePanel; // Основная игровая панель процесса рыбалки
    public GameObject resultSummaryPopupPanel; // Итоговое окно подсчета улова
    public Button closeButton; // Кнопка возврата / закрытия

    [Header("=== Кнопки выбора сложности ===")]
    public Button easyButton; // Кнопка выбора легкой сложности
    public Button mediumButton; // Кнопка выбора средней сложности
    public Button hardButton; // Кнопка выбора сложной сложности

    [Header("=== Счетчик попыток и опыт ===")]
    public TextMeshProUGUI attemptsCounterText; // Текст счетчика попыток ("Попытка: 1 / 10")
    public TextMeshProUGUI totalSessionXpText; // Текст суммарного набранного опыта
    private int currentAttempt = 1; // Текущий номер попытки
    private FishingDifficulty currentDifficulty = FishingDifficulty.Easy; // Текущая выбранная сложность

    [Header("=== Скорости горизонтальной шкалы по сложностям ===")]
    public float speedEasy = 0.30f; // Скорость расхождения для легкого уровня
    public float speedMedium = 0.50f; // Скорость расхождения для нормального уровня
    public float speedHard = 0.90f; // Скорость расхождения для сложного уровня

    [Header("=== Удочка (FishRod_Visual в правом нижнем углу) ===")]
    public Button fishRodButton; // Кнопка удочки в нижнем углу экрана
    public RectTransform fishRodTransform; // Трансформ спрайта удочки для анимации взмаха
    public Image fishRodImage; // Изображение удочки
    public RectTransform bobberTransform; // Поплавок на водной глади

    [Header("=== Вертикальная шкала заброса (Шкала 1: 4 Сектора и 3 разделителя) ===")]
    public GameObject verticalBarContainer; // Контейнер вертикальной шкалы заброса
    public RectTransform verticalBarBg; // Фон вертикальной шкалы
    public RectTransform verticalSliderArrow; // Бегунок-стрелка вертикальной шкалы
    public RectTransform delimiterZone4; // Линия разделителя 4-го сектора (золотая зона)
    public RectTransform delimiterZone3; // Линия разделителя 3-го сектора
    public RectTransform delimiterZone2; // Линия разделителя 2-го сектора
    public float baseVerticalSpeed = 1.0f; // Базовая плавная скорость движения вертикального бегунка

    [Header("=== Горизонтальная шкала поклевки (Шкала 2: 2 расходящихся луча) ===")]
    public GameObject horizontalBarContainer; // Контейнер горизонтальной шкалы поклевки
    public RectTransform horizontalBarBg; // Фон горизонтальной шкалы
    public RectTransform leftMovingBeam; // Левый луч, растущий от центра влево
    public RectTransform rightMovingBeam; // Правый луч, растущий от центра вправо
    [Range(10f, 250f)] public float beamHeight = 100f; // Настраиваемая вручную высота (толщина) фиолетовых полос
    [Range(0.2f, 1f)] public float maxBeamSpanRatio = 0.80f; // Максимальная длина расхождения лучей от центра
    public bool autoTaperBeams = true; // Автоматическое заострение концов полос в форме кристалла
    [Range(0f, 100f)] public float beamTaperWidth = 45f; // Длина заострения на концах лучей

    [Header("=== Разделители зон горизонтальной шкалы (по 2 с каждой стороны) ===")]
    public RectTransform leftZoneDelimiter1; // Ближняя левая риска
    public RectTransform leftZoneDelimiter2; // Дальняя левая риска
    public RectTransform rightZoneDelimiter1; // Ближняя правая риска
    public RectTransform rightZoneDelimiter2; // Дальняя правая риска

    [Header("=== Кнопка действия ===")]
    public Button actionButton; // Большая кнопка "Подсечь!" / "Тянуть!"
    public TextMeshProUGUI actionButtonText; // Текст на кнопке действия

    [Header("=== Всплывающее окно единичного улова (Catch Toast) ===")]
    public GameObject singleCatchToastPanel; // Панель всплывающего уведомления о пойманном предмете
    public Image singleCatchIcon; // Иконка пойманного предмета
    public TextMeshProUGUI singleCatchNameText; // Название пойманного предмета
    public TextMeshProUGUI singleCatchXpText; // Опыт за пойманный предмет

    [Header("=== Боковая панель улова / целей сессии ===")]
    public GameObject sideLootPanel; // Боковая панель текущих целей/улова на пруду
    public Transform sideLootContainer; // Контейнер с ячейками улова
    public GameObject sideLootItemPrefab; // Префаб ячейки для боковой панели
    public TextMeshProUGUI sideLootCounterText; // Текст общего количества улова ("Улов: 3/10")

    [Header("=== Справочник улова / Атлас водоема (Значок Рыбки) ===")]
    public Button pondCatchGuideButton; // Кнопка открытия справочника со значком рыбки
    public GameObject pondCatchGuidePopup; // Всплывающее окно справочника зон и рыб
    public Button pondCatchGuideCloseButton; // Кнопка закрытия справочника

    [Header("=== Итоговое окно попыток (Result_Summary_Popup_Panel) ===")]
    public Transform summaryLootContainer; // Контейнер иконок пойманного лута
    public GameObject summaryItemPrefab; // Префаб ячейки лута в окне итогов
    public TextMeshProUGUI summaryGoldText; // Текст итогового золота
    public TextMeshProUGUI summaryStonesText; // Текст итоговых камней
    public TextMeshProUGUI summaryScrollsText; // Текст итоговых свитков
    public TextMeshProUGUI summaryPotionBonusText; // Текст бонусных зелий
    public Button claimAllToBackpackButton; // Кнопка "Забрать все в рюкзак"

    [Header("=== Спрайты наград ===")]
    public Sprite trashBottleSprite; // Спрайт старой бутылки (мусор)
    public Sprite duckweedSprite; // Спрайт ряски
    public Sprite runeStoneSprite; // Спрайт рунного камня
    public Sprite potion10Sprite; // Зелье +10 XP
    public Sprite potion50Sprite; // Зелье +50 XP
    public Sprite potion100Sprite; // Зелье +100 XP
    public Sprite potion300Sprite; // Зелье +300 XP
    public Sprite potion500Sprite; // Зелье +500 XP
    public Sprite potion1000Sprite; // Зелье +1000 XP
    public Sprite potion3000Sprite; // Зелье Дракона +3000 XP

    // Внутренние состояния
    public enum GamePhase { Idle, VerticalCasting, HorizontalCatching, Splashing, SingleResultToast, Finished } // Фазы рыбалки
    private GamePhase currentPhase = GamePhase.Idle; // Текущая фаза

    private float verticalValue = 0.5f; // Текущее положение вертикального бегунка (0..1)
    private float horizontalSpread = 0f; // Текущее расхождение горизонтальных лучей (0..1)
    private int verticalDirection = 1; // Направление движения бегунка по вертикали
    private int horizontalDirection = 1; // Направление расхождения лучей

    private float lockedVertical = 0f; // Зафиксированное значение вертикальной шкалы
    private float lockedHorizontal = 0f; // Зафиксированное значение горизонтальной шкалы

    private List<LootResult> caughtSessionLoot = new List<LootResult>(); // Список наград за текущую сессию
    private int totalSessionXpGained = 0; // Суммарно набранный опыт

    [System.Serializable]
    public struct LootResult
    {
        public string itemId; // Идентификатор предмета
        public string itemName; // Русское название предмета
        public int xp; // Опыт за предмет
        public Sprite sprite; // Спрайт иконки
        public Color rarityColor; // Цвет рамки редкости
    }

    public int GetMaxAttemptsForDifficulty(FishingDifficulty diff) // Максимум попыток для сложности
    {
        return diff == FishingDifficulty.Hard ? 12 : 10; // Сложный = 12, Легкий и Средний = 10
    }

    public float GetHorizontalSpeedForDifficulty(FishingDifficulty diff) // Скорость горизонтальной шкалы
    {
        switch (diff)
        {
            case FishingDifficulty.Easy: return speedEasy > 0 ? speedEasy : 0.30f; // Легкий 0.3
            case FishingDifficulty.Medium: return speedMedium > 0 ? speedMedium : 0.50f; // Норма 0.5
            case FishingDifficulty.Hard: return speedHard > 0 ? speedHard : 0.90f; // Сложный 0.9
            default: return 0.50f;
        }
    }

    private void Awake() // Инициализация при создании объекта
    {
        Instance = this; // Инициализация синглтона

        // Автоматический поиск панелей, если не назначены в инспекторе
        if (activeFishingStagePanel == null)
        {
            Transform t = transform.Find("Active_Fishing_Stage");
            if (t != null) activeFishingStagePanel = t.gameObject;
        }
        if (difficultySelectPanel == null)
        {
            Transform t = transform.Find("Difficulty_Selection_Panel");
            if (t != null) difficultySelectPanel = t.gameObject;
        }
        if (resultSummaryPopupPanel == null)
        {
            Transform t = transform.Find("Result_Summary_Popup_Panel");
            if (t != null) resultSummaryPopupPanel = t.gameObject;
        }
    }

#if UNITY_EDITOR
    private void OnValidate() // Безопасное отложенное обновление высоты лучей в редакторе Unity
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return; // Проверка на уничтожение объекта
            ApplyBeamHeight(); // Применение высоты
            PositionHorizontalDelimiters(); // Расстановка разделителей
        };
    }
#endif

    public void ApplyBeamHeight() // Метод применения высоты лучей и настройки формы заострения
    {
        if (leftMovingBeam != null && beamHeight > 0)
        {
            leftMovingBeam.sizeDelta = new Vector2(leftMovingBeam.sizeDelta.x, beamHeight); // Применение высоты левого луча
            if (autoTaperBeams)
            {
                UI_TaperedBeamEffect tL = leftMovingBeam.GetComponent<UI_TaperedBeamEffect>();
                if (tL == null) tL = leftMovingBeam.gameObject.AddComponent<UI_TaperedBeamEffect>();
                tL.taperSide = UI_TaperedBeamEffect.TaperSide.Left;
                tL.taperWidth = beamTaperWidth;
                tL.enabled = true;
            }
        }
        if (rightMovingBeam != null && beamHeight > 0)
        {
            rightMovingBeam.sizeDelta = new Vector2(rightMovingBeam.sizeDelta.x, beamHeight); // Применение высоты правого луча
            if (autoTaperBeams)
            {
                UI_TaperedBeamEffect tR = rightMovingBeam.GetComponent<UI_TaperedBeamEffect>();
                if (tR == null) tR = rightMovingBeam.gameObject.AddComponent<UI_TaperedBeamEffect>();
                tR.taperSide = UI_TaperedBeamEffect.TaperSide.Right;
                tR.taperWidth = beamTaperWidth;
                tR.enabled = true;
            }
        }
    }

    public void PositionHorizontalDelimiters() // Автоматическая расстановка рисок-разделителей на шкале поклевки
    {
        if (horizontalBarBg == null) return;
        float halfW = horizontalBarBg.rect.width * 0.5f;

        if (leftZoneDelimiter1 != null) leftZoneDelimiter1.anchoredPosition = new Vector2(-halfW * 0.35f, 0); // Ближняя левая
        if (leftZoneDelimiter2 != null) leftZoneDelimiter2.anchoredPosition = new Vector2(-halfW * 0.68f, 0); // Дальняя левая
        if (rightZoneDelimiter1 != null) rightZoneDelimiter1.anchoredPosition = new Vector2(halfW * 0.35f, 0); // Ближняя правая
        if (rightZoneDelimiter2 != null) rightZoneDelimiter2.anchoredPosition = new Vector2(halfW * 0.68f, 0); // Дальняя правая
    }

    private void OnEnable() // При активации панели рыбалки
    {
        ApplyBeamHeight(); // Применение заданной толщины полос
        PositionHorizontalDelimiters(); // Расстановка разделителей
        if (DialogueSystem_Manager.Instance != null) // Если диалоговый менеджер доступен
        {
            DialogueSystem_Manager.Instance.HideHUDForMinigame(); // Скрытие аватарки и кнопок справа
        }
    }

    private void OnDisable() // При деактивации панели рыбалки
    {
        if (DialogueSystem_Manager.Instance != null) // Если диалоговый менеджер доступен
        {
            DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление аватарки и кнопок справа
        }
    }

    private void Start() // Стартовая инициализация подписчиков и панелей
    {
        if (easyButton) easyButton.onClick.AddListener(() => StartFishingSession(FishingDifficulty.Easy)); // Выбор легкого уровня
        if (mediumButton) mediumButton.onClick.AddListener(() => StartFishingSession(FishingDifficulty.Medium)); // Выбор среднего уровня
        if (hardButton) hardButton.onClick.AddListener(() => StartFishingSession(FishingDifficulty.Hard)); // Выбор сложного уровня

        if (closeButton) closeButton.onClick.AddListener(HandleCloseClicked); // Закрытие окна
        if (fishRodButton) fishRodButton.onClick.AddListener(OnRodOrActionButtonClicked); // Клик по удочке
        if (actionButton) actionButton.onClick.AddListener(OnRodOrActionButtonClicked); // Клик по кнопке действия
        if (claimAllToBackpackButton) claimAllToBackpackButton.onClick.AddListener(ClaimAllAndProceedToQuest); // Забрать лут

        if (pondCatchGuideButton) pondCatchGuideButton.onClick.AddListener(OpenCatchGuide); // Открытие справочника
        if (pondCatchGuideCloseButton) pondCatchGuideCloseButton.onClick.AddListener(CloseCatchGuide); // Закрытие справочника

        // Привязка всех крестиков в дочерних панелях к HandleCloseClicked
        if (activeFishingStagePanel != null)
        {
            Button[] stageButtons = activeFishingStagePanel.GetComponentsInChildren<Button>(true);
            foreach (var b in stageButtons)
            {
                if (b == pondCatchGuideButton || b == pondCatchGuideCloseButton) continue;
                if (b.gameObject.name.ToLower().Contains("close") || b.gameObject.name.ToLower().Contains("back"))
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(HandleCloseClicked);
                }
            }
        }

        // Автоматический поиск текста на удочке и бейджа попыток
        if (actionButtonText == null && fishRodButton != null)
        {
            actionButtonText = fishRodButton.GetComponentInChildren<TextMeshProUGUI>(); // Поиск дочернего текста
        }
        if (attemptsCounterText == null && activeFishingStagePanel != null)
        {
            Transform badge = activeFishingStagePanel.transform.Find("Attempts_Badge");
            if (badge != null) attemptsCounterText = badge.GetComponentInChildren<TextMeshProUGUI>();
        }

        ApplyBeamHeight(); // Применение высоты
        PositionHorizontalDelimiters(); // Расстановка разделителей
    }

    public void OpenCatchGuide() // Открытие справочника зон и рыб
    {
        if (pondCatchGuidePopup) pondCatchGuidePopup.SetActive(true); // Показ окна справочника
    }

    public void CloseCatchGuide() // Закрытие справочника
    {
        if (pondCatchGuidePopup) pondCatchGuidePopup.SetActive(false); // Скрытие окна справочника
    }

    public void OpenMinigame() // Открытие окна Алхимической Рыбалки
    {
        gameObject.SetActive(true); // Активация объекта игры
        Transform p = transform.parent;
        while (p != null)
        {
            p.gameObject.SetActive(true); // Гарантированное включение родительских панелей
            p = p.parent;
        }

        if (difficultySelectPanel != null && (easyButton != null || mediumButton != null || hardButton != null))
        {
            ShowDifficultySelection(); // Показ выбора сложности
        }
        else
        {
            StartFishingSession(FishingDifficulty.Easy); // Старт на легком уровне
        }
    }

    public void ShowDifficultySelection() // Показ экрана выбора уровня сложности
    {
        currentPhase = GamePhase.Idle; // Сброс фазы рыбалки
        if (difficultySelectPanel) difficultySelectPanel.SetActive(true); // Включение панели сложности
        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(false); // Выключение игровой панели
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false); // Выключение итогового окна
        if (singleCatchToastPanel) singleCatchToastPanel.SetActive(false); // Выключение тоста
        if (pondCatchGuidePopup) pondCatchGuidePopup.SetActive(false); // Выключение справочника
    }

    public void HandleCloseClicked() // Обработка закрытия окна рыбалки / возврат к выбору сложности
    {
        // 1. Если игрок находится на этапе активной рыбалки на пруду или в окне итогов
        if ((activeFishingStagePanel != null && activeFishingStagePanel.activeSelf) ||
            (resultSummaryPopupPanel != null && resultSummaryPopupPanel.activeSelf))
        {
            if (difficultySelectPanel != null)
            {
                ShowDifficultySelection(); // Возвращаемся на выбор сложности
                return;
            }
        }

        // 2. Если уже на экране выбора сложности (закрытие мини-игры)
        gameObject.SetActive(false);
        if (transform.parent != null && (transform.parent.name == "MinigamesPanel" || transform.parent.name.Contains("Minigames")))
        {
            transform.parent.gameObject.SetActive(false);
        }
        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление HUD
        }
    }

    public void StartFishingSession(FishingDifficulty difficulty) // Старт сессии рыбалки
    {
        currentDifficulty = difficulty; // Запоминаем выбранную сложность
        currentAttempt = 1; // Сброс номера попытки на 1
        caughtSessionLoot.Clear(); // Очистка накопленного улова
        totalSessionXpGained = 0; // Сброс опыта сессии

        if (difficultySelectPanel) difficultySelectPanel.SetActive(false); // Прячем выбор сложности
        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(true); // Открываем экран рыбалки
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false); // Прячем результаты
        if (singleCatchToastPanel) singleCatchToastPanel.SetActive(false); // Прячем тост
        if (pondCatchGuidePopup) pondCatchGuidePopup.SetActive(false); // Прячем справочник

        ConfigureDifficultySettings(); // Конфигурация шкал под уровень
        UpdateSideLootUI(); // Обновление боковой панели улова
        ResetAttemptToIdle(); // Сброс в режим ожидания заброса
    }

    private void ConfigureDifficultySettings() // Конфигурация высоты зон и разделителей
    {
        float z4Height = currentDifficulty == FishingDifficulty.Easy ? 0.35f : 
                         currentDifficulty == FishingDifficulty.Medium ? 0.22f : 0.12f;

        if (delimiterZone4 && verticalBarBg)
        {
            float totalH = verticalBarBg.rect.height;
            delimiterZone4.anchoredPosition = new Vector2(0, totalH * (1f - z4Height)); // Позиционирование 4 зоны
        }
    }

    private void ResetAttemptToIdle() // Сброс состояния попытки в ожидание клика по удочке
    {
        currentPhase = GamePhase.Idle;
        int maxAttempts = GetMaxAttemptsForDifficulty(currentDifficulty); // Максимальное количество попыток для текущего режима

        if (attemptsCounterText) attemptsCounterText.text = $"Попытка: {currentAttempt} / {maxAttempts}"; // Текст счетчика попыток
        if (totalSessionXpText) totalSessionXpText.text = $"+{totalSessionXpGained} XP"; // Текст набранного опыта

        if (verticalBarContainer) verticalBarContainer.SetActive(false); // Прячем вертикальную шкалу
        if (horizontalBarContainer) horizontalBarContainer.SetActive(false); // Прячем горизонтальную шкалу
        if (bobberTransform) bobberTransform.gameObject.SetActive(false); // Прячем поплавок
        if (singleCatchToastPanel) singleCatchToastPanel.SetActive(false); // Прячем тост

        // Разблокировка удочки
        if (fishRodButton) fishRodButton.interactable = true;
        if (fishRodImage) fishRodImage.color = Color.white;
        if (actionButtonText) actionButtonText.text = "ЗАБРОС!"; // Текст кнопки
    }

    public void OnRodOrActionButtonClicked() // Обработка клика по удочке или кнопке действия
    {
        switch (currentPhase)
        {
            case GamePhase.Idle: // Фаза 1: запуск вертикальной шкалы
                currentPhase = GamePhase.VerticalCasting;
                if (fishRodButton) fishRodButton.interactable = true;
                if (fishRodImage) fishRodImage.color = Color.white;
                if (verticalBarContainer) verticalBarContainer.SetActive(true);
                if (horizontalBarContainer) horizontalBarContainer.SetActive(false);
                if (actionButtonText) actionButtonText.text = "СТОП!";
                break;

            case GamePhase.VerticalCasting: // Фаза 2: фиксация дальности и запуск горизонтальной шкалы
                lockedVertical = verticalValue;
                currentPhase = GamePhase.HorizontalCatching;
                if (fishRodButton) fishRodButton.interactable = true;
                if (fishRodImage) fishRodImage.color = Color.white;
                if (verticalBarContainer) verticalBarContainer.SetActive(false);
                if (horizontalBarContainer) horizontalBarContainer.SetActive(true);
                if (actionButtonText) actionButtonText.text = "ПОДСЕЧЬ!";

                StartCoroutine(AnimateRodCast(lockedVertical)); // Запуск анимации взмаха
                break;

            case GamePhase.HorizontalCatching: // Фаза 3: подсечка и выуживание
                lockedHorizontal = horizontalSpread;
                currentPhase = GamePhase.Splashing;
                if (fishRodButton) fishRodButton.interactable = false;
                if (fishRodImage) fishRodImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                if (horizontalBarContainer) horizontalBarContainer.SetActive(false);
                if (actionButtonText) actionButtonText.text = "ТЯНЕМ УЛОВ..."; // Без эмодзи (чистый текст)

                StartCoroutine(ProcessCatchResult(lockedVertical, lockedHorizontal)); // Расчет улова
                break;
        }
    }

    private void Update() // Покадровое обновление движения бегунков
    {
        float vertSpeed = baseVerticalSpeed * (currentDifficulty == FishingDifficulty.Easy ? 1.0f : currentDifficulty == FishingDifficulty.Medium ? 1.4f : 1.9f);
        float horizSpeed = GetHorizontalSpeedForDifficulty(currentDifficulty); // Получение скорости по сложности (0.3 / 0.5 / 0.9)

        if (currentPhase == GamePhase.VerticalCasting) // Если активна вертикальная шкала
        {
            verticalValue += verticalDirection * vertSpeed * Time.deltaTime;
            if (verticalValue >= 1f) { verticalValue = 1f; verticalDirection = -1; }
            else if (verticalValue <= 0f) { verticalValue = 0f; verticalDirection = 1; }

            if (verticalSliderArrow && verticalBarBg)
            {
                float totalH = verticalBarBg.rect.height;
                verticalSliderArrow.anchoredPosition = new Vector2(verticalSliderArrow.anchoredPosition.x, verticalValue * totalH);
            }
        }
        else if (currentPhase == GamePhase.HorizontalCatching) // Если активна горизонтальная шкала
        {
            horizontalSpread += horizontalDirection * horizSpeed * Time.deltaTime;
            if (horizontalSpread >= 1f) { horizontalSpread = 1f; horizontalDirection = -1; }
            else if (horizontalSpread <= 0f) { horizontalSpread = 0f; horizontalDirection = 1; }

            if (horizontalBarBg)
            {
                float totalW = horizontalBarBg.rect.width;
                float halfW = totalW * 0.5f;
                
                float spanRatio = maxBeamSpanRatio > 0 ? maxBeamSpanRatio : 0.80f;
                float maxSpan = Mathf.Max(20f, halfW * spanRatio); 
                float currentSpan = Mathf.Lerp(15f, maxSpan, horizontalSpread);
                
                float h = beamHeight > 0 ? beamHeight : 100f; // Прямая высота из инспектора

                if (leftMovingBeam)
                {
                    leftMovingBeam.pivot = new Vector2(1f, 0.5f);
                    leftMovingBeam.anchoredPosition = Vector2.zero;
                    leftMovingBeam.localScale = Vector3.one;
                    leftMovingBeam.sizeDelta = new Vector2(currentSpan, h);
                }

                if (rightMovingBeam)
                {
                    rightMovingBeam.pivot = new Vector2(0f, 0.5f);
                    rightMovingBeam.anchoredPosition = Vector2.zero;
                    rightMovingBeam.localScale = Vector3.one;
                    rightMovingBeam.sizeDelta = new Vector2(currentSpan, h);
                }
            }
        }
    }

    private IEnumerator AnimateRodCast(float power) // Корутина анимации взмаха удочки и броска поплавка
    {
        if (fishRodTransform)
        {
            fishRodTransform.localRotation = Quaternion.Euler(0, 0, -20f); // Замах назад
            yield return new WaitForSeconds(0.2f);
            fishRodTransform.localRotation = Quaternion.Euler(0, 0, 0); // Возврат
        }

        if (bobberTransform)
        {
            bobberTransform.gameObject.SetActive(true);
            bobberTransform.anchoredPosition = new Vector2(0, -50f + power * 120f); // Полет поплавка
        }
    }

    private IEnumerator ProcessCatchResult(float vVal, float hVal) // Корутина расчета выловленного предмета
    {
        yield return new WaitForSeconds(0.8f); // Имитация времени выуживания

        float z4Threshold = 1f - (currentDifficulty == FishingDifficulty.Easy ? 0.35f : 
                                  currentDifficulty == FishingDifficulty.Medium ? 0.22f : 0.12f);
        int sector = vVal >= z4Threshold ? 4 : vVal >= 0.50f ? 3 : vVal >= 0.25f ? 2 : 1;

        float edgeAccuracy = hVal;
        float roll = Random.value;

        LootResult result = new LootResult();

        if (sector == 4 && edgeAccuracy > 0.70f) // Золотой 4-й сектор при точности
        {
            if (roll < 0.08f) { result.itemId = "potion_3000"; result.itemName = "Драконье Зелье Опыта"; result.xp = 3000; result.sprite = potion3000Sprite; result.rarityColor = new Color(1f, 0.4f, 0f); }
            else if (roll < 0.25f) { result.itemId = "potion_1000"; result.itemName = "Мифическое Зелье Опыта"; result.xp = 1000; result.sprite = potion1000Sprite; result.rarityColor = new Color(0.7f, 0.3f, 1f); }
            else if (roll < 0.60f) { result.itemId = "potion_500"; result.itemName = "Легендарное Зелье Опыта"; result.xp = 500; result.sprite = potion500Sprite; result.rarityColor = new Color(1f, 0.85f, 0.2f); }
            else { result.itemId = "potion_300"; result.itemName = "Магическое Зелье Опыта"; result.xp = 300; result.sprite = potion300Sprite; result.rarityColor = new Color(0.9f, 0.2f, 0.3f); }
        }
        else if (sector >= 3) // 3-й сектор
        {
            if (roll < 0.30f) { result.itemId = "potion_100"; result.itemName = "Высокое Зелье Опыта"; result.xp = 100; result.sprite = potion100Sprite; result.rarityColor = new Color(0.6f, 0.3f, 0.9f); }
            else if (roll < 0.70f) { result.itemId = "potion_50"; result.itemName = "Среднее Зелье Опыта"; result.xp = 50; result.sprite = potion50Sprite; result.rarityColor = new Color(0.2f, 0.6f, 1f); }
            else { result.itemId = "rune_stone"; result.itemName = "Магический Рунный Камень"; result.xp = 25; result.sprite = runeStoneSprite; result.rarityColor = new Color(0.4f, 0.9f, 0.9f); }
        }
        else // 1-й и 2-й секторы (тина и ряска)
        {
            if (roll < 0.5f) { result.itemId = "duckweed"; result.itemName = "Болотная тина"; result.xp = 5; result.sprite = duckweedSprite; result.rarityColor = Color.gray; }
            else { result.itemId = "trash_bottle"; result.itemName = "Старая бутылка"; result.xp = 5; result.sprite = trashBottleSprite; result.rarityColor = Color.gray; }
        }

        caughtSessionLoot.Add(result); // Добавление в улов сессии
        totalSessionXpGained += result.xp; // Прибавление опыта

        UpdateSideLootUI(); // Обновление боковой панели целей/улова

        // Показ всплывающего окна улова на 1.5 секунды
        yield return StartCoroutine(ShowSingleCatchToastCoroutine(result));

        int maxAttempts = GetMaxAttemptsForDifficulty(currentDifficulty);
        if (currentAttempt >= maxAttempts) // Если все попытки исчерпаны
        {
            ShowSummaryPopup(); // Показ окна итогов
        }
        else
        {
            currentAttempt++; // Следующая попытка
            ResetAttemptToIdle(); // Сброс в режим ожидания
        }
    }

    private IEnumerator ShowSingleCatchToastCoroutine(LootResult res) // Показ всплывающего уведомления о пойманной рыбе/зелье
    {
        currentPhase = GamePhase.SingleResultToast;
        if (singleCatchToastPanel)
        {
            singleCatchToastPanel.SetActive(true); // Включение тоста
            if (singleCatchIcon)
            {
                singleCatchIcon.sprite = res.sprite; // Иконка предмета
                singleCatchIcon.enabled = res.sprite != null;
            }
            if (singleCatchNameText) singleCatchNameText.text = $"Поймано: {res.itemName}"; // Название предмета
            if (singleCatchXpText) singleCatchXpText.text = res.xp > 0 ? $"+{res.xp} XP" : ""; // Опыт
        }

        yield return new WaitForSeconds(1.5f); // Длительность показа уведомления

        if (singleCatchToastPanel) singleCatchToastPanel.SetActive(false); // Скрытие тоста
    }

    public void UpdateSideLootUI() // Обновление боковой панели улова/целей на экране пруда
    {
        int maxAttempts = GetMaxAttemptsForDifficulty(currentDifficulty);
        if (sideLootCounterText) sideLootCounterText.text = $"Улов: {caughtSessionLoot.Count} / {maxAttempts}"; // Счетчик улова

        if (sideLootContainer == null || sideLootItemPrefab == null) return;

        // Очистка старых ячеек
        foreach (Transform child in sideLootContainer)
        {
            Destroy(child.gameObject);
        }

        // Подсчет количества каждого пойманного предмета
        Dictionary<string, (LootResult loot, int count)> groupedLoot = new Dictionary<string, (LootResult, int)>();
        foreach (var item in caughtSessionLoot)
        {
            if (groupedLoot.ContainsKey(item.itemId))
            {
                var entry = groupedLoot[item.itemId];
                entry.count++;
                groupedLoot[item.itemId] = entry;
            }
            else
            {
                groupedLoot[item.itemId] = (item, 1);
            }
        }

        // Создание аккуратных иконок с бейджами количества
        foreach (var pair in groupedLoot.Values)
        {
            GameObject cell = Instantiate(sideLootItemPrefab, sideLootContainer);
            Image img = cell.GetComponentInChildren<Image>();
            TextMeshProUGUI txt = cell.GetComponentInChildren<TextMeshProUGUI>();

            if (img && pair.loot.sprite != null)
            {
                img.sprite = pair.loot.sprite;
                img.color = Color.white;
            }
            if (txt)
            {
                txt.text = $"x{pair.count}";
            }
        }
    }

    private void ShowSummaryPopup() // Показ финального окна итогов
    {
        currentPhase = GamePhase.Finished;
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(true);

        int gold = currentDifficulty == FishingDifficulty.Easy ? 3000 : currentDifficulty == FishingDifficulty.Medium ? 5000 : 10000;
        int stones = currentDifficulty == FishingDifficulty.Easy ? 3 : currentDifficulty == FishingDifficulty.Medium ? 5 : 10;
        int scrolls = currentDifficulty == FishingDifficulty.Easy ? 1 : currentDifficulty == FishingDifficulty.Medium ? 2 : 5;

        if (summaryGoldText) summaryGoldText.text = $"+{gold:N0} Золота";
        if (summaryStonesText) summaryStonesText.text = $"+{stones} Камней";
        if (summaryScrollsText) summaryScrollsText.text = $"+{scrolls} Свитков";
        if (summaryPotionBonusText)
        {
            summaryPotionBonusText.gameObject.SetActive(currentDifficulty == FishingDifficulty.Hard);
            summaryPotionBonusText.text = "1 шт Зелье Опыта Мастерства (+100 XP)";
        }
    }

    public void ClaimAllAndProceedToQuest() // Забрать все награды в инвентарь
    {
        int gold = currentDifficulty == FishingDifficulty.Easy ? 3000 : currentDifficulty == FishingDifficulty.Medium ? 5000 : 10000;
        int stones = currentDifficulty == FishingDifficulty.Easy ? 3 : currentDifficulty == FishingDifficulty.Medium ? 5 : 10;
        int scrolls = currentDifficulty == FishingDifficulty.Easy ? 1 : currentDifficulty == FishingDifficulty.Medium ? 2 : 5;

        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.AddGold(gold);
            Avatar_Manager.Instance.AddStones(stones);
            Avatar_Manager.Instance.AddScrolls(scrolls);
            Avatar_Manager.Instance.AddExperience(totalSessionXpGained);
        }

        if (Inventory_Manager.Instance != null)
        {
            Inventory_Manager.Instance.AddFishingSessionLoot(caughtSessionLoot);
            if (currentDifficulty == FishingDifficulty.Hard)
            {
                Inventory_Manager.Instance.AddItem("potion_mastery_100", "Зелье Опыта Мастерства", 1, 100, potion100Sprite, new Color(1f, 0.85f, 0.2f));
            }
        }

        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(false);
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false);
        if (difficultySelectPanel) difficultySelectPanel.SetActive(true);

        Debug.Log("Рыбалка завершена! Улов сохранен со стаками. Открытие квестов поиска предметов.");
    }
}
