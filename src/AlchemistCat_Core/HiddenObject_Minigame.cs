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
    public static HiddenObject_Minigame Instance { get; private set; }

    public enum DifficultyTier { Easy, Normal, Hard }
    public enum RecordModeType { TimeRush, BigCountSurge, FlickerSpawn }

    [System.Serializable]
    public class DifficultyConfig
    {
        public DifficultyTier tier;
        public string tierName = "Легкий";
        public int itemsPerRound = 5;       // Количество предметов за 1 раунд
        public int roundsRequired = 3;      // Количество раундов (попыток) для победы
        public float timeLimitPerRound = 90f; // Время на 1 раунд в секундах

        [Header("Награды за завершение")]
        public int rewardGold = 1000;
        public int rewardStones = 3;
        public int rewardScrolls = 1;
        public int rewardExpPotion100 = 0;
        public int rewardExpPotion500 = 0;
        public int rewardMasteryPotion100 = 0;
        public int rewardMasteryPotion500 = 0;
    }

    [System.Serializable]
    public class LocationConfig
    {
        public string locationId = "Shop";
        public string locationName = "Лавка Алхимика";
        public Sprite backgroundSprite;
        public Button locationCardButton;
        public List<Sprite> itemsPool = new List<Sprite>();

        [Header("3 Уровня сложности локации")]
        public List<DifficultyConfig> difficulties = new List<DifficultyConfig>();

        [Header("Статус прохождения")]
        public bool isEasyCompleted;
        public bool isNormalCompleted;
        public bool isHardCompleted;
        public bool IsFullyCompleted => isEasyCompleted && isNormalCompleted && isHardCompleted;
    }

    [Header("=== Главные панели ===")]
    public GameObject hiddenObjectPanel;
    public GameObject locationSelectPopup;
    public GameObject difficultySelectPopup;
    public Button closeGameButton;

    [Header("=== Кнопки выбора сложности ===")]
    public Button buttonEasy;
    public Button buttonNormal;
    public Button buttonHard;
    public TextMeshProUGUI difficultyPopupTitleText;

    [Header("=== Viewport и Зум/Панорамирование ===")]
    public RectTransform viewportContainer;
    public RectTransform backgroundContentRoot;
    public Image backgroundLocationImage;
    public AspectRatioFitter backgroundAspect;
    public float minZoom = 1.0f;
    public float maxZoom = 2.8f;
    public float zoomSpeed = 0.5f;

    [Header("=== Нижняя панель целей (Target Items Bar) ===")]
    public RectTransform targetIconsContainer;
    public GameObject targetItemSlotPrefab;
    public TextMeshProUGUI itemsRemainingText;
    public TextMeshProUGUI locationTitleText;
    public TextMeshProUGUI currentRoundText;

    [Header("=== Кнопки подсказок и таймер ===")]
    public Button hintCatButton;
    public TextMeshProUGUI hintCountText;
    public TextMeshProUGUI timerText;
    public int availableHints = 3;

    [Header("=== Окно победы локации ===")]
    public GameObject victoryPopupPanel;
    public TextMeshProUGUI victoryTitleText;
    public TextMeshProUGUI victoryRewardsText;
    public Button claimRewardsAndBackButton;

    [Header("=== Кот и Режим 'Становление Рекорда' ===")]
    public GameObject catRecordUnlockedDialog; // Окно диалога с Котом
    public TextMeshProUGUI catSpeechText;
    public Button catDialogContinueButton;
    public GameObject recordModeSelectPopup;    // Попап с 3 хардкорными испытаниями
    public Button recordTimeRushButton;        // Блиц на время (+1 Кристалл)
    public Button recordItemSurgeButton;       // Лавина предметов (+1..5 Кристаллов)
    public Button recordFlickerSpawnButton;    // Мерцание (+1..10 Кристаллов)
    public Button closeRecordPopupButton;

    [Header("=== Спрайты зелий для выдачи в инвентарь ===")]
    public Sprite expPotion100Sprite;
    public Sprite expPotion500Sprite;
    public Sprite masteryPotion100Sprite;
    public Sprite masteryPotion500Sprite;
    public Sprite stoneSprite;
    public Sprite scrollSprite;

    [Header("=== Конфигурация 3 Локаций ===")]
    public List<LocationConfig> locations = new List<LocationConfig>();

    // Внутреннее состояние игры
    private LocationConfig currentLocation;
    private DifficultyConfig currentDifficulty;
    private int currentRoundIndex = 1;
    private int itemsFoundInCurrentRound = 0;
    private float roundTimer = 0f;
    private bool isGameRunning = false;
    private bool isRecordModeActive = false;
    private RecordModeType currentRecordMode = RecordModeType.TimeRush;
    private int recordTargetCount = 0;
    private float currentZoom = 1.0f;
    private Vector2 panOffset = Vector2.zero;
    private List<GameObject> activeClickableItems = new List<GameObject>();
    private Coroutine flickerCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeDefaultConfigurationsIfEmpty();
        SetupButtons();
        LoadCompletionProgress();
    }

    private void SetupButtons()
    {
        if (closeGameButton) closeGameButton.onClick.AddListener(CloseGame);
        if (claimRewardsAndBackButton) claimRewardsAndBackButton.onClick.AddListener(OnClaimRewardsClicked);
        if (hintCatButton) hintCatButton.onClick.AddListener(UseHint);

        // Кнопки сложностей
        if (buttonEasy) buttonEasy.onClick.AddListener(() => StartGameWithDifficulty(0));
        if (buttonNormal) buttonNormal.onClick.AddListener(() => StartGameWithDifficulty(1));
        if (buttonHard) buttonHard.onClick.AddListener(() => StartGameWithDifficulty(2));

        // Кнопки рекордов
        if (catDialogContinueButton) catDialogContinueButton.onClick.AddListener(OpenRecordModeSelection);
        if (recordTimeRushButton) recordTimeRushButton.onClick.AddListener(() => StartRecordMode(RecordModeType.TimeRush));
        if (recordItemSurgeButton) recordItemSurgeButton.onClick.AddListener(() => StartRecordMode(RecordModeType.BigCountSurge));
        if (recordFlickerSpawnButton) recordFlickerSpawnButton.onClick.AddListener(() => StartRecordMode(RecordModeType.FlickerSpawn));
        if (closeRecordPopupButton) closeRecordPopupButton.onClick.AddListener(() => {
            if (recordModeSelectPopup) recordModeSelectPopup.SetActive(false);
            if (locationSelectPopup) locationSelectPopup.SetActive(true);
        });

        // Карточки локаций
        for (int i = 0; i < locations.Count; i++)
        {
            int locIndex = i;
            if (locations[i].locationCardButton != null)
            {
                locations[i].locationCardButton.onClick.RemoveAllListeners();
                locations[i].locationCardButton.onClick.AddListener(() => OpenLocationDifficultySelect(locIndex));
            }
        }
    }

    /// <summary>
    /// Открытие окна выбора сложности для выбранной локации
    /// </summary>
    public void OpenLocationDifficultySelect(int locationIndex)
    {
        if (locationIndex < 0 || locationIndex >= locations.Count) return;
        currentLocation = locations[locationIndex];

        if (difficultyPopupTitleText)
            difficultyPopupTitleText.text = $"Сложность: {currentLocation.locationName}";

        if (locationSelectPopup) locationSelectPopup.SetActive(false);
        if (difficultySelectPopup) difficultySelectPopup.SetActive(true);
    }

    /// <summary>
    /// Старт локации с выбранной сложностью (0 - Легкий, 1 - Нормальный, 2 - Сложный)
    /// </summary>
    public void StartGameWithDifficulty(int difficultyIndex)
    {
        if (currentLocation == null) return;
        if (difficultyIndex < 0 || difficultyIndex >= currentLocation.difficulties.Count) return;

        currentDifficulty = currentLocation.difficulties[difficultyIndex];

        if (difficultySelectPopup) difficultySelectPopup.SetActive(false);
        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(true);

        isRecordModeActive = false;
        currentRoundIndex = 1;
        availableHints = 3;

        StartRound();
    }

    private void StartRound()
    {
        itemsFoundInCurrentRound = 0;
        roundTimer = currentDifficulty.timeLimitPerRound;
        isGameRunning = true;

        if (backgroundLocationImage && currentLocation.backgroundSprite != null)
        {
            backgroundLocationImage.sprite = currentLocation.backgroundSprite;
        }

        if (locationTitleText)
        {
            locationTitleText.text = $"{currentLocation.locationName} — {currentDifficulty.tierName}";
        }

        currentZoom = 1.0f;
        panOffset = Vector2.zero;
        ApplyZoomAndPan();

        UpdateUI();
        SpawnTargetItemsForRound();
    }

    private void SpawnTargetItemsForRound()
    {
        ClearActiveItems();

        if (targetIconsContainer == null || currentLocation.itemsPool.Count == 0) return;

        int totalToFind = currentDifficulty.itemsPerRound;
        for (int i = 0; i < totalToFind; i++)
        {
            Sprite itemSprite = currentLocation.itemsPool[i % currentLocation.itemsPool.Count];

            // Создание иконки цели в нижней панели
            GameObject slotUi = null;
            if (targetItemSlotPrefab != null)
            {
                slotUi = Instantiate(targetItemSlotPrefab, targetIconsContainer);
                Image img = slotUi.GetComponentInChildren<Image>();
                if (img) img.sprite = itemSprite;
            }

            // Спавн кликабельного предмета на фоне
            SpawnClickableItemOnBackground(itemSprite, slotUi);
        }
    }

    private void SpawnClickableItemOnBackground(Sprite itemSprite, GameObject slotUi)
    {
        if (backgroundContentRoot == null) return;

        GameObject clickable = new GameObject("HiddenItem_" + itemSprite.name, typeof(RectTransform), typeof(Image), typeof(Button));
        clickable.transform.SetParent(backgroundContentRoot, false);
        activeClickableItems.Add(clickable);

        RectTransform rt = clickable.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(75, 75);

        // Случайные координаты в пределах фоновой сцены
        float posX = UnityEngine.Random.Range(-550f, 550f);
        float posY = UnityEngine.Random.Range(-320f, 320f);
        rt.anchoredPosition = new Vector2(posX, posY);

        Image img = clickable.GetComponent<Image>();
        img.sprite = itemSprite;
        img.preserveAspect = true;

        Button btn = clickable.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            activeClickableItems.Remove(clickable);
            Destroy(clickable);
            if (slotUi != null) Destroy(slotUi);
            OnItemFound();
        });
    }

    private void OnItemFound()
    {
        itemsFoundInCurrentRound++;
        UpdateUI();

        if (isRecordModeActive)
        {
            if (itemsFoundInCurrentRound >= recordTargetCount)
            {
                CompleteRecordModeVictory();
            }
            return;
        }

        if (itemsFoundInCurrentRound >= currentDifficulty.itemsPerRound)
        {
            if (currentRoundIndex < currentDifficulty.roundsRequired)
            {
                currentRoundIndex++;
                StartRound();
            }
            else
            {
                CompleteCurrentDifficultyStage();
            }
        }
    }

    private void CompleteCurrentDifficultyStage()
    {
        isGameRunning = false;

        if (currentDifficulty.tier == DifficultyTier.Easy) currentLocation.isEasyCompleted = true;
        if (currentDifficulty.tier == DifficultyTier.Normal) currentLocation.isNormalCompleted = true;
        if (currentDifficulty.tier == DifficultyTier.Hard) currentLocation.isHardCompleted = true;

        SaveCompletionProgress();
        GrantRewards();

        if (victoryPopupPanel)
        {
            victoryPopupPanel.SetActive(true);
            if (victoryTitleText)
                victoryTitleText.text = $"Победа: {currentLocation.locationName} ({currentDifficulty.tierName})!";

            if (victoryRewardsText)
            {
                string rewardsSummary = "";
                if (currentDifficulty.rewardStones > 0) rewardsSummary += $"💎 Камни: +{currentDifficulty.rewardStones}  ";
                if (currentDifficulty.rewardScrolls > 0) rewardsSummary += $"📜 Свитки: +{currentDifficulty.rewardScrolls}\n";
                if (currentDifficulty.rewardExpPotion100 > 0) rewardsSummary += $"🧪 Зелье Опыта (+100 XP): x{currentDifficulty.rewardExpPotion100}\n";
                if (currentDifficulty.rewardExpPotion500 > 0) rewardsSummary += $"🧪 Зелье Опыта (+500 XP): x{currentDifficulty.rewardExpPotion500}\n";
                if (currentDifficulty.rewardMasteryPotion100 > 0) rewardsSummary += $"✨ Зелье Мастерства (+100 XP): x{currentDifficulty.rewardMasteryPotion100}\n";
                if (currentDifficulty.rewardMasteryPotion500 > 0) rewardsSummary += $"✨ Зелье Мастерства (+500 XP): x{currentDifficulty.rewardMasteryPotion500}\n";

                victoryRewardsText.text = rewardsSummary;
            }
        }
    }

    private void GrantRewards()
    {
        if (Avatar_Manager.Instance != null)
        {
            if (currentDifficulty.rewardStones > 0) Avatar_Manager.Instance.AddStones(currentDifficulty.rewardStones);
            if (currentDifficulty.rewardScrolls > 0) Avatar_Manager.Instance.AddScrolls(currentDifficulty.rewardScrolls);
            if (currentDifficulty.rewardGold > 0) Avatar_Manager.Instance.AddGold(currentDifficulty.rewardGold);
        }

        if (Inventory_Manager.Instance != null)
        {
            if (currentDifficulty.rewardExpPotion100 > 0)
                Inventory_Manager.Instance.AddItem("pot_exp_100", "Зелье Опыта (+100 XP)", currentDifficulty.rewardExpPotion100, 100, expPotion100Sprite, Color.cyan);
            if (currentDifficulty.rewardExpPotion500 > 0)
                Inventory_Manager.Instance.AddItem("pot_exp_500", "Зелье Опыта (+500 XP)", currentDifficulty.rewardExpPotion500, 500, expPotion500Sprite, Color.magenta);
            if (currentDifficulty.rewardMasteryPotion100 > 0)
                Inventory_Manager.Instance.AddItem("pot_mastery_100", "Зелье Мастерства (+100 XP)", currentDifficulty.rewardMasteryPotion100, 100, masteryPotion100Sprite, Color.green);
            if (currentDifficulty.rewardMasteryPotion500 > 0)
                Inventory_Manager.Instance.AddItem("pot_mastery_500", "Зелье Мастерства (+500 XP)", currentDifficulty.rewardMasteryPotion500, 500, masteryPotion500Sprite, Color.yellow);
        }
    }

    private void OnClaimRewardsClicked()
    {
        if (victoryPopupPanel) victoryPopupPanel.SetActive(false);
        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(false);

        // Проверка: пройдены ли все 3 локации на всех сложностях?
        bool allCompleted = true;
        foreach (var loc in locations)
        {
            if (!loc.IsFullyCompleted) { allCompleted = false; break; }
        }

        if (allCompleted)
        {
            bool recordDialogShown = PlayerPrefs.GetInt("CatRecordDialogShown", 0) == 1;
            if (!recordDialogShown)
            {
                PlayerPrefs.SetInt("CatRecordDialogShown", 1);
                PlayerPrefs.Save();
                TriggerCatRecordUnlockDialog();
                return;
            }
        }

        if (locationSelectPopup) locationSelectPopup.SetActive(true);
    }

    private void TriggerCatRecordUnlockDialog()
    {
        if (catRecordUnlockedDialog)
        {
            catRecordUnlockedDialog.SetActive(true);
            if (catSpeechText)
            {
                catSpeechText.text =
                    "Мяу! Невероятно, Мастер! Ты блестяще преодолел все три локации на всех сложностях!\n\n" +
                    "Теперь тебе открывается высшее испытание: «Становление Рекорда»!\n" +
                    "Испытай свои силы: Блиц-поиск на время, Лавина предметов (20-30 шт) и Мерцающие вспышки с наградами от 1 до 10 Кристаллов раз в месяц!";
            }
        }
    }

    public void OpenRecordModeSelection()
    {
        if (catRecordUnlockedDialog) catRecordUnlockedDialog.SetActive(false);
        if (recordModeSelectPopup) recordModeSelectPopup.SetActive(true);
    }

    public void StartRecordMode(RecordModeType mode)
    {
        isRecordModeActive = true;
        currentRecordMode = mode;

        if (recordModeSelectPopup) recordModeSelectPopup.SetActive(false);
        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(true);

        ClearActiveItems();
        itemsFoundInCurrentRound = 0;
        isGameRunning = true;

        if (locations.Count > 0 && locations[0].backgroundSprite != null)
            backgroundLocationImage.sprite = locations[UnityEngine.Random.Range(0, locations.Count)].backgroundSprite;

        switch (mode)
        {
            case RecordModeType.TimeRush:
                recordTargetCount = 15;
                roundTimer = 40f; // Экстремальное время на 15 предметов
                if (locationTitleText) locationTitleText.text = "★ СТАНОВЛЕНИЕ РЕКОРДА: БЛИЦ ★";
                SpawnRecordItems(recordTargetCount);
                break;

            case RecordModeType.BigCountSurge:
                recordTargetCount = UnityEngine.Random.Range(20, 31);
                roundTimer = 150f;
                if (locationTitleText) locationTitleText.text = $"★ СТАНОВЛЕНИЕ РЕКОРДА: ЛАВИНА ({recordTargetCount} шт) ★";
                SpawnRecordItems(recordTargetCount);
                break;

            case RecordModeType.FlickerSpawn:
                recordTargetCount = UnityEngine.Random.Range(20, 31);
                roundTimer = 180f;
                if (locationTitleText) locationTitleText.text = $"★ СТАНОВЛЕНИЕ РЕКОРДА: МЕРЦАНИЕ ({recordTargetCount} шт) ★";
                if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
                flickerCoroutine = StartCoroutine(FlickerSpawnRoutine(recordTargetCount));
                break;
        }

        UpdateUI();
    }

    private void SpawnRecordItems(int count)
    {
        List<Sprite> combinedPool = new List<Sprite>();
        foreach (var loc in locations) combinedPool.AddRange(loc.itemsPool);
        if (combinedPool.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            Sprite itemSprite = combinedPool[i % combinedPool.Count];
            GameObject slotUi = null;
            if (targetItemSlotPrefab != null)
            {
                slotUi = Instantiate(targetItemSlotPrefab, targetIconsContainer);
                Image img = slotUi.GetComponentInChildren<Image>();
                if (img) img.sprite = itemSprite;
            }
            SpawnClickableItemOnBackground(itemSprite, slotUi);
        }
    }

    private IEnumerator FlickerSpawnRoutine(int targetCount)
    {
        List<Sprite> combinedPool = new List<Sprite>();
        foreach (var loc in locations) combinedPool.AddRange(loc.itemsPool);
        if (combinedPool.Count == 0) yield break;

        for (int i = 0; i < targetCount; i++)
        {
            if (!isGameRunning || !isRecordModeActive) yield break;

            yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 1.8f));

            Sprite itemSprite = combinedPool[UnityEngine.Random.Range(0, combinedPool.Count)];
            GameObject clickable = new GameObject("FlickerItem_" + i, typeof(RectTransform), typeof(Image), typeof(Button));
            clickable.transform.SetParent(backgroundContentRoot, false);
            activeClickableItems.Add(clickable);

            RectTransform rt = clickable.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(75, 75);
            rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-550f, 550f), UnityEngine.Random.Range(-320f, 320f));

            Image img = clickable.GetComponent<Image>();
            img.sprite = itemSprite;
            img.preserveAspect = true;

            Button btn = clickable.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                activeClickableItems.Remove(clickable);
                Destroy(clickable);
                OnItemFound();
            });

            // Предмет исчезает через 1.2 секунды
            StartCoroutine(DisappearAfterTime(clickable, 1.2f));
        }
    }

    private IEnumerator DisappearAfterTime(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            activeClickableItems.Remove(obj);
            Destroy(obj);
        }
    }

    private void CompleteRecordModeVictory()
    {
        isGameRunning = false;
        int crystalsReward = 1;

        switch (currentRecordMode)
        {
            case RecordModeType.TimeRush:
                crystalsReward = 1;
                break;
            case RecordModeType.BigCountSurge:
                crystalsReward = UnityEngine.Random.Range(1, 6);
                break;
            case RecordModeType.FlickerSpawn:
                crystalsReward = UnityEngine.Random.Range(1, 11);
                break;
        }

        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.AddCrystals(crystalsReward);
        }

        if (victoryPopupPanel)
        {
            victoryPopupPanel.SetActive(true);
            if (victoryTitleText) victoryTitleText.text = "★ ВЕЛИКИЙ РЕКОРД УСТАНОВЛЕН! ★";
            if (victoryRewardsText)
            {
                victoryRewardsText.text = $"Вы одолели тяжелейшее испытание месяца!\n💎 Получено Кристаллов: +{crystalsReward}";
            }
        }
    }

    private void Update()
    {
        if (!isGameRunning) return;

        roundTimer -= Time.deltaTime;
        if (timerText)
        {
            int min = Mathf.FloorToInt(Mathf.Max(0, roundTimer) / 60f);
            int sec = Mathf.FloorToInt(Mathf.Max(0, roundTimer) % 60f);
            timerText.text = $"⏳ {min:00}:{sec:00}";
        }

        if (roundTimer <= 0)
        {
            GameOverTimeout();
        }

        HandleTouchAndMouseZoomPan();
    }

    private void GameOverTimeout()
    {
        isGameRunning = false;
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
        ClearActiveItems();

        if (victoryPopupPanel)
        {
            victoryPopupPanel.SetActive(true);
            if (victoryTitleText) victoryTitleText.text = "Время вышло!";
            if (victoryRewardsText) victoryRewardsText.text = "Попробуйте снова преодолеть это испытание!";
        }
    }

    private void UseHint()
    {
        if (availableHints <= 0 || activeClickableItems.Count == 0) return;
        availableHints--;
        if (hintCountText) hintCountText.text = availableHints.ToString();

        GameObject itemToHighlight = activeClickableItems[0];
        if (itemToHighlight != null)
        {
            StartCoroutine(PulseHintEffect(itemToHighlight));
        }
    }

    private IEnumerator PulseHintEffect(GameObject obj)
    {
        Transform t = obj.transform;
        Vector3 originalScale = t.localScale;
        for (int i = 0; i < 3; i++)
        {
            if (obj == null) yield break;
            t.localScale = originalScale * 1.5f;
            yield return new WaitForSeconds(0.2f);
            t.localScale = originalScale;
            yield return new WaitForSeconds(0.2f);
        }
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

    private void UpdateUI()
    {
        if (itemsRemainingText)
        {
            int total = isRecordModeActive ? recordTargetCount : (currentDifficulty != null ? currentDifficulty.itemsPerRound : 0);
            itemsRemainingText.text = $"Осталось: {Mathf.Max(0, total - itemsFoundInCurrentRound)}";
        }

        if (currentRoundText)
        {
            if (isRecordModeActive)
                currentRoundText.text = "Рекордный раунд";
            else if (currentDifficulty != null)
                currentRoundText.text = $"Этап {currentRoundIndex} из {currentDifficulty.roundsRequired}";
        }

        if (hintCountText)
            hintCountText.text = $"💡 Подсказка: {availableHints}";
    }

    private void ClearActiveItems()
    {
        foreach (var item in activeClickableItems)
        {
            if (item != null) Destroy(item);
        }
        activeClickableItems.Clear();

        if (targetIconsContainer != null)
        {
            foreach (Transform child in targetIconsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void CloseGame()
    {
        isGameRunning = false;
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
        ClearActiveItems();

        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(false);
        if (difficultySelectPopup) difficultySelectPopup.SetActive(false);
        if (recordModeSelectPopup) recordModeSelectPopup.SetActive(false);
        if (locationSelectPopup) locationSelectPopup.SetActive(true);
    }

    private void SaveCompletionProgress()
    {
        for (int i = 0; i < locations.Count; i++)
        {
            PlayerPrefs.SetInt($"Loc_{i}_Easy", locations[i].isEasyCompleted ? 1 : 0);
            PlayerPrefs.SetInt($"Loc_{i}_Norm", locations[i].isNormalCompleted ? 1 : 0);
            PlayerPrefs.SetInt($"Loc_{i}_Hard", locations[i].isHardCompleted ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    private void LoadCompletionProgress()
    {
        for (int i = 0; i < locations.Count; i++)
        {
            locations[i].isEasyCompleted = PlayerPrefs.GetInt($"Loc_{i}_Easy", 0) == 1;
            locations[i].isNormalCompleted = PlayerPrefs.GetInt($"Loc_{i}_Norm", 0) == 1;
            locations[i].isHardCompleted = PlayerPrefs.GetInt($"Loc_{i}_Hard", 0) == 1;
        }
    }

    public void InitializeDefaultConfigurationsIfEmpty()
    {
        if (locations.Count == 0)
        {
            // 1. Локация: Лавка Алхимика
            LocationConfig shop = new LocationConfig { locationId = "Shop", locationName = "Лавка Алхимика" };
            shop.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Easy, tierName = "Легкий", itemsPerRound = 5, roundsRequired = 3, timeLimitPerRound = 90f, rewardStones = 3, rewardScrolls = 1 });
            shop.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Normal, tierName = "Нормальный", itemsPerRound = 10, roundsRequired = 3, timeLimitPerRound = 90f, rewardStones = 6, rewardScrolls = 3, rewardExpPotion100 = 1 });
            shop.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Hard, tierName = "Сложный", itemsPerRound = 10, roundsRequired = 5, timeLimitPerRound = 90f, rewardStones = 15, rewardScrolls = 10, rewardExpPotion500 = 1, rewardMasteryPotion100 = 1 });
            locations.Add(shop);

            // 2. Локация: Дом Алхимика
            LocationConfig house = new LocationConfig { locationId = "House", locationName = "Дом Алхимика" };
            house.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Easy, tierName = "Легкий", itemsPerRound = 6, roundsRequired = 3, timeLimitPerRound = 90f, rewardStones = 5, rewardScrolls = 1 });
            house.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Normal, tierName = "Нормальный", itemsPerRound = 12, roundsRequired = 3, timeLimitPerRound = 90f, rewardStones = 9, rewardScrolls = 3, rewardExpPotion100 = 2 });
            house.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Hard, tierName = "Сложный", itemsPerRound = 15, roundsRequired = 5, timeLimitPerRound = 90f, rewardStones = 20, rewardScrolls = 12, rewardExpPotion500 = 2, rewardMasteryPotion100 = 1 });
            locations.Add(house);

            // 3. Локация: Магический Рынок
            LocationConfig market = new LocationConfig { locationId = "Market", locationName = "Магический Рынок" };
            market.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Easy, tierName = "Легкий", itemsPerRound = 6, roundsRequired = 3, timeLimitPerRound = 90f, rewardExpPotion100 = 3 });
            market.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Normal, tierName = "Нормальный", itemsPerRound = 12, roundsRequired = 3, timeLimitPerRound = 90f, rewardExpPotion100 = 6, rewardMasteryPotion100 = 3 });
            market.difficulties.Add(new DifficultyConfig { tier = DifficultyTier.Hard, tierName = "Сложный", itemsPerRound = 15, roundsRequired = 5, timeLimitPerRound = 90f, rewardExpPotion500 = 3, rewardMasteryPotion500 = 1 });
            locations.Add(market);
        }
    }
}
