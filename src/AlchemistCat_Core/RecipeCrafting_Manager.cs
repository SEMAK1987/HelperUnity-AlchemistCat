using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core v18.12.38)
/// Полный контроллер интерактивного крафта первого рецепта и перехода к Сундуку-Инвентарю:
/// 1. Панель Старого Свитка: отображение ресурсов (100 Золота + 5 Камней + 1 Свиток = Зелье), кнопка "Начать".
/// 2. Активация Котла на столе и сидящего Маленького Кота с диалоговой подсказкой.
/// 3. Появление плашки "Изготовить" над котлом при нажатии.
/// 4. 5-секундный процесс варки со шкалой прогресса и таймером обратного отсчета (5.0s -> 0.0s).
/// 5. Кнопка "Забрать" -> начисление +10 XP (10/10 XP -> Level Up до 2 Ур. 0/20 XP).
/// 6. Возврат к Основному Коту-Алхимику: поздравление, появление иконки Сундука слева от свитка.
/// 7. Рассказ про хранение предметов в сундуке (свитки, зелья, части пазлов/скинов с суммированием одинаковых в стаках).
/// 8. Кнопка "Открыть инвентарь" -> открытие окна Инвентаря с некликабельной (пока) кнопкой закрытия.
/// </summary>
public class RecipeCrafting_Manager : MonoBehaviour
{
    private static RecipeCrafting_Manager _instance;
    public static RecipeCrafting_Manager Instance
    {
        get
        {
            if (_instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                _instance = FindFirstObjectByType<RecipeCrafting_Manager>();
#else
                _instance = FindObjectOfType<RecipeCrafting_Manager>();
#endif
                if (_instance == null)
                {
                    RecipeCrafting_Manager[] all = Resources.FindObjectsOfTypeAll<RecipeCrafting_Manager>();
                    foreach (var m in all)
                    {
                        if (m != null && m.gameObject != null && m.gameObject.scene.isLoaded)
                        {
                            _instance = m;
                            if (!_instance.gameObject.activeSelf)
                            {
                                _instance.gameObject.SetActive(true);
                            }
                            break;
                        }
                    }
                }
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    [Header("0. Главная группа Стола и Котла")]
    public GameObject tableCauldronGroup;      // Родительская группа Table_Cauldron_Group

    [Header("1. Большой Свиток Рецепта")]
    public GameObject recipeScrollPanel;
    public Button startCraftButton;
    public TextMeshProUGUI startCraftButtonText;

    [Header("2. Стол, Котел и Маленький Кот")]
    public GameObject tableCauldronObject;     // Объект/Кнопка котла на столе
    public Button cauldronClickButton;
    public GameObject tableMiniCatObject;       // Маленький кот на столе
    public Button miniCatClickButton;           // Кнопка на маленьком котике для вызова подсказки
    public GameObject miniCatBubblePanel;       // Рамка-реплика маленького кота
    public TextMeshProUGUI miniCatBubbleText;

    [Header("3. Кнопка 'Изготовить' над Котлом")]
    public GameObject makeBadgeButtonObject;    // Плашка с надписью 'Изготовить'
    public Button makeBadgeButton;

    [Header("4. Шкала Прогресса Варки Зелья")]
    public GameObject craftingProgressBarContainer;
    public Image craftingProgressFill;
    public TextMeshProUGUI craftingTimerText;
    public float craftDurationSeconds = 5.0f;

    [Header("5. Кнопка 'Забрать' и Всплывающий Опыт (Floating XP)")]
    public GameObject claimPotionButtonObject;
    public Button claimPotionButton;
    public int firstRecipeRewardXP = 10;
    public GameObject floatingXPPrefab;          // Префаб/Объект всплывающей плашки опыта
    public RectTransform floatingXPSpawnPoint;   // Точка над котлом, откуда взлетает опыт
    public CanvasGroup floatingXPCanvasGroup;    // Для плавного исчезновения/осветления вверх

    [Header("Спрайты/Значки опыта для разных рецептов (5..1000 XP)")]
    public Image floatingXPImage;                // Иконка спрайта опыта
    public Sprite xpBadge5;
    public Sprite xpBadge10;
    public Sprite xpBadge20;
    public Sprite xpBadge30;
    public Sprite xpBadge50;
    public Sprite xpBadge100;
    public Sprite xpBadge200;
    public Sprite xpBadge300;
    public Sprite xpBadge500;
    public Sprite xpBadge1000;

    [Header("6. Иконка Сундука в верхнем UI")]
    public GameObject chestIconButton;         // Иконка сундучка (слева от свитка)
    public Button chestButton;

    [Header("7. Окно Инвентаря (100 слотов, 5 в ряд, со скроллом)")]
    public GameObject inventoryPanel;
    public Button inventoryCloseButton;        // Кнопка закрытия (крестик)
    public Transform inventorySlotsContent;    // Content внутри ScrollRect
    public GameObject inventorySlotPrefab;     // Префаб ячейки инвентаря
    public int totalSlots = 100;               // 100 ячеек
    public int columnsCount = 5;               // 5 в ряду

    [Header("8. Колба Опыта Мастерства в 1-м слоте")]
    public GameObject masteryPotionItemObject; // Иконка колбы опыта мастерства в первом слоте
    public Button masteryPotionButton;         // Кнопка на колбе для ее выпивания (+100 XP)
    public AudioClip potionConsumeSound;       // Звук применения колбы
    public bool isMasteryPotionConsumed = false;

    [Header("Звуки")]
    public AudioClip craftStartSound;
    public AudioClip craftCompleteSound;
    public AudioClip chestOpenSound;

    private Coroutine craftCoroutine;

    private void Awake()
    {
        Instance = this;

        AutoFindAndBindTableElements();

        if (startCraftButton != null)
        {
            startCraftButton.onClick.RemoveAllListeners();
            startCraftButton.onClick.AddListener(OnStartCraftButtonClicked);
        }

        if (cauldronClickButton != null)
        {
            cauldronClickButton.onClick.RemoveAllListeners();
            cauldronClickButton.onClick.AddListener(OnCauldronClicked);
        }

        if (miniCatClickButton != null)
        {
            miniCatClickButton.onClick.RemoveAllListeners();
            miniCatClickButton.onClick.AddListener(OnMiniCatClicked);
        }
        
        SanitizeMiniCatObject();

        if (makeBadgeButton != null)
        {
            makeBadgeButton.onClick.RemoveAllListeners();
            makeBadgeButton.onClick.AddListener(OnMakeBadgeClicked);
        }

        if (claimPotionButton != null)
        {
            claimPotionButton.onClick.RemoveAllListeners();
            claimPotionButton.onClick.AddListener(OnClaimPotionClicked);
        }

        if (chestButton != null)
        {
            chestButton.onClick.RemoveAllListeners();
            chestButton.onClick.AddListener(OnChestButtonClicked);
        }

        if (masteryPotionButton != null)
        {
            masteryPotionButton.onClick.RemoveAllListeners();
            masteryPotionButton.onClick.AddListener(OnMasteryPotionClicked);
        }

        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.onClick.RemoveAllListeners();
            inventoryCloseButton.onClick.AddListener(OnInventoryCloseClicked);
        }

        // По умолчанию вспомогательные плашки скрыты в чистом начальном состоянии
        if (makeBadgeButtonObject != null) makeBadgeButtonObject.SetActive(false);
        if (craftingProgressBarContainer != null) craftingProgressBarContainer.SetActive(false);
        if (claimPotionButtonObject != null) claimPotionButtonObject.SetActive(false);
        if (miniCatBubblePanel != null) miniCatBubblePanel.SetActive(false);
        if (floatingXPPrefab != null) floatingXPPrefab.SetActive(false);
        if (chestIconButton != null) chestIconButton.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    private void Start()
    {
        AutoFindAndBindTableElements();
    }

    public void AutoFindAndBindTableElements()
    {
        if (tableCauldronGroup == null)
        {
            // Ищем Table_Cauldron_Group среди всех Canvas и всех объектов сцены (включая неактивные)
#if UNITY_2023_1_OR_NEWER
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
#endif
            foreach (var canvas in allCanvases)
            {
                Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
                foreach (var child in children)
                {
                    if (child.name == "Table_Cauldron_Group" || child.name == "Table_Group" || child.name == "Table")
                    {
                        tableCauldronGroup = child.gameObject;
                        break;
                    }
                }
                if (tableCauldronGroup != null) break;
            }
        }

        if (tableCauldronGroup != null)
        {
            Transform[] children = tableCauldronGroup.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (tableCauldronObject == null && (child.name == "Cauldron_Button" || child.name == "Table_Cauldron" || child.name.Contains("Cauldron")))
                {
                    tableCauldronObject = child.gameObject;
                    if (cauldronClickButton == null) cauldronClickButton = child.GetComponent<Button>();
                }
                if (tableMiniCatObject == null && (child.name == "Mini_Cat_Image" || child.name == "Mini_Cat" || child.name == "Table_Mini_Cat"))
                {
                    tableMiniCatObject = child.gameObject;
                    if (miniCatClickButton == null) miniCatClickButton = child.GetComponent<Button>();
                }
                if (miniCatBubblePanel == null && (child.name == "Mini_Cat_Bubble" || child.name.Contains("Bubble")))
                {
                    miniCatBubblePanel = child.gameObject;
                    if (miniCatBubbleText == null) miniCatBubbleText = child.GetComponentInChildren<TextMeshProUGUI>(true);
                }
                if (makeBadgeButtonObject == null && (child.name == "Make_Badge_Button" || child.name.Contains("Make_Badge")))
                {
                    makeBadgeButtonObject = child.gameObject;
                    if (makeBadgeButton == null) makeBadgeButton = child.GetComponent<Button>();
                }
                if (craftingProgressBarContainer == null && (child.name == "Crafting_Progress_Bar" || child.name.Contains("Crafting_Progress")))
                {
                    craftingProgressBarContainer = child.gameObject;
                    if (craftingProgressFill == null)
                    {
                        Image[] imgs = child.GetComponentsInChildren<Image>(true);
                        foreach (var img in imgs)
                        {
                            if (img.type == Image.Type.Filled || img.name.Contains("Fill") || img.name.Contains("Bar"))
                            {
                                craftingProgressFill = img;
                                break;
                            }
                        }
                    }
                    if (craftingTimerText == null) craftingTimerText = child.GetComponentInChildren<TextMeshProUGUI>(true);
                }
                if (claimPotionButtonObject == null && (child.name == "Claim_Potion_Button" || child.name.Contains("Claim_Potion")))
                {
                    claimPotionButtonObject = child.gameObject;
                    if (claimPotionButton == null) claimPotionButton = child.GetComponent<Button>();
                }
                if (floatingXPPrefab == null && (child.name == "Floating_XP_Badge" || child.name.Contains("Floating_XP")))
                {
                    floatingXPPrefab = child.gameObject;
                    if (floatingXPSpawnPoint == null) floatingXPSpawnPoint = child.GetComponent<RectTransform>();
                    if (floatingXPCanvasGroup == null) floatingXPCanvasGroup = child.GetComponent<CanvasGroup>();
                    if (floatingXPImage == null) floatingXPImage = child.GetComponentInChildren<Image>(true);
                }
            }
        }

        // Автоматическая привязка кнопки выпивания Колбы Опыта Мастерства
        if (masteryPotionButton == null && masteryPotionItemObject != null)
        {
            masteryPotionButton = masteryPotionItemObject.GetComponent<Button>();
            if (masteryPotionButton == null)
            {
                masteryPotionButton = masteryPotionItemObject.AddComponent<Button>();
            }
            masteryPotionButton.onClick.RemoveAllListeners();
            masteryPotionButton.onClick.AddListener(OnMasteryPotionClicked);
        }
    }

    private Coroutine miniCatBubbleHideCoroutine;

    private void SanitizeMiniCatObject()
    {
        if (tableMiniCatObject != null)
        {
            // 1. Отключаем лишний дочерний GameObject "Button" с белым фоном, если он был создан
            Transform childBtn = tableMiniCatObject.transform.Find("Button");
            if (childBtn != null)
            {
                Image childImg = childBtn.GetComponent<Image>();
                if (childImg != null) childImg.enabled = false;
                TextMeshProUGUI childTmp = childBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (childTmp != null) childTmp.text = "";
                UnityEngine.UI.Text childTxt = childBtn.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (childTxt != null) childTxt.text = "";
                childBtn.gameObject.SetActive(false);
            }

            // 2. Включаем кликабельность на самом спрайте кота
            Image catImage = tableMiniCatObject.GetComponent<Image>();
            if (catImage != null)
            {
                catImage.raycastTarget = true;
            }

            Button catBtn = tableMiniCatObject.GetComponent<Button>();
            if (catBtn == null)
            {
                catBtn = tableMiniCatObject.AddComponent<Button>();
            }

            if (catImage != null)
            {
                catBtn.targetGraphic = catImage;
            }

            catBtn.onClick.RemoveAllListeners();
            catBtn.onClick.AddListener(OnMiniCatClicked);
        }
    }

    /// <summary>
    /// Шаг 1: Игрок открыл Старый Свиток и нажал кнопку 'Начать' внизу свитка (Скриншот 4 -> Скриншот 3)
    /// </summary>
    public void OnStartCraftButtonClicked()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(craftStartSound);

        if (recipeScrollPanel != null) recipeScrollPanel.SetActive(false);

        // Панель ресурсов, аватарка, календарь и свиток остаются видимыми, но БЛОКИРУЮТСЯ (заблокированы) на время варки
        if (DialogueSystem_Manager.Instance != null)
        {
            if (DialogueSystem_Manager.Instance.topPanel != null) 
                DialogueSystem_Manager.Instance.topPanel.SetActive(true);
            if (DialogueSystem_Manager.Instance.calendarIconButton != null) 
                DialogueSystem_Manager.Instance.calendarIconButton.SetActive(true);
            if (DialogueSystem_Manager.Instance.playerAvatarContainer != null) 
                DialogueSystem_Manager.Instance.playerAvatarContainer.SetActive(true);
            if (DialogueSystem_Manager.Instance.smallScrollIconButton != null) 
                DialogueSystem_Manager.Instance.smallScrollIconButton.SetActive(true);

            DialogueSystem_Manager.Instance.isCraftingInProgress = true;
            DialogueSystem_Manager.Instance.SetCalendarButtonInteractable(false);
            DialogueSystem_Manager.Instance.SetSmallScrollInteractable(false);
        }

        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.SetAvatarButtonInteractable(false);
        }

        // Автоматически находим и привязываем всю группу стола и котла
        AutoFindAndBindTableElements();

        if (tableCauldronGroup != null)
        {
            tableCauldronGroup.SetActive(true);
        }

        // Появляется котел и маленький кот на столе (включая родительскую группу Table_Cauldron_Group при наличии)
        if (tableCauldronObject != null) 
        {
            tableCauldronObject.SetActive(true);
            if (tableCauldronObject.transform.parent != null && (tableCauldronObject.transform.parent.name.Contains("Table") || tableCauldronObject.transform.parent.name.Contains("Cauldron")))
            {
                tableCauldronObject.transform.parent.gameObject.SetActive(true);
            }
        }
        else
        {
            GameObject foundTable = GameObject.Find("Table_Cauldron_Group");
            if (foundTable == null) foundTable = GameObject.Find("Table_Group");
            if (foundTable == null) foundTable = GameObject.Find("Table");
            if (foundTable != null)
            {
                foundTable.SetActive(true);
                tableCauldronObject = foundTable;
            }
        }

        // Если есть cauldronClickButton, убедимся что клик назначен
        if (cauldronClickButton != null)
        {
            cauldronClickButton.onClick.RemoveAllListeners();
            cauldronClickButton.onClick.AddListener(OnCauldronClicked);
        }
        else if (tableCauldronObject != null)
        {
            Button btn = tableCauldronObject.GetComponentInChildren<Button>(true);
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnCauldronClicked);
            }
        }

        if (tableMiniCatObject != null)
        {
            tableMiniCatObject.SetActive(true);
            SanitizeMiniCatObject();
        }
        else
        {
            GameObject foundMiniCat = GameObject.Find("Mini_Cat");
            if (foundMiniCat == null) foundMiniCat = GameObject.Find("Table_Mini_Cat");
            if (foundMiniCat != null)
            {
                foundMiniCat.SetActive(true);
                tableMiniCatObject = foundMiniCat;
                SanitizeMiniCatObject();
            }
        }

        // Показываем бабл с подсказкой котика на столе
        ShowMiniCatBubble("Нажми на котёл, чтобы начать варить!", false);
    }

    /// <summary>
    /// Клик по маленькому коту на столе — переключает/показывает облачко с подсказкой
    /// </summary>
    public void OnMiniCatClicked()
    {
        if (miniCatBubblePanel != null)
        {
            bool isCurrentActive = miniCatBubblePanel.activeSelf;
            if (isCurrentActive)
            {
                HideMiniCatBubble();
            }
            else
            {
                ShowMiniCatBubble("Нажми на котёл, чтобы начать варить!", false);
            }
        }
    }

    public void ShowMiniCatBubble(string text, bool autoHide = false)
    {
        if (miniCatBubblePanel != null)
        {
            miniCatBubblePanel.SetActive(true);
            if (miniCatBubbleText != null)
            {
                miniCatBubbleText.text = text;
            }

            if (miniCatBubbleHideCoroutine != null) StopCoroutine(miniCatBubbleHideCoroutine);
            if (autoHide)
            {
                miniCatBubbleHideCoroutine = StartCoroutine(AutoHideMiniCatBubbleRoutine(3.5f));
            }
        }
    }

    public void HideMiniCatBubble()
    {
        if (miniCatBubbleHideCoroutine != null)
        {
            StopCoroutine(miniCatBubbleHideCoroutine);
            miniCatBubbleHideCoroutine = null;
        }
        if (miniCatBubblePanel != null)
        {
            miniCatBubblePanel.SetActive(false);
        }
    }

    private IEnumerator AutoHideMiniCatBubbleRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (miniCatBubblePanel != null)
        {
            miniCatBubblePanel.SetActive(false);
        }
        miniCatBubbleHideCoroutine = null;
    }

    /// <summary>
    /// Шаг 2: Нажатие на котел на столе -> скрывается облачко кота, появляется плашка 'Изготовить'
    /// </summary>
    public void OnCauldronClicked()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(craftStartSound);

        HideMiniCatBubble();
        if (makeBadgeButtonObject != null) makeBadgeButtonObject.SetActive(true);
    }

    /// <summary>
    /// Шаг 3: Нажатие на 'Изготовить' -> запуск шкалы варки на 5 секунд
    /// </summary>
    public void OnMakeBadgeClicked()
    {
        if (makeBadgeButtonObject != null) makeBadgeButtonObject.SetActive(false);

        if (craftCoroutine != null) StopCoroutine(craftCoroutine);
        craftCoroutine = StartCoroutine(CraftingProgressRoutine());
    }

    private IEnumerator CraftingProgressRoutine()
    {
        if (craftingProgressBarContainer != null) craftingProgressBarContainer.SetActive(true);
        if (craftingProgressFill != null) craftingProgressFill.fillAmount = 0f;

        float elapsed = 0f;
        while (elapsed < craftDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / craftDurationSeconds);
            if (craftingProgressFill != null) craftingProgressFill.fillAmount = ratio;

            float remaining = Mathf.Max(0f, craftDurationSeconds - elapsed);
            if (craftingTimerText != null) craftingTimerText.text = $"{remaining:F1}s";

            yield return null;
        }

        if (craftingProgressFill != null) craftingProgressFill.fillAmount = 1f;
        if (craftingTimerText != null) craftingTimerText.text = "0.0s";

        yield return new WaitForSeconds(0.2f);

        if (craftingProgressBarContainer != null) craftingProgressBarContainer.SetActive(false);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(craftCompleteSound);

        // Появляется кнопка 'Забрать'
        if (claimPotionButtonObject != null) claimPotionButtonObject.SetActive(true);
    }

    /// <summary>
    /// Шаг 4: Нажатие 'Забрать' -> скрытие кнопки, плавный вылет кружка опыта (+10 XP) вверх, затем скрытие котла и начисление XP
    /// </summary>
    public void OnClaimPotionClicked()
    {
        if (claimPotionButtonObject != null) claimPotionButtonObject.SetActive(false);
        if (miniCatBubblePanel != null) miniCatBubblePanel.SetActive(false);

        StartCoroutine(FlyFloatingXPAndProceed(firstRecipeRewardXP));
    }

    private IEnumerator FlyFloatingXPAndProceed(int xpAmount)
    {
        GameObject activeFloatingXP = floatingXPPrefab;
        bool isDynamic = false;

        // Если префаб/объект не привязан в инспекторе, создаем красивый динамический кружок опыта
        if (activeFloatingXP == null)
        {
            Transform parentCanvas = transform;
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null) parentCanvas = rootCanvas.transform;

            activeFloatingXP = new GameObject("Dynamic_Floating_XP", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            activeFloatingXP.transform.SetParent(parentCanvas, false);
            isDynamic = true;

            RectTransform drt = activeFloatingXP.GetComponent<RectTransform>();
            drt.sizeDelta = new Vector2(76f, 76f);
            drt.anchorMin = new Vector2(0.5f, 0.5f);
            drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);

            Vector2 spawn = Vector2.zero;
            if (floatingXPSpawnPoint != null)
                spawn = floatingXPSpawnPoint.anchoredPosition;
            else if (tableCauldronObject != null)
            {
                RectTransform crt = tableCauldronObject.GetComponent<RectTransform>();
                if (crt != null) spawn = crt.anchoredPosition + new Vector2(0f, 60f);
            }
            drt.anchoredPosition = spawn;

            Image dImg = activeFloatingXP.GetComponent<Image>();
            Sprite s = GetSpriteForXp(xpAmount);
            if (s != null)
            {
                dImg.sprite = s;
            }
            else
            {
                dImg.color = new Color(0.2f, 0.85f, 0.4f, 1f);
            }

            // Добавляем красивый текст внутри
            GameObject textObj = new GameObject("XP_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(activeFloatingXP.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
            txt.text = $"+{xpAmount} XP";
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 20f;
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;
        }

        if (activeFloatingXP != null)
        {
            activeFloatingXP.SetActive(true);

            Image targetImg = floatingXPImage;
            if (targetImg == null)
            {
                targetImg = activeFloatingXP.GetComponent<Image>();
                if (targetImg == null) targetImg = activeFloatingXP.GetComponentInChildren<Image>(true);
            }

            if (targetImg != null)
            {
                targetImg.preserveAspect = true;
                if (floatingXPImage != null)
                {
                    Sprite chosenSprite = GetSpriteForXp(xpAmount);
                    if (chosenSprite != null) targetImg.sprite = chosenSprite;
                }
            }

            RectTransform rt = activeFloatingXP.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Сохраняем исходные размеры объекта, настроенные пользователем в Inspector
                rt.localScale = Vector3.one;
            }

            CanvasGroup cg = activeFloatingXP.GetComponent<CanvasGroup>();
            if (cg == null) cg = activeFloatingXP.AddComponent<CanvasGroup>();

            Vector2 startPos = rt != null ? rt.anchoredPosition : Vector2.zero;
            if (floatingXPSpawnPoint != null) startPos = floatingXPSpawnPoint.anchoredPosition;
            if (rt != null) rt.anchoredPosition = startPos;

            float duration = 1.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Плавный взлет вверх с замедлением к концу
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (rt != null)
                {
                    rt.anchoredPosition = startPos + new Vector2(0f, easeOut * 150f);
                    float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
                    rt.localScale = new Vector3(scale, scale, 1f);
                }

                if (cg != null)
                {
                    cg.alpha = (t > 0.6f) ? Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f) : 1f;
                }

                yield return null;
            }

            if (isDynamic)
            {
                Destroy(activeFloatingXP);
            }
            else
            {
                activeFloatingXP.SetActive(false);
                if (cg != null) cg.alpha = 1f;
                if (rt != null) rt.anchoredPosition = startPos;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        // Скрываем котел и помощника только после завершения полета кружка опыта
        if (tableCauldronObject != null) tableCauldronObject.SetActive(false);
        if (tableMiniCatObject != null) tableMiniCatObject.SetActive(false);

        // Начисление опыта в профиль (10/10 XP -> повышает уровень до 2 Ур. 0/20 XP)
        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.AddExperience(xpAmount);
        }

        // Запуск финальной фазы диалога с основным котом
        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.isCraftingInProgress = false;
            DialogueSystem_Manager.Instance.StartPostCraftChestDialogue();
        }
    }

    private Sprite GetSpriteForXp(int xp)
    {
        switch (xp)
        {
            case 5: return xpBadge5;
            case 10: return xpBadge10;
            case 20: return xpBadge20;
            case 30: return xpBadge30;
            case 50: return xpBadge50;
            case 100: return xpBadge100;
            case 200: return xpBadge200;
            case 300: return xpBadge300;
            case 500: return xpBadge500;
            case 1000: return xpBadge1000;
            default: return xpBadge10;
        }
    }

    /// <summary>
    /// Шаг 5: Нажатие на сундучок в верхнем левом меню
    /// </summary>
    public void OnChestButtonClicked()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(chestOpenSound);

        OpenInventory();
    }

    [ContextMenu("Сбросить Прогресс Крафта и Сундука (Reset Crafting & Chest)")]
    public void ResetCraftingAndChestProgress()
    {
        PlayerPrefs.DeleteKey("Mastery_Flask_Consumed");
        PlayerPrefs.DeleteKey("First_Recipe_Done");
        PlayerPrefs.DeleteKey("Tutorial_Recipe_Done");
        PlayerPrefs.Save();
        isMasteryPotionConsumed = false;
        if (masteryPotionItemObject != null) masteryPotionItemObject.SetActive(true);
        if (inventoryCloseButton != null) inventoryCloseButton.interactable = false;
        Debug.Log("[RecipeCrafting_Manager] Прогресс крафта и колбы сундука успешно сброшен!");
    }

    public void OpenInventory()
    {
        if (DialogueSystem_Manager.Instance != null && DialogueSystem_Manager.Instance.dialoguePanel != null)
        {
            DialogueSystem_Manager.Instance.dialoguePanel.SetActive(false);
        }

        // Скрываем верхнюю панель ресурсов, чтобы не накладывалась на заголовок сундука
        HideTopResources();

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            SanitizeInventoryTitle();
        }

        EnsureInventorySlots();

        // Проверяем, выпита ли колба опыта мастерства
        isMasteryPotionConsumed = PlayerPrefs.GetInt("Mastery_Flask_Consumed", 0) == 1;

        // Проверяем/привязываем колбу в 1-м слоте
        EnsureMasteryPotionInFirstSlot();

        if (masteryPotionItemObject != null)
        {
            masteryPotionItemObject.SetActive(!isMasteryPotionConsumed);
        }

        // Если колба еще не выпита - блокируем крестик закрытия. Если выпита - разблокируем
        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.interactable = isMasteryPotionConsumed;
        }
    }

    private void HideTopResources()
    {
        GameObject topPanel = GameObject.Find("TopPanel");
        if (topPanel != null) topPanel.SetActive(false);
        GameObject headerPlate = GameObject.Find("Header_Plate");
        if (headerPlate != null) headerPlate.SetActive(false);
    }

    private void RestoreTopResources()
    {
        GameObject topPanel = GameObject.Find("TopPanel");
        if (topPanel != null) topPanel.SetActive(true);
        GameObject headerPlate = GameObject.Find("Header_Plate");
        if (headerPlate != null) headerPlate.SetActive(true);
    }

    private void SanitizeInventoryTitle()
    {
        if (inventoryPanel == null) return;
        TextMeshProUGUI[] tmps = inventoryPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmps)
        {
            if (t.name.Contains("Title") || t.text.Contains("Инвентарь") || t.text.Contains("Сундук"))
            {
                t.text = t.text.Replace("(Инвентарь)", "").Replace("Инвентарь", "").Trim();
                if (string.IsNullOrEmpty(t.text)) t.text = "Сундук Алхимика";
                t.alignment = TextAlignmentOptions.Center;

                RectTransform rt = t.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -22f);
                }
            }
        }
        UnityEngine.UI.Text[] texts = inventoryPanel.GetComponentsInChildren<UnityEngine.UI.Text>(true);
        foreach (var t in texts)
        {
            if (t.name.Contains("Title") || t.text.Contains("Инвентарь") || t.text.Contains("Сундук"))
            {
                t.text = t.text.Replace("(Инвентарь)", "").Replace("Инвентарь", "").Trim();
                if (string.IsNullOrEmpty(t.text)) t.text = "Сундук Алхимика";
                t.alignment = TextAnchor.MiddleCenter;

                RectTransform rt = t.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -22f);
                }
            }
        }
    }

    private void EnsureMasteryPotionInFirstSlot()
    {
        if (inventorySlotsContent != null && inventorySlotsContent.childCount > 0)
        {
            Transform firstSlot = inventorySlotsContent.GetChild(0);

            if (masteryPotionItemObject == null)
            {
                // Ищем существующий объект колбы внутри 1-го слота
                Transform found = firstSlot.Find("Mastery_Potion_Flask");
                if (found == null) found = firstSlot.Find("Potion_Item");
                if (found == null) found = firstSlot.Find("Flask");

                if (found != null)
                {
                    masteryPotionItemObject = found.gameObject;
                }
                else
                {
                    // Создаем плашку колбы опыта мастерства в первом слоте
                    GameObject flaskObj = new GameObject("Mastery_Potion_Flask", typeof(RectTransform), typeof(Image), typeof(Button));
                    flaskObj.transform.SetParent(firstSlot, false);

                    RectTransform frt = flaskObj.GetComponent<RectTransform>();
                    frt.anchorMin = Vector2.zero;
                    frt.anchorMax = Vector2.one;
                    frt.offsetMin = new Vector2(8, 8);
                    frt.offsetMax = new Vector2(-8, -8);

                    Image fImg = flaskObj.GetComponent<Image>();
                    if (xpBadge100 != null)
                    {
                        fImg.sprite = xpBadge100;
                    }
                    else if (floatingXPImage != null && floatingXPImage.sprite != null)
                    {
                        fImg.sprite = floatingXPImage.sprite;
                    }
                    else
                    {
                        fImg.color = new Color(0.2f, 0.9f, 0.6f, 0.95f);
                    }

                    masteryPotionItemObject = flaskObj;
                }
            }

            if (masteryPotionItemObject != null)
            {
                masteryPotionButton = masteryPotionItemObject.GetComponent<Button>();
                if (masteryPotionButton == null) masteryPotionButton = masteryPotionItemObject.AddComponent<Button>();
                masteryPotionButton.onClick.RemoveAllListeners();
                masteryPotionButton.onClick.AddListener(OnMasteryPotionClicked);
            }
        }
    }

    /// <summary>
    /// Автоматическая генерация или проверка 100 ячеек инвентаря
    /// </summary>
    public void EnsureInventorySlots()
    {
        if (inventorySlotsContent == null) return;

        // Настраиваем сетку (GridLayoutGroup) на 5 колонок если компонент есть
        GridLayoutGroup grid = inventorySlotsContent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columnsCount;
        }

        // Если в инвентаре уже есть дочерние слоты и их меньше 100, дополняем при наличии префаба или клонируя первый слот
        if (inventorySlotPrefab != null)
        {
            int currentChildCount = inventorySlotsContent.childCount;
            for (int i = currentChildCount; i < totalSlots; i++)
            {
                GameObject newSlot = Instantiate(inventorySlotPrefab, inventorySlotsContent);
                newSlot.name = $"Inventory_Slot_Hex_{i + 1}";
            }
        }
        else if (inventorySlotsContent.childCount > 0 && inventorySlotsContent.childCount < totalSlots)
        {
            GameObject template = inventorySlotsContent.GetChild(0).gameObject;
            int currentChildCount = inventorySlotsContent.childCount;
            for (int i = currentChildCount; i < totalSlots; i++)
            {
                GameObject newSlot = Instantiate(template, inventorySlotsContent);
                newSlot.name = $"Inventory_Slot_Hex_{i + 1}";
            }
        }
    }

    /// <summary>
    /// Шаг 6: Игрок нажимает на Колбу Опыта Мастерства в 1-м слоте (+100 XP)
    /// </summary>
    public void OnMasteryPotionClicked()
    {
        if (isMasteryPotionConsumed) return;
        isMasteryPotionConsumed = true;

        if (potionConsumeSound != null && SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(potionConsumeSound);
        else if (craftCompleteSound != null && SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(craftCompleteSound);

        // Колба пропадает из слота
        if (masteryPotionItemObject != null)
        {
            masteryPotionItemObject.SetActive(false);
        }

        // Начисляем 100 опыта мастерства (переход с Новичка на Новичок-травник)
        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.AddMasteryExperience(100);
        }

        // Активируем кнопку-крестик выхода из инвентаря
        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.interactable = true;
        }
    }

    /// <summary>
    /// Шаг 7: Игрок нажимает крестик выхода из инвентаря -> блокировка сундука, переход к диалогу о Знаниях
    /// </summary>
    public void OnInventoryCloseClicked()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        // Восстанавливаем отображение верхней панели ресурсов
        RestoreTopResources();

        if (DialogueSystem_Manager.Instance != null)
        {
            // Блокируем сундук
            DialogueSystem_Manager.Instance.SetChestButtonInteractable(false);

            // Запускаем диалог про раздел 'Знания' и повышение до 'Новичок-травник'
            DialogueSystem_Manager.Instance.StartPostMasteryKnowledgeDialogue();
        }
    }

    /// <summary>
    /// Добавление зелья или предмета в первый свободный слот инвентаря
    /// </summary>
    public void AddPotionToFirstEmptySlot(string potionId, string potionTitle)
    {
        int currentCount = PlayerPrefs.GetInt($"Item_Count_{potionId}", 0);
        PlayerPrefs.SetInt($"Item_Count_{potionId}", currentCount + 1);
        PlayerPrefs.SetString($"Item_Name_{potionId}", potionTitle);
        PlayerPrefs.Save();
        Debug.Log($"[INVENTORY] Награда {potionTitle} ({potionId}) успешно добавлена в инвентарь (Количество: {currentCount + 1}).");
    }
}
