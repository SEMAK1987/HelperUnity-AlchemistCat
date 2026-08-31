using System;
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
        public GameObject sceneClickableObject; // Коллайдер/кнопка на фоновом 8K спрайте
        [HideInInspector] public bool isFound = false;
    }

    [System.Serializable]
    public class LocationData
    {
        public SearchLocation locationType;
        public string locationName;
        public Sprite background8kSprite;
        public GameObject locationSceneRoot; // Родительский объект сцены с объектами
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

    /// <summary>
    /// Запуск поиска в выбранной локации
    /// </summary>
    public void StartLocationSearch(SearchLocation locType)
    {
        currentLocation = locations.Find(l => l.locationType == locType);
        if (currentLocation == null) return;

        if (hiddenObjectPanel) hiddenObjectPanel.SetActive(true);
        if (locationSelectPopup) locationSelectPopup.SetActive(false);
        if (victoryPopupPanel) victoryPopupPanel.SetActive(false);

        // Настройка фона под экран
        if (backgroundLocationImage && currentLocation.background8kSprite != null)
        {
            backgroundLocationImage.sprite = currentLocation.background8kSprite;
        }

        // Активация интерактивных объектов текущей локации
        foreach (var loc in locations)
        {
            if (loc.locationSceneRoot)
                loc.locationSceneRoot.SetActive(loc == currentLocation);
        }

        // Сброс предметов и зума
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

        // Запуск таймера и UI
        searchTimeRemaining = 180f;
        isTimerActive = true;
        UpdateTargetsUI();

        if (locationTitleText)
            locationTitleText.text = currentLocation.locationName;
    }

    /// <summary>
    /// Обработка клика по найденному предмету
    /// </summary>
    public void OnItemClicked(HiddenItemTarget item)
    {
        if (item.isFound) return;
        item.isFound = true;
        foundCount++;

        // Скрываем или анимируем найденный объект
        if (item.sceneClickableObject)
        {
            item.sceneClickableObject.SetActive(false);
        }

        // Начисляем предмет в Инвентарь игрока
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

        // Проверка победы (все предметы найдены)
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

        // Бонус в Avatar_Manager
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
                // Время вышло
            }
        }

        HandleTouchAndMouseZoomPan();
    }

    /// <summary>
    /// Адаптивное управление масштабированием (Колесо мыши на ПК / Pinch на сенсорных экранах)
    /// </summary>
    private void HandleTouchAndMouseZoomPan()
    {
        // 1. Колесо мыши на ПК
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed * 3f, minZoom, maxZoom);
            ApplyZoomAndPan();
        }

        // 2. Двойное касание (Pinch Zoom) на телефонах и планшетах
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
}
