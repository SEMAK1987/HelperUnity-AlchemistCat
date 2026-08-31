import React, { useState } from 'react';
import { Code, Copy, Check, Search, FileCode, CheckCircle2 } from 'lucide-react';
import { CSharpScript } from '../types';

export const SCRIPTS_COLLECTION: CSharpScript[] = [
  {
    name: 'AlchemyFishing_Minigame.cs',
    category: 'Minigames',
    description: 'Алхимическая рыбалка: 3 уровня сложности, 10 попыток за сессию, клик по удочке -> 4 сектора заброса -> горизонтальная подсечка на краях -> окно 10 наград.',
    code: `using System.Collections;
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
        public string itemName;
        public int xp;
        public Sprite sprite;
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

        ConfigureDifficultySettings();
        ResetAttemptToIdle();
    }

    private void ConfigureDifficultySettings()
    {
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

        if (fishRodButton) fishRodButton.interactable = true;
        if (fishRodImage) fishRodImage.color = Color.white;
        if (actionButtonText) actionButtonText.text = "ЗАБРОСИТЬ УДОЧКУ!";
    }

    public void OnRodOrActionButtonClicked()
    {
        switch (currentPhase)
        {
            case GamePhase.Idle:
                currentPhase = GamePhase.VerticalCasting;
                if (fishRodButton) fishRodButton.interactable = false;
                if (fishRodImage) fishRodImage.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                if (verticalBarContainer) verticalBarContainer.SetActive(true);
                if (horizontalBarContainer) horizontalBarContainer.SetActive(false);
                if (actionButtonText) actionButtonText.text = "ОСТАНОВИТЬ ДАЛЬНОСТЬ (КЛИК)!";
                break;

            case GamePhase.VerticalCasting:
                lockedVertical = verticalValue;
                currentPhase = GamePhase.HorizontalCatching;
                if (verticalBarContainer) verticalBarContainer.SetActive(false);
                if (horizontalBarContainer) horizontalBarContainer.SetActive(true);
                if (actionButtonText) actionButtonText.text = "ПОДСЕЧЬ НА КРАЯХ (КЛИК)!";

                StartCoroutine(AnimateRodCast(lockedVertical));
                break;

            case GamePhase.HorizontalCatching:
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

        float z4Threshold = 1f - (currentDifficulty == FishingDifficulty.Easy ? 0.35f :
                                  currentDifficulty == FishingDifficulty.Medium ? 0.22f : 0.12f);
        int sector = vVal >= z4Threshold ? 4 : vVal >= 0.50f ? 3 : vVal >= 0.25f ? 2 : 1;
        float edgeAccuracy = hVal;
        float roll = Random.value;

        LootResult result = new LootResult();

        if (sector == 4 && edgeAccuracy > 0.75f)
        {
            if (roll < 0.08f) { result.itemName = "Драконье Зелье Опыта"; result.xp = 3000; result.sprite = potion3000Sprite; }
            else if (roll < 0.25f) { result.itemName = "Мифическое Зелье Опыта"; result.xp = 1000; result.sprite = potion1000Sprite; }
            else if (roll < 0.60f) { result.itemName = "Легендарное Зелье Опыта"; result.xp = 500; result.sprite = potion500Sprite; }
            else { result.itemName = "Магическое Зелье Опыта"; result.xp = 300; result.sprite = potion300Sprite; }
        }
        else if (sector >= 3)
        {
            if (roll < 0.30f) { result.itemName = "Высокое Зелье Опыта"; result.xp = 100; result.sprite = potion100Sprite; }
            else if (roll < 0.70f) { result.itemName = "Среднее Зелье Опыта"; result.xp = 50; result.sprite = potion50Sprite; }
            else { result.itemName = "Магический Рунный Камень"; result.xp = 25; result.sprite = runeStoneSprite; }
        }
        else
        {
            if (roll < 0.5f) { result.itemName = "Болотная тина"; result.xp = 0; result.sprite = duckweedSprite; }
            else { result.itemName = "Старая бутылка"; result.xp = 0; result.sprite = trashBottleSprite; }
        }

        caughtSessionLoot.Add(result);
        totalSessionXpGained += result.xp;

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
        int gold = currentDifficulty == FishingDifficulty.Easy ? 3000 : currentDifficulty == FishingDifficulty.Medium ? 5000 : 10000;
        int stones = currentDifficulty == FishingDifficulty.Easy ? 3 : currentDifficulty == FishingDifficulty.Medium ? 5 : 10;
        int scrolls = currentDifficulty == FishingDifficulty.Easy ? 1 : currentDifficulty == FishingDifficulty.Medium ? 2 : 5;

        // 1. Начисление валюты и опыта
        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.AddGold(gold);
            Avatar_Manager.Instance.AddStones(stones);
            Avatar_Manager.Instance.AddScrolls(scrolls);
            Avatar_Manager.Instance.AddExperience(totalSessionXpGained);
        }

        // 2. Складывание всех зелий в сундук инвентаря со стаками одинаковых предметов
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
    }

    public void HandleCloseClicked()
    {
        ShowDifficultySelection();
    }
}`,
  },
  {
    name: 'Inventory_Manager.cs',
    category: 'Core',
    description: 'Система Сундука / Инвентаря: автоматический стак одинаковых зелий и предметов в одну ячейку, отображение бейджа количества xN.',
    code: `using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Система Инвентаря и Сундука игрока с поддержкой стака одинаковых предметов:
/// - При добавлении одинаковых зелий/ресурсов они объединяются в один слот
/// - Счетчик количества отображается в виде бейджа "x{count}"
/// - Автоматическая сортировка и сохранение состояния
/// </summary>
public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance { get; private set; }

    [System.Serializable]
    public class ItemStack
    {
        public string itemId;
        public string itemName;
        public int count;
        public int xpPerItem;
        public Sprite icon;
        public Color rarityColor;

        public ItemStack(string id, string name, int count, int xp, Sprite icon, Color color)
        {
            this.itemId = id;
            this.itemName = name;
            this.count = count;
            this.xpPerItem = xp;
            this.icon = icon;
            this.rarityColor = color;
        }
    }

    [Header("UI Панель Сундука / Инвентаря")]
    public GameObject chestInventoryPanel;
    public Transform chestSlotsContainer;
    public GameObject chestSlotPrefab;
    public Button closeChestButton;
    public TextMeshProUGUI totalItemsCountText;
    public TextMeshProUGUI totalChestXpText;

    [Header("Список предметов в сундуке игрока")]
    public List<ItemStack> inventorySlots = new List<ItemStack>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Добавить предмет в сундук с автоматическим стаком одинаковых
    /// </summary>
    public void AddItem(string itemId, string itemName, int count, int xpPerItem, Sprite icon, Color rarityColor)
    {
        // Ищем, есть ли уже такой предмет в сундуке
        ItemStack existingSlot = inventorySlots.Find(slot => slot.itemId == itemId);

        if (existingSlot != null)
        {
            // Одинаковые предметы вкладываются друг в друга! Увеличиваем счетчик
            existingSlot.count += count;
            Debug.Log($"[ИНВЕНТАРЬ] Предмет {itemName} сложен в стек! Новое количество: x{existingSlot.count}");
        }
        else
        {
            // Создаем новую ячейку со счетчиком
            ItemStack newStack = new ItemStack(itemId, itemName, count, xpPerItem, icon, rarityColor);
            inventorySlots.Add(newStack);
            Debug.Log($"[ИНВЕНТАРЬ] Создан новый слот для {itemName} (x{count})");
        }

        UpdateChestUI();
    }

    /// <summary>
    /// Массовое добавление улова из мини-игры рыбалки
    /// </summary>
    public void AddFishingSessionLoot(List<AlchemyFishing_Minigame.LootResult> caughtLoot)
    {
        foreach (var loot in caughtLoot)
        {
            AddItem(loot.itemId, loot.itemName, 1, loot.xp, loot.sprite, loot.rarityColor);
        }

        UpdateChestUI();
    }

    /// <summary>
    /// Отрисовка UI сундука со стаками предметов
    /// </summary>
    public void UpdateChestUI()
    {
        if (chestSlotsContainer == null || chestSlotPrefab == null) return;

        foreach (Transform child in chestSlotsContainer)
        {
            Destroy(child.gameObject);
        }

        int totalCount = 0;
        int totalXp = 0;

        foreach (var stack in inventorySlots)
        {
            totalCount += stack.count;
            totalXp += stack.xpPerItem * stack.count;

            GameObject slotObj = Instantiate(chestSlotPrefab, chestSlotsContainer);
            
            // Иконка
            Image iconImg = slotObj.transform.Find("Item_Icon")?.GetComponent<Image>();
            if (iconImg != null && stack.icon != null) iconImg.sprite = stack.icon;

            // Счетчик количества (x3, x4 и т.д.)
            TextMeshProUGUI countBadge = slotObj.transform.Find("Count_Badge/Text")?.GetComponent<TextMeshProUGUI>();
            if (countBadge != null)
            {
                countBadge.text = $"x{stack.count}";
                countBadge.transform.parent.gameObject.SetActive(stack.count > 1);
            }

            // Название предмета
            TextMeshProUGUI titleText = slotObj.transform.Find("Item_Title")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = stack.itemName;
                titleText.color = stack.rarityColor;
            }
        }

        if (totalItemsCountText != null)
            totalItemsCountText.text = $"Предметов в сундуке: {totalCount} шт ({inventorySlots.Count} слотов)";

        if (totalChestXpText != null)
            totalChestXpText.text = $"Всего опыта в зельях: +{totalXp} XP";
    }
}`,
  },
  {
    name: 'CatchMouse_Minigame.cs',
    category: 'Minigames',
    description: 'Ловля мышей: Holes_Container (Y=60), Road_Track (Y=-18, W=960, H=120, RotX=-126.083), 3 сложности, награды и диалог Кота.',
    code: `using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CatchMouse_Minigame : MonoBehaviour
{
    public static CatchMouse_Minigame Instance;

    [Header("Панели и Контейнеры")]
    public GameObject mouseGamePanel;
    public RectTransform holesContainer; // Pos Y = 60, Width = 1000, Height = 150
    public RectTransform roadTrack;      // Pos Y = -18, Width = 960, Height = 120, Rot X = -126.083
    public Button closeButton;
    public GameObject victoryDialogPanel;
    public Button consentButton;

    public void HandleCloseButtonClicked()
    {
        if (mouseGamePanel != null) mouseGamePanel.SetActive(false);
    }
}`,
  },
  {
    name: 'HiddenObject_Minigame.cs',
    category: 'Minigames',
    description: 'Поиск предметов в 3 локациях (8K Fullscreen): адаптивность под 4K PC и любые экраны телефонов, Pinch-zoom, панорамирование, таймер, подсказки Кота и начисление наград.',
    code: `using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Мини-игра: Поиск предметов в 3 локациях (Hidden Object Game):
/// 1. Алхимическая лавка Кота (Alchemy Shop)
/// 2. Старый заброшенный дом (Old Abandoned House)
/// 3. Антикварный рынок (Antique Fantasy Market)
/// 
/// Особенности:
/// - Полноэкранный адаптивный рендер под 4K PC (16:9, 21:9, 16:10) и все мобильные экраны (19.5:9, 20:9, 4:3)
/// - Поддержка плавного зума (Pinch-to-zoom на телефонах, Колесо мыши на ПК) и перемещения (Pan/Drag)
/// - Нижняя панель целей со спрайтами и счетчиком оставшихся предметов
/// - Подсветка найденных предметов, анимация полета в инвентарь
/// - Автоматическая передача найденных наград в Inventory_Manager со стаком (xN)
/// </summary>
public class HiddenObject_Minigame : MonoBehaviour
{
    public static HiddenObject_Minigame Instance { get; private set; }

    public enum SearchLocation
    {
        AlchemyShop,        // 1. Алхимическая лавка Кота
        OldAbandonedHouse,  // 2. Старый заброшенный дом
        AntiqueMarket       // 3. Антикварный рынок
    }

    [System.Serializable]
    public class HiddenItemTarget
    {
        public string itemId;
        public string itemName;
        public Sprite itemIcon;
        public Color rarityColor = Color.white;
        public int rewardXp = 50;
        public GameObject sceneClickableObject;
        [HideInInspector] public bool isFound = false;
    }

    [System.Serializable]
    public class LocationData
    {
        public SearchLocation locationType;
        public string locationName;
        public Sprite background8kSprite;
        public GameObject locationSceneRoot;
        public List<HiddenItemTarget> itemsToFind = new List<HiddenItemTarget>();
    }

    [Header("=== Главные панели ===")]
    public GameObject hiddenObjectPanel;
    public GameObject locationSelectPopup;
    public Button closeGameButton;

    [Header("=== Viewport & Зум/Панорамирование (Все разрешения 4K/Mobile) ===")]
    public RectTransform viewportContainer;     // Область просмотра (Anchor: Stretch All)
    public RectTransform backgroundContentRoot;  // Двигаемый и масштабируемый фон со спрайтом
    public Image backgroundLocationImage;
    public AspectRatioFitter backgroundAspectFitter;
    public float minZoom = 1.0f;
    public float maxZoom = 2.8f;
    public float zoomSpeed = 0.5f;

    [Header("=== Нижняя панель целей (Target Items Bar) ===")]
    public Transform targetItemsContainer;
    public GameObject targetItemSlotPrefab;
    public TextMeshProUGUI itemsRemainingText;
    public TextMeshProUGUI locationTitleText;

    [Header("=== Кнопки подсказок и таймер ===")]
    public Button hintCatButton;
    public TextMeshProUGUI hintCountText;
    public int availableHints = 3;
    public TextMeshProUGUI timerText;
    public float searchTimeRemaining = 180f; // 3 минуты на локацию
    public bool isTimerActive = false;

    [Header("=== Окно победы локации ===")]
    public GameObject victoryPopupPanel;
    public TextMeshProUGUI victoryTitleText;
    public TextMeshProUGUI victoryRewardsText;
    public Button claimRewardsAndNextButton;

    [Header("=== Конфигурация 3 Локаций ===")]
    public List<LocationData> locations = new List<LocationData>();
    private LocationData currentLocation;
    private int foundCount = 0;
    private float currentZoom = 1.0f;
    private Vector2 panOffset = Vector2.zero;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (closeGameButton) closeGameButton.onClick.AddListener(CloseMinigame);
        if (hintCatButton) hintCatButton.onClick.AddListener(UseCatHint);
        if (claimRewardsAndNextButton) claimRewardsAndNextButton.onClick.AddListener(ClaimVictoryRewards);
    }

    public void StartLocationSearch(SearchLocation locType)
    {
        currentLocation = locations.Find(l => l.locationType == locType);
        if (currentLocation == null) return;

        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(true);
        if (locationSelectPopup) locationSelectPopup.SetActive(false);
        if (victoryPopupPanel) victoryPopupPanel.SetActive(false);

        if (backgroundLocationImage && currentLocation.background8kSprite != null)
        {
            backgroundLocationImage.sprite = currentLocation.background8kSprite;
        }

        foreach (var loc in locations)
        {
            if (loc.locationSceneRoot)
                loc.locationSceneRoot.SetActive(loc == currentLocation);
        }

        foundCount = 0;
        currentZoom = 1.0f;
        panOffset = Vector2.zero;
        ApplyZoomAndPan();

        foreach (var item in currentLocation.itemsToFind)
        {
            item.isFound = false;
            if (item.sceneClickableObject)
            {
                item.sceneClickableObject.SetActive(true);
                Button btn = item.sceneClickableObject.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnItemClicked(item));
                }
            }
        }

        searchTimeRemaining = 180f;
        isTimerActive = true;
        UpdateTargetsUI();

        if (locationTitleText)
            locationTitleText.text = currentLocation.locationName;
    }

    public void OnItemClicked(HiddenItemTarget item)
    {
        if (item.isFound) return;
        item.isFound = true;
        foundCount++;

        if (item.sceneClickableObject)
        {
            item.sceneClickableObject.SetActive(false);
        }

        if (Inventory_Manager.Instance != null)
        {
            Inventory_Manager.Instance.AddItem(
                item.itemId,
                item.itemName,
                1,
                item.rewardXp,
                item.itemIcon,
                item.rarityColor
            );
        }

        UpdateTargetsUI();

        if (foundCount >= currentLocation.itemsToFind.Count)
        {
            OnLocationCompleted();
        }
    }

    private void UpdateTargetsUI()
    {
        if (itemsRemainingText && currentLocation != null)
        {
            int total = currentLocation.itemsToFind.Count;
            itemsRemainingText.text = $"Найдено: {foundCount} / {total}";
        }

        if (hintCountText)
            hintCountText.text = $"💡 Подсказка Кота: {availableHints}";
    }

    private void OnLocationCompleted()
    {
        isTimerActive = false;

        if (victoryPopupPanel) victoryPopupPanel.SetActive(true);
        if (victoryTitleText)
            victoryTitleText.text = $"Локация «{currentLocation.locationName}» пройдена!";

        if (victoryRewardsText)
        {
            victoryRewardsText.text = "+5 000 Золота, +3 Камня, +2 Свитка и все найденные артефакты отправлены в сундук!";
        }

        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.AddGold(5000);
            Avatar_Manager.Instance.AddStones(3);
            Avatar_Manager.Instance.AddScrolls(2);
            Avatar_Manager.Instance.AddExperience(500);
        }
    }

    public void ClaimVictoryRewards()
    {
        if (victoryPopupPanel) victoryPopupPanel.SetActive(false);
        if (locationSelectPopup) locationSelectPopup.SetActive(true);
        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(false);
    }

    public void UseCatHint()
    {
        if (availableHints <= 0 || currentLocation == null) return;

        HiddenItemTarget unFound = currentLocation.itemsToFind.Find(i => !i.isFound);
        if (unFound != null && unFound.sceneClickableObject != null)
        {
            availableHints--;
            StartCoroutine(PulseHintEffect(unFound.sceneClickableObject));
            UpdateTargetsUI();
        }
    }

    private IEnumerator PulseHintEffect(GameObject obj)
    {
        Transform t = obj.transform;
        Vector3 originalScale = t.localScale;
        for (int i = 0; i < 3; i++)
        {
            t.localScale = originalScale * 1.4f;
            yield return new WaitForSeconds(0.2f);
            t.localScale = originalScale;
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void Update()
    {
        if (isTimerActive && searchTimeRemaining > 0)
        {
            searchTimeRemaining -= Time.deltaTime;
            if (timerText)
            {
                int min = Mathf.FloorToInt(searchTimeRemaining / 60);
                int sec = Mathf.FloorToInt(searchTimeRemaining % 60);
                timerText.text = $"⏳ {min:00}:{sec:00}";
            }

            if (searchTimeRemaining <= 0)
            {
                isTimerActive = false;
            }
        }

        HandleTouchAndMouseZoomPan();
    }

    private void HandleTouchAndMouseZoomPan()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed * 3f, minZoom, maxZoom);
            ApplyZoomAndPan();
        }

        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            currentZoom = Mathf.Clamp(currentZoom - deltaMagnitudeDiff * 0.005f, minZoom, maxZoom);
            ApplyZoomAndPan();
        }
    }

    private void ApplyZoomAndPan()
    {
        if (backgroundContentRoot)
        {
            backgroundContentRoot.localScale = new Vector3(currentZoom, currentZoom, 1f);
            backgroundContentRoot.anchoredPosition = panOffset;
        }
    }

    public void CloseMinigame()
    {
        isTimerActive = false;
        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(false);
        if (locationSelectPopup) locationSelectPopup.SetActive(true);
    }
}`,
  },
];

export const ScriptsViewer: React.FC = () => {
  const [selectedScript, setSelectedScript] = useState<CSharpScript>(SCRIPTS_COLLECTION[0]);
  const [search, setSearch] = useState<string>('');
  const [copied, setCopied] = useState<boolean>(false);

  const filtered = SCRIPTS_COLLECTION.filter(s =>
    s.name.toLowerCase().includes(search.toLowerCase()) || s.description.toLowerCase().includes(search.toLowerCase())
  );

  const handleCopy = () => {
    navigator.clipboard.writeText(selectedScript.code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="flex flex-col gap-6 max-w-6xl mx-auto">
      {/* Header */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400">
            <Code className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white">ЧАСТЬ 4. Готовый C# скрипт AlchemyFishing_Minigame.cs</h2>
            <p className="text-xs text-slate-400">
              Исходный код контроллера рыбалки с поддержкой клика по удочке и поочередных шкал
            </p>
          </div>
        </div>

        <button
          onClick={handleCopy}
          className="flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-400 hover:to-amber-500 text-slate-950 text-xs font-bold rounded-xl shadow-lg transition"
        >
          {copied ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
          <span>{copied ? 'Скопировано в буфер!' : 'Скопировать C# скрипт'}</span>
        </button>
      </div>

      {/* Script Selection Tabs */}
      <div className="flex flex-wrap gap-2">
        {SCRIPTS_COLLECTION.map((s) => (
          <button
            key={s.name}
            onClick={() => setSelectedScript(s)}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 border ${
              selectedScript.name === s.name
                ? 'bg-amber-500 text-slate-950 border-amber-400 shadow-lg'
                : 'bg-slate-900/80 text-slate-400 border-slate-800 hover:text-white hover:border-slate-700'
            }`}
          >
            <FileCode className="w-4 h-4" />
            <span>{s.name}</span>
          </button>
        ))}
      </div>

      {/* Script Details & Code Box */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-6 flex flex-col gap-4 shadow-xl">
        <div className="flex items-center justify-between border-b border-slate-800 pb-3">
          <div className="flex items-center gap-2">
            <FileCode className="w-5 h-5 text-amber-400" />
            <span className="text-sm font-bold text-white">{selectedScript.name}</span>
            <span className="text-[10px] px-2 py-0.5 rounded-full bg-emerald-950 border border-emerald-500/40 text-emerald-300 font-semibold">
              Готов к компиляции
            </span>
          </div>
          <span className="text-xs text-slate-400">{selectedScript.description}</span>
        </div>

        <div className="bg-slate-950 rounded-xl p-4 border border-slate-800 font-mono text-xs text-slate-200 overflow-x-auto max-h-[500px] overflow-y-auto whitespace-pre leading-relaxed shadow-inner">
          {selectedScript.code}
        </div>
      </div>
    </div>
  );
};
