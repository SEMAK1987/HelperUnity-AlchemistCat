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
    public GameObject rootFishingGamePanel; // Корневой объект панели рыбалки (AlchemyFishing_Game_Panel)
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
    public TextMeshProUGUI pondCatchGuideTableText; // Текст с подробной таблицей вероятностей (TextMeshPro)
    public UnityEngine.UI.Text pondCatchGuideLegacyTableText; // Запасной компонент текста (Legacy UI Text)

    [Header("=== Итоговое окно попыток (Result_Summary_Popup_Panel) ===")]
    public Transform summaryLootContainer; // Контейнер иконок пойманного лута
    public GameObject summaryItemPrefab; // Префаб ячейки лута в окне итогов
    public TextMeshProUGUI summaryGoldText; // Текст итогового золота
    public TextMeshProUGUI summaryStonesText; // Текст итоговых камней
    public TextMeshProUGUI summaryScrollsText; // Текст итоговых свитков
    public TextMeshProUGUI summaryPotionBonusText; // Текст бонусных зелий
    public Button claimAllToBackpackButton; // Кнопка "Забрать все в рюкзак" (Claim_All_Button)
    public Button claimAndContinueButton; // Кнопка "Забрать улов и продолжить >>" (Claim_And_Continue_Button)

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

        // Автопоиск кнопок завершения в окне итогов
        if (resultSummaryPopupPanel != null)
        {
            if (claimAndContinueButton == null)
            {
                Transform t = resultSummaryPopupPanel.transform.Find("Claim_And_Continue_Button");
                if (t != null) claimAndContinueButton = t.GetComponent<Button>();
            }
            if (claimAllToBackpackButton == null)
            {
                Transform t = resultSummaryPopupPanel.transform.Find("Claim_All_Button");
                if (t != null) claimAllToBackpackButton = t.GetComponent<Button>();
            }
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
        UpdateGuideTableText(); // Предварительное обновление текста таблицы улова
    }

    public void OpenCatchGuide() // Открытие справочника зон и рыб
    {
        UpdateGuideTableText(); // Обновление текста таблицы зон и вероятностей
        if (pondCatchGuidePopup) pondCatchGuidePopup.SetActive(true); // Показ окна справочника
        if (fishRodButton) fishRodButton.interactable = false; // Блокировка удочки при открытом справочнике
        if (actionButton) actionButton.interactable = false; // Блокировка кнопки действия
        if (closeButton) closeButton.interactable = false; // Блокировка основного крестика
    }

    /// <summary>
    /// Генерация и обновление структурированной таблицы вероятностей шкалы поклевки (значок Рыбки)
    /// Без использования эмодзи (для предотвращения появления квадратиков и предупреждений шрифта)
    /// </summary>
    public void UpdateGuideTableText() // Заполнение понятной игрокам таблицы зон улова с поддержкой TMP и Legacy Text
    {
        // Автопоиск текстового компонента, если не привязан в инспекторе
        if (pondCatchGuideTableText == null && pondCatchGuideLegacyTableText == null && pondCatchGuidePopup != null)
        {
            Transform t = pondCatchGuidePopup.transform.Find("Zones_Info_Text"); // Поиск по точному имени из иерархии
            if (t != null) pondCatchGuideTableText = t.GetComponent<TextMeshProUGUI>(); // Получение компонента TMP
            
            if (pondCatchGuideTableText == null)
            {
                pondCatchGuideTableText = pondCatchGuidePopup.GetComponentInChildren<TextMeshProUGUI>(true); // Поиск первого подходящего TMP
            }
            if (pondCatchGuideTableText == null)
            {
                pondCatchGuideLegacyTableText = pondCatchGuidePopup.GetComponentInChildren<UnityEngine.UI.Text>(true); // Поиск Legacy Text
            }
        }

        // Компактный текст без спецсимволов и эмодзи (исключает ошибки шрифта LiberationSans SDF и квадратики)
        string tableContent = 
            "<color=#FFD700><b>ТАБЛИЦА ДОБЫЧИ НА ШКАЛЕ (Все уровни сложности):</b></color>\n" +
            "<color=#FFAA55><b>1. За 2-й полоской (0.75 - 1.00) [Края шкалы]:</b></color>\n" +
            "- Магическое (40%): +300 XP | Легендарное (30%): +500 XP\n" +
            "- Мифическое (20%): +1000 XP | Драконье (10%): +3000 XP\n" +
            "<color=#55CCFF><b>2. От 1-й до 2-й полоски (0.50 - 0.75) [Средняя зона]:</b></color>\n" +
            "- Среднее зелье (50%): +50 XP | Высокое зелье (30%): +100 XP\n" +
            "- Магический Рунный Камень (20%): +50 XP\n" +
            "<color=#77DD77><b>3. За 1-й полоской (0.35 - 0.50) [Переходная зона]:</b></color>\n" +
            "- Малое зелье (50%): +10 XP | Мусор (50%): Тина / Бутылка (+10 / +5 XP)\n" +
            "<color=#E0E0E0><b>4. Центр шкалы (0.00 - 0.35) [0-Позиция]:</b></color>\n" +
            "- Болотная тина (50%): +10 XP | Старая пустая бутылка (50%): +5 XP\n" +
            "<color=#FFFFAA><i>* Весь улов идет в Сундук. Кликните по предмету в инвентаре для начисления XP!</i></color>";

        if (pondCatchGuideTableText != null) // Если назначен TextMeshPro
        {
            pondCatchGuideTableText.enableAutoSizing = true; // Автоматическая подгонка размера шрифта под рамку
            pondCatchGuideTableText.fontSizeMin = 12f; // Минимальный размер шрифта
            pondCatchGuideTableText.fontSizeMax = 19f; // Максимальный размер шрифта
            pondCatchGuideTableText.lineSpacing = -5f; // Плотный межстрочный интервал без пустых пропусков
            pondCatchGuideTableText.text = tableContent; // Установка чистого текста
        }
        else if (pondCatchGuideLegacyTableText != null) // Если назначен Legacy Text
        {
            pondCatchGuideLegacyTableText.text = tableContent; // Установка чистого текста
        }
    }

    public void CloseCatchGuide() // Закрытие справочника
    {
        if (pondCatchGuidePopup) pondCatchGuidePopup.SetActive(false); // Скрытие окна справочника
        if (fishRodButton) fishRodButton.interactable = currentPhase == GamePhase.Idle || currentPhase == GamePhase.VerticalCasting || currentPhase == GamePhase.HorizontalCatching; // Восстановление кликабельности
        if (actionButton) actionButton.interactable = fishRodButton.interactable;
        if (closeButton) closeButton.interactable = true; // Разблокировка основного крестика
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

    private IEnumerator ProcessCatchResult(float vVal, float hVal) // Корутина расчета выловленного предмета по горизонтальной шкале подсечки
    {
        yield return new WaitForSeconds(0.8f); // Имитация времени выуживания

        // Точное определение сектора по шкале подсечки (hVal: 0.00 .. 1.00) в соответствии с разметкой
        // 0 - Позиция 0 (Центр до риски 1: 0.00 .. 0.35)
        // 1 - За полоской 1 (0.35 .. 0.50)
        // 2 - От 1 полоски до 2 полоски (0.50 .. 0.75)
        // 3 - За 2 полоской (0.75 .. 1.00)
        float hSpread = Mathf.Clamp01(hVal); // Значение горизонтальной подсечки (0..1)
        float roll = Random.value; // Случайное число для вероятностей внутри выбранного сектора

        LootResult result = new LootResult(); // Объект результата вылова

        // 🌟 1. ЗА 2 ПОЛОСКОЙ (0.75 .. 1.00):
        // Магическое (+300 XP) - 40%, Легендарное (+500 XP) - 30%, Мифическое (+1000 XP) - 20%, Драконье (+3000 XP) - 10%
        if (hSpread >= 0.75f)
        {
            if (roll < 0.10f) // 10% вероятность
            {
                result.itemId = "potion_3000"; // ID предмета
                result.itemName = "Драконье Зелье Опыта"; // Имя предмета
                result.xp = 3000; // Опыт предмета
                result.sprite = potion3000Sprite != null ? potion3000Sprite : potion1000Sprite; // Спрайт
                result.rarityColor = new Color(1f, 0.4f, 0f); // Оранжево-красный цвет редкости
            }
            else if (roll < 0.30f) // 20% вероятность (0.10 .. 0.30)
            {
                result.itemId = "potion_1000"; // ID предмета
                result.itemName = "Мифическое Зелье Опыта"; // Имя предмета
                result.xp = 1000; // Опыт предмета
                result.sprite = potion1000Sprite != null ? potion1000Sprite : potion500Sprite; // Спрайт
                result.rarityColor = new Color(0.7f, 0.3f, 1f); // Пурпурный цвет редкости
            }
            else if (roll < 0.60f) // 30% вероятность (0.30 .. 0.60)
            {
                result.itemId = "potion_500"; // ID предмета
                result.itemName = "Легендарное Зелье Опыта"; // Имя предмета
                result.xp = 500; // Опыт предмета
                result.sprite = potion500Sprite != null ? potion500Sprite : potion300Sprite; // Спрайт
                result.rarityColor = new Color(1f, 0.85f, 0.2f); // Золотой цвет редкости
            }
            else // 40% вероятность (0.60 .. 1.00)
            {
                result.itemId = "potion_300"; // ID предмета
                result.itemName = "Магическое Зелье Опыта"; // Имя предмета
                result.xp = 300; // Опыт предмета
                result.sprite = potion300Sprite != null ? potion300Sprite : potion100Sprite; // Спрайт
                result.rarityColor = new Color(0.9f, 0.2f, 0.3f); // Рубиновый цвет редкости
            }
        }
        // 🧪 2. ОТ 1 ПОЛОСКИ ДО 2 ПОЛОСКИ (0.50 .. 0.75):
        // Магический Рунный Камень (+50 XP) - 20%, Среднее Зелье Опыта (+50 XP) - 50%, Высокое Зелье Опыта (+100 XP) - 30%
        else if (hSpread >= 0.50f)
        {
            if (roll < 0.20f) // 20% вероятность
            {
                result.itemId = "rune_stone"; // ID предмета
                result.itemName = "Магический Рунный Камень"; // Имя предмета
                result.xp = 50; // Опыт предмета
                result.sprite = runeStoneSprite; // Спрайт рунного камня
                result.rarityColor = new Color(0.4f, 0.9f, 0.9f); // Бирюзовый цвет редкости
            }
            else if (roll < 0.70f) // 50% вероятность (0.20 .. 0.70)
            {
                result.itemId = "potion_50"; // ID предмета
                result.itemName = "Среднее Зелье Опыта"; // Имя предмета
                result.xp = 50; // Опыт предмета
                result.sprite = potion50Sprite; // Спрайт зелья 50 XP
                result.rarityColor = new Color(0.2f, 0.6f, 1f); // Синий цвет редкости
            }
            else // 30% вероятность (0.70 .. 1.00)
            {
                result.itemId = "potion_100"; // ID предмета
                result.itemName = "Высокое Зелье Опыта"; // Имя предмета
                result.xp = 100; // Опыт предмета
                result.sprite = potion100Sprite; // Спрайт зелья 100 XP
                result.rarityColor = new Color(0.6f, 0.3f, 0.9f); // Фиолетовый цвет редкости
            }
        }
        // 💧 3. ЗА ПОЛОСКОЙ 1 (0.35 .. 0.50):
        // 50% вероятность: Болотная тина (+10 XP) или Старая пустая бутылка (+5 XP)
        // 50% вероятность: Малое Зелье Опыта (+10 XP)
        else if (hSpread >= 0.35f)
        {
            if (roll < 0.50f) // 50% шанс на мусор
            {
                if (roll < 0.25f) // 25% Болотная тина (+10 XP)
                {
                    result.itemId = "duckweed"; // ID предмета
                    result.itemName = "Болотная тина"; // Имя предмета
                    result.xp = 10; // Нажатие на тину дает 10 XP игрока
                    result.sprite = duckweedSprite; // Спрайт тины
                    result.rarityColor = new Color(0.4f, 0.75f, 0.3f); // Травяной цвет
                }
                else // 25% Старая бутылка (+5 XP)
                {
                    result.itemId = "trash_bottle"; // ID предмета
                    result.itemName = "Старая бутылка"; // Имя предмета
                    result.xp = 5; // Нажатие на бутылку дает 5 XP игрока
                    result.sprite = trashBottleSprite; // Спрайт бутылки
                    result.rarityColor = Color.gray; // Серый цвет
                }
            }
            else // 50% шанс на Малое Зелье Опыта (+10 XP)
            {
                result.itemId = "potion_10"; // ID предмета
                result.itemName = "Малое Зелье Опыта"; // Имя предмета
                result.xp = 10; // Опыт предмета
                result.sprite = potion10Sprite != null ? potion10Sprite : potion50Sprite; // Спрайт
                result.rarityColor = new Color(0.3f, 0.7f, 0.4f); // Зеленый цвет редкости
            }
        }
        // 🪵 4. В 0 ПОЗИЦИИ (Центр до риски 1: 0.00 .. 0.35):
        // 100% самое плохое: 50% Болотная тина (+10 XP) и 50% Старая пустая бутылка (+5 XP)
        else
        {
            if (roll < 0.50f) // 50% вероятность
            {
                result.itemId = "duckweed"; // ID предмета
                result.itemName = "Болотная тина"; // Имя предмета
                result.xp = 10; // Нажатие на тину дает 10 XP игрока
                result.sprite = duckweedSprite; // Спрайт тины
                result.rarityColor = new Color(0.4f, 0.75f, 0.3f); // Травяной цвет
            }
            else // 50% вероятность
            {
                result.itemId = "trash_bottle"; // ID предмета
                result.itemName = "Старая бутылка"; // Имя предмета
                result.xp = 5; // Нажатие на пустую бутылку дает 5 XP игрока
                result.sprite = trashBottleSprite; // Спрайт пустой бутылки
                result.rarityColor = Color.gray; // Серый цвет
            }
        }

        Debug.Log($"[РЫБАЛКА] Шкала hSpread={hSpread:F2} (vVal={vVal:F2}) -> Выловлено: {result.itemName} (+{result.xp} XP)"); // Логирование улова

        caughtSessionLoot.Add(result); // Добавление в улов сессии
        totalSessionXpGained += result.xp; // Прибавление опыта

        UpdateSideLootUI(); // Обновление боковой панели целей/улова

        // Показ всплывающего окна улова на 1.5 секунды
        yield return StartCoroutine(ShowSingleCatchToastCoroutine(result));

        int maxAttempts = GetMaxAttemptsForDifficulty(currentDifficulty);
        if (caughtSessionLoot.Count >= maxAttempts) // Если выловлено максимальное количество предметов
        {
            ShowSummaryPopup(); // Показ окна итогов
        }
        else
        {
            currentAttempt = caughtSessionLoot.Count + 1; // Синхронизация текущей попытки строго со списком улова
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
            GameObject cell = Instantiate(sideLootItemPrefab, sideLootContainer); // Создание ячейки
            RectTransform cellRect = cell.GetComponent<RectTransform>(); // RectTransform ячейки
            if (cellRect != null)
            {
                cellRect.localScale = Vector3.one; // Нормализация масштаба
            }

            // Поиск или настройка иконки предмета
            Image img = cell.GetComponentInChildren<Image>();
            if (img != null)
            {
                if (pair.loot.sprite != null)
                {
                    img.sprite = pair.loot.sprite; // Назначение спрайта предмета
                    img.color = Color.white; // Белый цвет
                    img.preserveAspect = true; // Сохранение пропорций без искажений
                }
                
                // Ограничиваем размер иконки, чтобы она идеально вписывалась внутрь рамки
                RectTransform imgRect = img.rectTransform;
                if (imgRect != null)
                {
                    imgRect.anchorMin = new Vector2(0.5f, 0.5f);
                    imgRect.anchorMax = new Vector2(0.5f, 0.5f);
                    imgRect.pivot = new Vector2(0.5f, 0.5f);
                    imgRect.sizeDelta = new Vector2(100f, 100f); // Аккуратный размер иконки внутри золотой рамки
                    imgRect.localScale = Vector3.one;
                }
            }

            // Настройка текста количества "x2", "x4"
            TextMeshProUGUI txt = cell.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = $"x{pair.count}"; // Запись количества
                txt.fontSize = 28f; // Четкий читаемый размер шрифта
                txt.enableAutoSizing = false;
                RectTransform txtRect = txt.rectTransform;
                if (txtRect != null)
                {
                    txtRect.anchorMin = new Vector2(1f, 0.5f);
                    txtRect.anchorMax = new Vector2(1f, 0.5f);
                    txtRect.pivot = new Vector2(0f, 0.5f);
                    txtRect.anchoredPosition = new Vector2(10f, 0f); // Размещение бейджа справа от иконки
                }
            }
        }
    }

    private void ShowSummaryPopup() // Показ финального окна итогов с кнопкой продолжения
    {
        currentPhase = GamePhase.Finished; // Установка фазы завершения
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(true); // Активация окна итогов

        int gold = currentDifficulty == FishingDifficulty.Easy ? 3000 : currentDifficulty == FishingDifficulty.Medium ? 5000 : 10000; // Награда золотом
        int stones = currentDifficulty == FishingDifficulty.Easy ? 3 : currentDifficulty == FishingDifficulty.Medium ? 5 : 10; // Награда камнями
        int scrolls = currentDifficulty == FishingDifficulty.Easy ? 1 : currentDifficulty == FishingDifficulty.Medium ? 2 : 5; // Награда свитками

        if (summaryGoldText) summaryGoldText.text = $"+{gold:N0} Золота"; // Заполнение текста золота
        if (summaryStonesText) summaryStonesText.text = $"+{stones} Камней"; // Заполнение текста камней
        if (summaryScrollsText) summaryScrollsText.text = $"+{scrolls} Свитков"; // Заполнение текста свитков
        if (summaryPotionBonusText) // Бонусное зелье
        {
            summaryPotionBonusText.gameObject.SetActive(currentDifficulty == FishingDifficulty.Hard); // Показ только на сложном
            summaryPotionBonusText.text = "1 шт Зелье Опыта Мастерства (+100 XP)"; // Текст бонуса
        }

        // Автоматический поиск, создание и привязка кнопки "Продолжить" в окне итогов
        SetupSummaryContinueButton(); // Гарантированное отображение кнопки перехода к диалогу
    }

    /// <summary>
    /// Автоматическая настройка кнопки "Забрать улов и продолжить диалог с Котом"
    /// </summary>
    public void SetupSummaryContinueButton() // Настройка кнопки продолжения в окне итогов
    {
        if (resultSummaryPopupPanel == null) return; // Проверка наличия панели итогов

        // 1. Поиск кнопки Claim_And_Continue_Button, если не назначена вручную
        if (claimAndContinueButton == null)
        {
            Transform t = resultSummaryPopupPanel.transform.Find("Claim_And_Continue_Button");
            if (t != null) claimAndContinueButton = t.GetComponent<Button>();
        }

        // 2. Поиск стандартной кнопки Claim_All_Button, если не назначена вручную
        if (claimAllToBackpackButton == null)
        {
            Transform t = resultSummaryPopupPanel.transform.Find("Claim_All_Button");
            if (t != null) claimAllToBackpackButton = t.GetComponent<Button>();
        }

        // 3. Если назначена или найдена специальная кнопка Claim_And_Continue_Button
        if (claimAndContinueButton != null)
        {
            claimAndContinueButton.gameObject.SetActive(true); // Активация кнопки
            claimAndContinueButton.interactable = true; // Разрешение взаимодействия
            claimAndContinueButton.transform.SetAsLastSibling(); // Вынос поверх всех слоев
            claimAndContinueButton.onClick.RemoveAllListeners(); // Очистка старых событий
            claimAndContinueButton.onClick.AddListener(ClaimAllAndProceedToQuest); // Привязка завершения рыбалки и диалога

            // Скрываем старую кнопку Claim_All_Button во избежание наложения
            if (claimAllToBackpackButton != null && claimAllToBackpackButton != claimAndContinueButton)
            {
                claimAllToBackpackButton.gameObject.SetActive(false); // Скрытие дублирующей старой кнопки
            }
        }
        else if (claimAllToBackpackButton != null) // 4. Иначе используем стандартную кнопку
        {
            claimAllToBackpackButton.gameObject.SetActive(true); // Активация кнопки
            claimAllToBackpackButton.interactable = true; // Разрешение взаимодействия
            claimAllToBackpackButton.transform.SetAsLastSibling(); // Размещение поверх всех слоев
            claimAllToBackpackButton.onClick.RemoveAllListeners(); // Очистка старых подписчиков
            claimAllToBackpackButton.onClick.AddListener(ClaimAllAndProceedToQuest); // Привязка перехода к квесту и диалогу

            // Обновление надписи на кнопке
            TextMeshProUGUI label = claimAllToBackpackButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "<b>Забрать улов и продолжить >></b>"; // Русская надпись
                label.color = new Color(0.12f, 0.08f, 0.02f, 1f); // Четкий контраст
            }
        }
        else // 5. Если кнопки нет вовсе — динамически создаем стильную золотую кнопку
        {
            GameObject btnObj = new GameObject("Claim_And_Continue_Dialogue_Button"); // Создание объекта кнопки
            btnObj.transform.SetParent(resultSummaryPopupPanel.transform, false); // Помещение в панель итогов

            RectTransform rect = btnObj.AddComponent<RectTransform>(); // Добавление RectTransform
            rect.anchorMin = new Vector2(0.5f, 0.5f); // Центровка минимального якоря
            rect.anchorMax = new Vector2(0.5f, 0.5f); // Центровка максимального якоря
            rect.pivot = new Vector2(0.5f, 0.5f); // Центровка точки привязки
            rect.anchoredPosition = new Vector2(0f, -170f); // Позиция внизу золотой рамки
            rect.sizeDelta = new Vector2(440f, 64f); // Размер кнопки

            Image btnImage = btnObj.AddComponent<Image>(); // Добавление фона кнопки
            btnImage.color = new Color(0.95f, 0.70f, 0.15f, 1f); // Золотисто-янтарный цвет

            claimAndContinueButton = btnObj.AddComponent<Button>(); // Добавление компонента Button
            ColorBlock cb = claimAndContinueButton.colors; // Настройка цветов
            cb.normalColor = new Color(1f, 0.80f, 0.20f, 1f);
            cb.highlightedColor = new Color(1f, 0.92f, 0.45f, 1f);
            cb.pressedColor = new Color(0.80f, 0.60f, 0.10f, 1f);
            claimAndContinueButton.colors = cb;

            GameObject textObj = new GameObject("Text_TMP"); // Создание текста на кнопке
            textObj.transform.SetParent(btnObj.transform, false); // Помещение в кнопку
            RectTransform textRect = textObj.AddComponent<RectTransform>(); // RectTransform текста
            textRect.anchorMin = Vector2.zero; // Растяжение на всю кнопку
            textRect.anchorMax = Vector2.one; // Растяжение на всю ширину и высоту кнопки
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>(); // Добавление TextMeshPro
            tmp.text = "<b>Забрать улов и продолжить >></b>"; // Текст на кнопке
            tmp.fontSize = 24f; // Размер шрифта
            tmp.alignment = TextAlignmentOptions.Center; // Выравнивание по центру
            tmp.color = new Color(0.12f, 0.08f, 0.02f, 1f); // Темно-коричневый читаемый цвет

            claimAndContinueButton.onClick.AddListener(ClaimAllAndProceedToQuest); // Привязка действия
        }

        // 6. Защита от случайного закрытия по клику мимо кнопки: отключаем кликабельность фона панели
        Button panelBgButton = resultSummaryPopupPanel.GetComponent<Button>(); // Проверка компонента Button на самом фоне
        if (panelBgButton != null)
        {
            panelBgButton.onClick.RemoveAllListeners(); // Очистка любых случайных слушателей на фоне
            panelBgButton.interactable = false; // Отключение фона, чтобы работала ТОЛЬКО сама кнопка
        }
    }

    [Header("=== Переход к следующим мини-играм и диалогам ===")]
    public GameObject hiddenObjectGamePanel; // Ссылка на панель поиска предметов (HiddenObject_Game_Panel)
    public GameObject dialogueContainer; // Панель диалога с Котом
    public TextMeshProUGUI dialogueText; // Текст внутри диалога с Котом

    public void ClaimAllAndProceedToQuest() // Забрать все награды в инвентарь и запустить Поиск предметов
    {
        int gold = currentDifficulty == FishingDifficulty.Easy ? 3000 : currentDifficulty == FishingDifficulty.Medium ? 5000 : 10000; // Расчет золота
        int stones = currentDifficulty == FishingDifficulty.Easy ? 3 : currentDifficulty == FishingDifficulty.Medium ? 5 : 10; // Расчет камней
        int scrolls = currentDifficulty == FishingDifficulty.Easy ? 1 : currentDifficulty == FishingDifficulty.Medium ? 2 : 5; // Расчет свитков

        if (Avatar_Manager.Instance != null) // Если менеджер аватара активен
        {
            Avatar_Manager.Instance.AddGold(gold); // Начисление золота
            Avatar_Manager.Instance.AddStones(stones); // Начисление камней
            Avatar_Manager.Instance.AddScrolls(scrolls); // Начисление свитков
            Avatar_Manager.Instance.AddExperience(totalSessionXpGained); // Начисление опыта
        }

        if (Inventory_Manager.Instance != null) // Если инвентарь активен
        {
            Inventory_Manager.Instance.AddFishingSessionLoot(caughtSessionLoot); // Сохранение улова
            if (currentDifficulty == FishingDifficulty.Hard) // Бонус сложного уровня
            {
                Inventory_Manager.Instance.AddItem("potion_mastery_100", "Зелье Опыта Мастерства", 1, 100, potion100Sprite, new Color(1f, 0.85f, 0.2f)); // Выдача зелья
            }
        }

        PlayerPrefs.SetInt("Minigame_Fishing_Completed", 1); // Запись факта завершения рыбалки
        PlayerPrefs.Save(); // Сохранение на диск

        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(false); // Скрытие игровой сцены
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false); // Скрытие окна итогов
        if (rootFishingGamePanel != null) rootFishingGamePanel.SetActive(false); // Скрытие всей панели рыбалки
        else gameObject.SetActive(false); // Запасное скрытие текущего объекта

        // Восстановление HUD верхнего интерфейса
        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление аватарки и кнопок
        }

        // Получение сохраненного имени игрока
        string playerName = PlayerPrefs.GetString("PlayerName", PlayerPrefs.GetString("Player_Name", "Алхимик")); // Чтение имени игрока

        // Запуск централизованного диалога с Котом и подготовка перехода в Поиск Предметов через DialogueSystem_Manager
        if (DialogueSystem_Manager.Instance != null) // Если центральный менеджер диалогов доступен
        {
            DialogueSystem_Manager.Instance.StartPostFishingDialogue(); // Запуск сюжетной линии и анонса Поиска Предметов
        }
        else if (dialogueContainer != null && dialogueText != null) // Фоллбэк: если диалог подключен локально
        {
            dialogueContainer.SetActive(true); // Включение локального диалога
            dialogueText.text = $"Мурр! Превосходный улов, {playerName}! Твоя ловкость восхитительна!\n\nА теперь заглянем в «Поиск Предметов»! Тебя ждут 3 волшебные комнаты!\nВыбирай сложность, находи спрятанные ингредиенты и спасай зелья!"; // Реплика Кота
            if (hiddenObjectGamePanel != null) hiddenObjectGamePanel.SetActive(true); // Включение панели
        }

        Debug.Log($"[РЫБАЛКА ЗАВЕРШЕНА] Улов отправлен в сундук для {playerName}. Запущен диалог Кота перед Поиском Предметов."); // Лог завершения
    }
}
