using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum FishingDifficulty
{
    Easy,   // +3000 Золота, 3 Камня, 1 Свиток (Зона 4: 35%, Скорость x1.0)
    Medium, // +5000 Золота, 5 Камней, 2 Свитка (Зона 4: 22%, Скорость x1.4)
    Hard    // +10000 Золота, 10 Камней, 5 Свитков, 1 Зелье Мастерства (+100 XP) (Зона 4: 12%, Скорость x1.9)
}

/// <summary>
/// Алхимическая Рыбалка: 3 уровня сложности, 10 попыток за сессию,
/// последовательное появление 2 шкал (вертикальная с 4 секторами -> горизонтальная с расходящимися лучами),
/// блокировка удочки, подсчет улова и начисление наград в инвентарь.
/// </summary>
public class AlchemyFishing_Minigame : MonoBehaviour
{
    public static AlchemyFishing_Minigame Instance;

    [Header("=== Главные панели ===")]
    public GameObject difficultySelectPanel;
    public GameObject activeFishingStagePanel;
    public GameObject resultSummaryPopupPanel;
    public Button closeButton; // Крестик сверху слева (возврат в меню выбора)

    [Header("=== Кнопки выбора сложности ===")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    [Header("=== Счетчик попыток и ресурсов ===")]
    public TextMeshProUGUI attemptsCounterText; // "Попытка: 1 / 10"
    public TextMeshProUGUI totalSessionXpText;
    private int currentAttempt = 1;
    private const int MAX_ATTEMPTS = 10;
    private FishingDifficulty currentDifficulty = FishingDifficulty.Medium;

    [Header("=== Удочка (FishRod_Visual в правом нижнем углу) ===")]
    public Button fishRodButton;
    public RectTransform fishRodTransform;
    public Image fishRodImage;
    public RectTransform bobberTransform;

    [Header("=== Вертикальная шкала заброса (Шкала 1: 4 Сектора и 3 разделителя) ===")]
    public GameObject verticalBarContainer;
    public RectTransform verticalBarBg;
    public RectTransform verticalSliderArrow;
    public RectTransform delimiterZone4; // Линия границы сектора 4 (динамическая высота)
    public RectTransform delimiterZone3; // Линия границы сектора 3
    public RectTransform delimiterZone2; // Линия границы сектора 2
    public float baseVerticalSpeed = 3.5f;

    [Header("=== Горизонтальная шкала поклевки (Шкала 2: 2 расходящихся луча) ===")]
    public GameObject horizontalBarContainer;
    public RectTransform horizontalBarBg;
    public RectTransform leftMovingBeam;  // Луч от центра к левому краю
    public RectTransform rightMovingBeam; // Луч от центра к правому краю
    public float baseHorizontalSpeed = 4.0f;

    [Header("=== Кнопка действия ===")]
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    [Header("=== Итоговое окно 10 попыток (Result_Summary_Popup_Panel) ===")]
    public Transform summaryLootContainer;
    public GameObject summaryItemPrefab;
    public TextMeshProUGUI summaryGoldText;
    public TextMeshProUGUI summaryStonesText;
    public TextMeshProUGUI summaryScrollsText;
    public TextMeshProUGUI summaryPotionBonusText;
    public Button claimAllToBackpackButton;

    [Header("=== Спрайты наград ===")]
    public Sprite trashBottleSprite;
    public Sprite duckweedSprite;
    public Sprite runeStoneSprite;
    public Sprite potion10Sprite;
    public Sprite potion50Sprite;
    public Sprite potion100Sprite;
    public Sprite potion300Sprite;
    public Sprite potion500Sprite;
    public Sprite potion1000Sprite;
    public Sprite potion3000Sprite; // Драконье зелье

    // Внутренние состояния
    public enum GamePhase { Idle, VerticalCasting, HorizontalCatching, Splashing, SingleResultToast, Finished }
    private GamePhase currentPhase = GamePhase.Idle;

    private float verticalValue = 0.5f;
    private float horizontalSpread = 0f;
    private int verticalDirection = 1;
    private int horizontalDirection = 1;

    private float lockedVertical = 0f;
    private float lockedHorizontal = 0f;

    private List<LootResult> caughtSessionLoot = new List<LootResult>();
    private int totalSessionXpGained = 0;

    [System.Serializable]
    public struct LootResult
    {
        public string itemId;
        public string itemName;
        public int xp;
        public Sprite sprite;
        public Color rarityColor;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (easyButton) easyButton.onClick.AddListener(() => StartFishingSession(FishingDifficulty.Easy));
        if (mediumButton) mediumButton.onClick.AddListener(() => StartFishingSession(FishingDifficulty.Medium));
        if (hardButton) hardButton.onClick.AddListener(() => StartFishingSession(FishingDifficulty.Hard));

        if (closeButton) closeButton.onClick.AddListener(HandleCloseClicked);
        if (fishRodButton) fishRodButton.onClick.AddListener(OnRodOrActionButtonClicked);
        if (actionButton) actionButton.onClick.AddListener(OnRodOrActionButtonClicked);
        if (claimAllToBackpackButton) claimAllToBackpackButton.onClick.AddListener(ClaimAllAndProceedToQuest);

        ShowDifficultySelection();
    }

    public void ShowDifficultySelection()
    {
        if (difficultySelectPanel) difficultySelectPanel.SetActive(true);
        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(false);
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false);
    }

    public void StartFishingSession(FishingDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        currentAttempt = 1;
        caughtSessionLoot.Clear();
        totalSessionXpGained = 0;

        if (difficultySelectPanel) difficultySelectPanel.SetActive(false);
        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(true);
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false);

        // Настройка делителей зон в зависимости от сложности
        ConfigureDifficultySettings();
        ResetAttemptToIdle();
    }

    private void ConfigureDifficultySettings()
    {
        // Зона 4: Легкий = 35% высоты, Средний = 22%, Сложный = 12%
        float z4Height = currentDifficulty == FishingDifficulty.Easy ? 0.35f :
                         currentDifficulty == FishingDifficulty.Medium ? 0.22f : 0.12f;

        if (delimiterZone4 && verticalBarBg)
        {
            float totalH = verticalBarBg.rect.height;
            delimiterZone4.anchoredPosition = new Vector2(0, totalH * (1f - z4Height));
        }
    }

    private void ResetAttemptToIdle()
    {
        currentPhase = GamePhase.Idle;
        if (attemptsCounterText) attemptsCounterText.text = $"Попытка: {currentAttempt} / {MAX_ATTEMPTS}";
        if (totalSessionXpText) totalSessionXpText.text = $"+{totalSessionXpGained} XP";

        if (verticalBarContainer) verticalBarContainer.SetActive(false);
        if (horizontalBarContainer) horizontalBarContainer.SetActive(false);
        if (bobberTransform) bobberTransform.gameObject.SetActive(false);

        // Разблокировка удочки
        if (fishRodButton) fishRodButton.interactable = true;
        if (fishRodImage) fishRodImage.color = Color.white;
        if (actionButtonText) actionButtonText.text = "ЗАБРОСИТЬ УДОЧКУ!";
    }

    public void OnRodOrActionButtonClicked()
    {
        switch (currentPhase)
        {
            case GamePhase.Idle:
                // 1. Клик по удочке: удочка блокируется, появляется вертикальная шкала 1
                currentPhase = GamePhase.VerticalCasting;
                if (fishRodButton) fishRodButton.interactable = false;
                if (fishRodImage) fishRodImage.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                if (verticalBarContainer) verticalBarContainer.SetActive(true);
                if (horizontalBarContainer) horizontalBarContainer.SetActive(false);
                if (actionButtonText) actionButtonText.text = "ОСТАНОВИТЬ ДАЛЬНОСТЬ (КЛИК)!";
                break;

            case GamePhase.VerticalCasting:
                // 2. Остановка шкалы 1: фиксируем дальность, прячем шкалу 1, запускаем шкалу 2
                lockedVertical = verticalValue;
                currentPhase = GamePhase.HorizontalCatching;
                if (verticalBarContainer) verticalBarContainer.SetActive(false);
                if (horizontalBarContainer) horizontalBarContainer.SetActive(true);
                if (actionButtonText) actionButtonText.text = "ПОДСЕЧЬ НА КРАЯХ (КЛИК)!";

                // Анимация заброса удочки и полет поплавка
                StartCoroutine(AnimateRodCast(lockedVertical));
                break;

            case GamePhase.HorizontalCatching:
                // 3. Остановка шкалы 2: подсекаем поплавок когда лучи на краях
                lockedHorizontal = horizontalSpread;
                currentPhase = GamePhase.Splashing;
                if (horizontalBarContainer) horizontalBarContainer.SetActive(false);
                if (actionButtonText) actionButtonText.text = "ТЯНЕМ УЛОВ... 🌊";

                StartCoroutine(ProcessCatchResult(lockedVertical, lockedHorizontal));
                break;
        }
    }

    private void Update()
    {
        float speedMultiplier = currentDifficulty == FishingDifficulty.Easy ? 1.0f :
                                currentDifficulty == FishingDifficulty.Medium ? 1.4f : 1.9f;

        if (currentPhase == GamePhase.VerticalCasting)
        {
            // Движение стрелки по вертикали 0..1
            verticalValue += verticalDirection * baseVerticalSpeed * speedMultiplier * Time.deltaTime;
            if (verticalValue >= 1f) { verticalValue = 1f; verticalDirection = -1; }
            else if (verticalValue <= 0f) { verticalValue = 0f; verticalDirection = 1; }

            if (verticalSliderArrow && verticalBarBg)
            {
                float totalH = verticalBarBg.rect.height;
                verticalSliderArrow.anchoredPosition = new Vector2(verticalSliderArrow.anchoredPosition.x, verticalValue * totalH);
            }
        }
        else if (currentPhase == GamePhase.HorizontalCatching)
        {
            // 2 луча расходятся от центра (0) к краям (1) и обратно
            horizontalSpread += horizontalDirection * baseHorizontalSpeed * speedMultiplier * Time.deltaTime;
            if (horizontalSpread >= 1f) { horizontalSpread = 1f; horizontalDirection = -1; }
            else if (horizontalSpread <= 0f) { horizontalSpread = 0f; horizontalDirection = 1; }

            if (horizontalBarBg)
            {
                float halfW = horizontalBarBg.rect.width * 0.5f;
                if (leftMovingBeam) leftMovingBeam.anchoredPosition = new Vector2(-horizontalSpread * halfW, 0);
                if (rightMovingBeam) rightMovingBeam.anchoredPosition = new Vector2(horizontalSpread * halfW, 0);
            }
        }
    }

    private IEnumerator AnimateRodCast(float power)
    {
        if (fishRodTransform)
        {
            // Наклон удочки при замахе
            fishRodTransform.localRotation = Quaternion.Euler(0, 0, -20f);
            yield return new WaitForSeconds(0.2f);
            fishRodTransform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        if (bobberTransform)
        {
            bobberTransform.gameObject.SetActive(true);
            bobberTransform.anchoredPosition = new Vector2(0, -50f + power * 120f);
        }
    }

    private IEnumerator ProcessCatchResult(float vVal, float hVal)
    {
        yield return new WaitForSeconds(0.8f);

        // Определение сектора заброса
        float z4Threshold = 1f - (currentDifficulty == FishingDifficulty.Easy ? 0.35f :
                                  currentDifficulty == FishingDifficulty.Medium ? 0.22f : 0.12f);
        int sector = vVal >= z4Threshold ? 4 : vVal >= 0.50f ? 3 : vVal >= 0.25f ? 2 : 1;

        // Точность по горизонтали (чем ближе лучи к краям 1.0, тем выше точность)
        float edgeAccuracy = hVal;
        float roll = Random.value;

        LootResult result = new LootResult();

        if (sector == 4 && edgeAccuracy > 0.75f)
        {
            if (roll < 0.08f) { result.itemId = "potion_3000"; result.itemName = "Драконье Зелье Опыта"; result.xp = 3000; result.sprite = potion3000Sprite; result.rarityColor = new Color(1f, 0.4f, 0f); }
            else if (roll < 0.25f) { result.itemId = "potion_1000"; result.itemName = "Мифическое Зелье Опыта"; result.xp = 1000; result.sprite = potion1000Sprite; result.rarityColor = new Color(0.7f, 0.3f, 1f); }
            else if (roll < 0.60f) { result.itemId = "potion_500"; result.itemName = "Легендарное Зелье Опыта"; result.xp = 500; result.sprite = potion500Sprite; result.rarityColor = new Color(1f, 0.85f, 0.2f); }
            else { result.itemId = "potion_300"; result.itemName = "Магическое Зелье Опыта"; result.xp = 300; result.sprite = potion300Sprite; result.rarityColor = new Color(0.9f, 0.2f, 0.3f); }
        }
        else if (sector >= 3)
        {
            if (roll < 0.30f) { result.itemId = "potion_100"; result.itemName = "Высокое Зелье Опыта"; result.xp = 100; result.sprite = potion100Sprite; result.rarityColor = new Color(0.6f, 0.3f, 0.9f); }
            else if (roll < 0.70f) { result.itemId = "potion_50"; result.itemName = "Среднее Зелье Опыта"; result.xp = 50; result.sprite = potion50Sprite; result.rarityColor = new Color(0.2f, 0.6f, 1f); }
            else { result.itemId = "rune_stone"; result.itemName = "Магический Рунный Камень"; result.xp = 25; result.sprite = runeStoneSprite; result.rarityColor = new Color(0.4f, 0.9f, 0.9f); }
        }
        else
        {
            if (roll < 0.5f) { result.itemId = "duckweed"; result.itemName = "Болотная тина"; result.xp = 0; result.sprite = duckweedSprite; result.rarityColor = Color.gray; }
            else { result.itemId = "trash_bottle"; result.itemName = "Старая бутылка"; result.xp = 0; result.sprite = trashBottleSprite; result.rarityColor = Color.gray; }
        }

        caughtSessionLoot.Add(result);
        totalSessionXpGained += result.xp;

        // Завершение попытки
        if (currentAttempt >= MAX_ATTEMPTS)
        {
            ShowSummaryPopup();
        }
        else
        {
            currentAttempt++;
            ResetAttemptToIdle();
        }
    }

    private void ShowSummaryPopup()
    {
        currentPhase = GamePhase.Finished;
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(true);

        // Начисление базовых наград по уровню сложности
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

    public void ClaimAllAndProceedToQuest()
    {
        // 1. Начисление ресурсов и опыта в менеджер профиля
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

        // 2. Добавление всех выловленных зелий в сундук/инвентарь (со стаком одинаковых предметов!)
        if (Inventory_Manager.Instance != null)
        {
            Inventory_Manager.Instance.AddFishingSessionLoot(caughtSessionLoot);
            if (currentDifficulty == FishingDifficulty.Hard)
            {
                Inventory_Manager.Instance.AddItem("potion_mastery_100", "Зелье Опыта Мастерства", 1, 100, potion100Sprite, new Color(1f, 0.85f, 0.2f));
            }
        }

        // Закрываем рыбалку
        if (activeFishingStagePanel) activeFishingStagePanel.SetActive(false);
        if (resultSummaryPopupPanel) resultSummaryPopupPanel.SetActive(false);
        if (difficultySelectPanel) difficultySelectPanel.SetActive(true);

        // Кот начинает диалог про 3 новые локации (Лавка, Старый дом, Рынок)
        Debug.Log("Рыбалка завершена! Улов сложен в сундук инвентаря со стаками одинаковых предметов. Запуск квеста Поиска предметов в 3 локациях.");
    }

    public void HandleCloseClicked()
    {
        ShowDifficultySelection();
    }
}
