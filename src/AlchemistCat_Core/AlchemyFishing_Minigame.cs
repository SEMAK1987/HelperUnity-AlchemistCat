using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum FishingDifficulty
{
    Easy, // Легкий уровень: +3000 Золота, 3 Камня, 1 Свиток (Зона 4: 35%, Скорость x1.0)
    Medium, // Средний уровень: +5000 Золота, 5 Камней, 2 Свитка (Зона 4: 22%, Скорость x1.4)
    Hard // Сложный уровень: +10000 Золота, 10 Камней, 5 Свитков, 1 Зелье Мастерства (+100 XP)
}

/// <summary>
/// Алхимическая Рыбалка: 3 уровня сложности, 10 попыток за сессию,
/// последовательное появление 2 шкал (вертикальная с 4 секторами -> горизонтальная с расходящимися лучами),
/// блокировка удочки, подсчет улова и начисление наград в инвентарь.
/// </summary>
public class AlchemyFishing_Minigame : MonoBehaviour
{
    public static AlchemyFishing_Minigame Instance; // Синглтон мини-игры рыбалки

    [Header("=== Главные панели ===")]
    public GameObject difficultySelectPanel; // Панель выбора сложности (Легко / Средне / Сложно)
    public GameObject activeFishingStagePanel; // Основная игровая панель процесса рыбалки
    public GameObject resultSummaryPopupPanel; // Итоговое окно подсчета улова после 10 попыток
    public Button closeButton; // Кнопка возврата в главное меню / закрытия

    [Header("=== Кнопки выбора сложности ===")]
    public Button easyButton; // Кнопка выбора легкой сложности
    public Button mediumButton; // Кнопка выбора средней сложности
    public Button hardButton; // Кнопка выбора сложной сложности

    [Header("=== Счетчик попыток и ресурсов ===")]
    public TextMeshProUGUI attemptsCounterText; // Текст счетчика попыток ("Попытка: 1 / 10")
    public TextMeshProUGUI totalSessionXpText; // Текст суммарного набранного опыта
    private int currentAttempt = 1; // Текущий номер попытки из 10
    private const int MAX_ATTEMPTS = 10; // Максимальное количество попыток за сессию
    private FishingDifficulty currentDifficulty = FishingDifficulty.Medium; // Текущая выбранная сложность

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
    public RectTransform leftMovingBeam; // Левый луч, движущийся от центра
    public RectTransform rightMovingBeam; // Правый луч, движущийся от центра
    public float baseHorizontalSpeed = 0.55f; // Плавная базовая скорость расхождения лучей от центра к краям

    [Header("=== Кнопка действия ===")]
    public Button actionButton; // Большая кнопка "Подсечь!" / "Тянуть!"
    public TextMeshProUGUI actionButtonText; // Текст на кнопке действия

    [Header("=== Итоговое окно 10 попыток (Result_Summary_Popup_Panel) ===")]
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
    private int totalSessionXpGained = 0; // Суммарно набранный опыт за 10 попыток

    [System.Serializable]
    public struct LootResult
    {
        public string itemId; // Идентификатор предмета
        public string itemName; // Русское название предмета
        public int xp; // Опыт за предмет
        public Sprite sprite; // Спрайт иконки
        public Color rarityColor; // Цвет рамки редкости
    }

    private void Awake() // Инициализация при создании объекта
    {
        Instance = this; // Инициализация синглтона
    }

    private void OnEnable() // При активации панели рыбалки
    {
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

        // Автоматический поиск текста на удочке и бейджа попыток, если поле не перетащено в инспекторе
        if (actionButtonText == null && fishRodButton != null)
        {
            actionButtonText = fishRodButton.GetComponentInChildren<TextMeshProUGUI>(); // Поиск дочернего текста кнопки удочки
        }
        if (attemptsCounterText == null && activeFishingStagePanel != null)
        {
            Transform badge = activeFishingStagePanel.transform.Find("Attempts_Badge"); // Поиск бейджа попыток в иерархии
            if (badge != null) attemptsCounterText = badge.GetComponentInChildren<TextMeshProUGUI>(); // Поиск текста счетчика
        }

        ShowDifficultySelection(); // Открываем меню выбора сложности на старте
    }

    public void ShowDifficultySelection() // Показ экрана выбора уровня сложности
    {
        if (difficultySelectPanel) difficultySelectPanel.SetActive(true); // Включение панели сложности
        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(false); // Выключение игровой панели
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false); // Выключение итогового окна
    }

    public void StartFishingSession(FishingDifficulty difficulty) // Старт сессии рыбалки из 10 попыток
    {
        currentDifficulty = difficulty; // Запоминаем выбранную сложность
        currentAttempt = 1; // Сброс номера попытки на 1
        caughtSessionLoot.Clear(); // Очистка накопленного улова
        totalSessionXpGained = 0; // Сброс опыта сессии

        if (difficultySelectPanel) difficultySelectPanel.SetActive(false); // Прячем выбор сложности
        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(true); // Открываем экран рыбалки
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false); // Прячем результаты

        // Настройка делителей зон в зависимости от сложности
        ConfigureDifficultySettings(); // Конфигурация шкал под уровень
        ResetAttemptToIdle(); // Сброс в режим ожидания заброса
    }

    private void ConfigureDifficultySettings() // Конфигурация высоты зон и разделителей
    {
        // Зона 4: Легкий = 35% высоты, Средний = 22%, Сложный = 12%
        float z4Height = currentDifficulty == FishingDifficulty.Easy ? 0.35f : // 35% для легкого
                         currentDifficulty == FishingDifficulty.Medium ? 0.22f : 0.12f; // 22% для среднего, 12% для сложного

        if (delimiterZone4 && verticalBarBg) // Если разделитель и фон назначены
        {
            float totalH = verticalBarBg.rect.height; // Полная высота шкалы
            delimiterZone4.anchoredPosition = new Vector2(0, totalH * (1f - z4Height)); // Позиционирование 4 зоны
        }
    }

    private void ResetAttemptToIdle() // Сброс состояния попытки в ожидание клика по удочке
    {
        currentPhase = GamePhase.Idle; // Установка состояния ожидания
        if (attemptsCounterText) attemptsCounterText.text = $"Попытка: {currentAttempt} / {MAX_ATTEMPTS}"; // Текст счетчика попыток
        if (totalSessionXpText) totalSessionXpText.text = $"+{totalSessionXpGained} XP"; // Текст набранного опыта

        if (verticalBarContainer) verticalBarContainer.SetActive(false); // Прячем вертикальную шкалу
        if (horizontalBarContainer) horizontalBarContainer.SetActive(false); // Прячем горизонтальную шкалу
        if (bobberTransform) bobberTransform.gameObject.SetActive(false); // Прячем поплавок

        // Разблокировка удочки
        if (fishRodButton) fishRodButton.interactable = true; // Разблокировка кликабельности удочки
        if (fishRodImage) fishRodImage.color = Color.white; // Яркий белый цвет удочки
        if (actionButtonText) actionButtonText.text = "ЗАБРОС!"; // Лаконичный текст заброса на удочке/кнопке
    }

    public void OnRodOrActionButtonClicked() // Обработка клика по удочке или кнопке действия
    {
        switch (currentPhase) // Переключение по фазам
        {
            case GamePhase.Idle: // Фаза 1: запуск заброса
                // 1. Клик по удочке: запуск вертикальной шкалы дальности
                currentPhase = GamePhase.VerticalCasting; // Переход в фазу вертикальной шкалы
                if (fishRodButton) fishRodButton.interactable = true; // Удочка остается активной для нажатия СТОП
                if (fishRodImage) fishRodImage.color = Color.white; // Яркий цвет удочки
                if (verticalBarContainer) verticalBarContainer.SetActive(true); // Включение вертикальной шкалы
                if (horizontalBarContainer) horizontalBarContainer.SetActive(false); // Выключение горизонтальной
                if (actionButtonText) actionButtonText.text = "СТОП!"; // Короткий текст фиксации дальности
                break;

            case GamePhase.VerticalCasting: // Фаза 2: остановка дальности и запуск поклевки
                // 2. Остановка шкалы 1: фиксируем дальность, прячем шкалу 1, запускаем шкалу 2
                lockedVertical = verticalValue; // Фиксация дальности заброса
                currentPhase = GamePhase.HorizontalCatching; // Переход в фазу подсечки
                if (fishRodButton) fishRodButton.interactable = true; // Удочка остается активной для нажатия ПОДСЕЧЬ
                if (fishRodImage) fishRodImage.color = Color.white; // Яркий цвет удочки
                if (verticalBarContainer) verticalBarContainer.SetActive(false); // Прячем вертикальную шкалу
                if (horizontalBarContainer) horizontalBarContainer.SetActive(true); // Показываем горизонтальную
                if (actionButtonText) actionButtonText.text = "ПОДСЕЧЬ!"; // Короткий текст подсечки

                // Анимация заброса удочки и полет поплавка
                StartCoroutine(AnimateRodCast(lockedVertical)); // Запуск анимации взмаха
                break;

            case GamePhase.HorizontalCatching: // Фаза 3: подсечка и выуживание
                // 3. Остановка шкалы 2: подсекаем поплавок когда лучи на краях
                lockedHorizontal = horizontalSpread; // Фиксация горизонтального расхождения
                currentPhase = GamePhase.Splashing; // Фаза анимации всплеска
                if (fishRodButton) fishRodButton.interactable = false; // Блокировка удочки во время вытягивания улова
                if (fishRodImage) fishRodImage.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Легкое затемнение при анимации вытягивания
                if (horizontalBarContainer) horizontalBarContainer.SetActive(false); // Прячем шкалу
                if (actionButtonText) actionButtonText.text = "ТЯНЕМ УЛОВ... 🌊"; // Текст процесса вытягивания

                StartCoroutine(ProcessCatchResult(lockedVertical, lockedHorizontal)); // Расчет улова
                break;
        }
    }

    private void Update() // Покадровое обновление движения бегунков
    {
        float speedMultiplier = currentDifficulty == FishingDifficulty.Easy ? 1.0f : // Множитель легкой скорости
                                currentDifficulty == FishingDifficulty.Medium ? 1.4f : 1.9f; // Множитель средней и сложной скорости

        if (currentPhase == GamePhase.VerticalCasting) // Если активна вертикальная шкала
        {
            // Движение стрелки по вертикали 0..1
            verticalValue += verticalDirection * baseVerticalSpeed * speedMultiplier * Time.deltaTime; // Смещение стрелки
            if (verticalValue >= 1f) { verticalValue = 1f; verticalDirection = -1; } // Отскок от верхнего края
            else if (verticalValue <= 0f) { verticalValue = 0f; verticalDirection = 1; } // Отскок от нижнего края

            if (verticalSliderArrow && verticalBarBg) // Если стрелка и фон заданы
            {
                float totalH = verticalBarBg.rect.height; // Полная высота шкалы
                verticalSliderArrow.anchoredPosition = new Vector2(verticalSliderArrow.anchoredPosition.x, verticalValue * totalH); // Перемещение стрелки
            }
        }
        else if (currentPhase == GamePhase.HorizontalCatching) // Если активна горизонтальная шкала
        {
            // 2 луча расходятся от центра (0) к краям (1) и обратно
            horizontalSpread += horizontalDirection * baseHorizontalSpeed * speedMultiplier * Time.deltaTime; // Расхождение лучей
            if (horizontalSpread >= 1f) { horizontalSpread = 1f; horizontalDirection = -1; } // Отскок от краев к центру
            else if (horizontalSpread <= 0f) { horizontalSpread = 0f; horizontalDirection = 1; } // Отскок от центра к краям

            if (horizontalBarBg) // Фон горизонтальной шкалы
            {
                float halfW = horizontalBarBg.rect.width * 0.5f; // Половина ширины шкалы
                float beamHalfWidth = leftMovingBeam ? leftMovingBeam.rect.width * 0.5f : 60f; // Полуширина спрайта луча
                float maxTravel = Mathf.Max(20f, halfW - beamHalfWidth - 35f); // Ограничение хода строго внутри золотой рамки

                float offset = horizontalSpread * maxTravel; // Текущее смещение от центра к краю
                if (leftMovingBeam) leftMovingBeam.anchoredPosition = new Vector2(-offset, 0); // Левый луч
                if (rightMovingBeam) rightMovingBeam.anchoredPosition = new Vector2(offset, 0); // Правый луч
            }
        }
    }

    private IEnumerator AnimateRodCast(float power) // Корутина анимации взмаха удочки и броска поплавка
    {
        if (fishRodTransform) // Если есть трансформ удочки
        {
            // Наклон удочки при замахе
            fishRodTransform.localRotation = Quaternion.Euler(0, 0, -20f); // Замах назад
            yield return new WaitForSeconds(0.2f); // Пауза
            fishRodTransform.localRotation = Quaternion.Euler(0, 0, 0); // Возврат в исходное положение
        }

        if (bobberTransform) // Если есть поплавок
        {
            bobberTransform.gameObject.SetActive(true); // Включение поплавка
            bobberTransform.anchoredPosition = new Vector2(0, -50f + power * 120f); // Полет поплавка в воду
        }
    }

    private IEnumerator ProcessCatchResult(float vVal, float hVal) // Корутина расчета выловленного предмета
    {
        yield return new WaitForSeconds(0.8f); // Имитация времени выуживания

        // Определение сектора заброса
        float z4Threshold = 1f - (currentDifficulty == FishingDifficulty.Easy ? 0.35f : // Порог 4-й зоны для легкого
                                  currentDifficulty == FishingDifficulty.Medium ? 0.22f : 0.12f); // Порог 4-й зоны для среднего/сложного
        int sector = vVal >= z4Threshold ? 4 : vVal >= 0.50f ? 3 : vVal >= 0.25f ? 2 : 1; // Номер попавшего сектора

        // Точность по горизонтали (чем ближе лучи к краям 1.0, тем выше точность)
        float edgeAccuracy = hVal; // Значение близости к краю
        float roll = Random.value; // Случайное число от 0 до 1

        LootResult result = new LootResult(); // Структура выпавшего предмета

        if (sector == 4 && edgeAccuracy > 0.75f) // Попадание в 4 сектор при высокой точности
        {
            if (roll < 0.08f) { result.itemId = "potion_3000"; result.itemName = "Драконье Зелье Опыта"; result.xp = 3000; result.sprite = potion3000Sprite; result.rarityColor = new Color(1f, 0.4f, 0f); } // Зелье 3000 XP
            else if (roll < 0.25f) { result.itemId = "potion_1000"; result.itemName = "Мифическое Зелье Опыта"; result.xp = 1000; result.sprite = potion1000Sprite; result.rarityColor = new Color(0.7f, 0.3f, 1f); } // Зелье 1000 XP
            else if (roll < 0.60f) { result.itemId = "potion_500"; result.itemName = "Легендарное Зелье Опыта"; result.xp = 500; result.sprite = potion500Sprite; result.rarityColor = new Color(1f, 0.85f, 0.2f); } // Зелье 500 XP
            else { result.itemId = "potion_300"; result.itemName = "Магическое Зелье Опыта"; result.xp = 300; result.sprite = potion300Sprite; result.rarityColor = new Color(0.9f, 0.2f, 0.3f); } // Зелье 300 XP
        }
        else if (sector >= 3) // Попадание в 3 сектор
        {
            if (roll < 0.30f) { result.itemId = "potion_100"; result.itemName = "Высокое Зелье Опыта"; result.xp = 100; result.sprite = potion100Sprite; result.rarityColor = new Color(0.6f, 0.3f, 0.9f); } // Зелье 100 XP
            else if (roll < 0.70f) { result.itemId = "potion_50"; result.itemName = "Среднее Зелье Опыта"; result.xp = 50; result.sprite = potion50Sprite; result.rarityColor = new Color(0.2f, 0.6f, 1f); } // Зелье 50 XP
            else { result.itemId = "rune_stone"; result.itemName = "Магический Рунный Камень"; result.xp = 25; result.sprite = runeStoneSprite; result.rarityColor = new Color(0.4f, 0.9f, 0.9f); } // Рунный камень
        }
        else // 1-й и 2-й секторы (мусор)
        {
            if (roll < 0.5f) { result.itemId = "duckweed"; result.itemName = "Болотная тина"; result.xp = 0; result.sprite = duckweedSprite; result.rarityColor = Color.gray; } // Тина
            else { result.itemId = "trash_bottle"; result.itemName = "Старая бутылка"; result.xp = 0; result.sprite = trashBottleSprite; result.rarityColor = Color.gray; } // Старая бутылка
        }

        caughtSessionLoot.Add(result); // Добавление улова в сессионный список
        totalSessionXpGained += result.xp; // Прибавление набранного опыта

        // Завершение попытки
        if (currentAttempt >= MAX_ATTEMPTS) // Если достигли 10 попыток
        {
            ShowSummaryPopup(); // Показываем окно итогов
        }
        else // Если еще есть попытки
        {
            currentAttempt++; // Переход к следующей попытке
            ResetAttemptToIdle(); // Сброс в режим ожидания
        }
    }

    private void ShowSummaryPopup() // Показ финального окна итогов после 10 попыток
    {
        currentPhase = GamePhase.Finished; // Установка завершенной фазы
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(true); // Включение итогового окна

        // Начисление базовых наград по уровню сложности
        int gold = currentDifficulty == FishingDifficulty.Easy ? 3000 : currentDifficulty == FishingDifficulty.Medium ? 5000 : 10000; // Расчет золота
        int stones = currentDifficulty == FishingDifficulty.Easy ? 3 : currentDifficulty == FishingDifficulty.Medium ? 5 : 10; // Расчет камней
        int scrolls = currentDifficulty == FishingDifficulty.Easy ? 1 : currentDifficulty == FishingDifficulty.Medium ? 2 : 5; // Расчет свитков

        if (summaryGoldText) summaryGoldText.text = $"+{gold:N0} Золота"; // Текст золота
        if (summaryStonesText) summaryStonesText.text = $"+{stones} Камней"; // Текст камней
        if (summaryScrollsText) summaryScrollsText.text = $"+{scrolls} Свитков"; // Текст свитков
        if (summaryPotionBonusText) // Бонусное зелье мастерства
        {
            summaryPotionBonusText.gameObject.SetActive(currentDifficulty == FishingDifficulty.Hard); // Только на сложном
            summaryPotionBonusText.text = "1 шт Зелье Опыта Мастерства (+100 XP)"; // Текст бонуса
        }
    }

    public void ClaimAllAndProceedToQuest() // Забрать все награды в инвентарь и закрыть сессию
    {
        // 1. Начисление ресурсов и опыта в менеджер профиля
        int gold = currentDifficulty == FishingDifficulty.Easy ? 3000 : currentDifficulty == FishingDifficulty.Medium ? 5000 : 10000; // Расчет золота
        int stones = currentDifficulty == FishingDifficulty.Easy ? 3 : currentDifficulty == FishingDifficulty.Medium ? 5 : 10; // Расчет камней
        int scrolls = currentDifficulty == FishingDifficulty.Easy ? 1 : currentDifficulty == FishingDifficulty.Medium ? 2 : 5; // Расчет свитков

        if (Avatar_Manager.Instance != null) // Если менеджер профиля доступен
        {
            Avatar_Manager.Instance.AddGold(gold); // Начисление золота
            Avatar_Manager.Instance.AddStones(stones); // Начисление камней
            Avatar_Manager.Instance.AddScrolls(scrolls); // Начисление свитков
            Avatar_Manager.Instance.AddExperience(totalSessionXpGained); // Начисление суммарного опыта
        }

        // 2. Добавление всех выловленных зелий в сундук/инвентарь (со стаком одинаковых предметов!)
        if (Inventory_Manager.Instance != null) // Если инвентарь доступен
        {
            Inventory_Manager.Instance.AddFishingSessionLoot(caughtSessionLoot); // Передача сессионного лута со стаками
            if (currentDifficulty == FishingDifficulty.Hard) // Бонус сложного уровня
            {
                Inventory_Manager.Instance.AddItem("potion_mastery_100", "Зелье Опыта Мастерства", 1, 100, potion100Sprite, new Color(1f, 0.85f, 0.2f)); // Добавление зелья мастерства
            }
        }

        // Закрываем рыбалку
        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(false); // Скрытие игрового экрана
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false); // Скрытие итогового окна
        if (difficultySelectPanel) difficultySelectPanel.SetActive(true); // Возврат к выбору сложности

        // Кот начинает диалог про 3 новые локации (Лавка, Старый дом, Рынок)
        Debug.Log("Рыбалка завершена! Улов сложен в сундук инвентаря со стаками одинаковых предметов. Запуск квеста Поиска предметов в 3 локациях."); // Лог завершения
    }

    public void HandleCloseClicked() // Обработка нажатия кнопки Закрыть
    {
        gameObject.SetActive(false); // Выключение панели рыбалки
        if (DialogueSystem_Manager.Instance != null) // Если диалоговый менеджер доступен
        {
            DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление UI
        }
    }
}
