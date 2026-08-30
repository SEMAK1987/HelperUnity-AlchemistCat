using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core v18.12.44)
/// Менеджер Окна 'Знания' (Древо Рангов и Прокачки Алхимического Мастерства):
/// - 4 Этапа и 21 Ранг мастерства
/// - Иконка сложенных книг (Knowledge_Icon_Button) слева от Сундука с увеличенным отступом
/// - Полноценный ScrollView с автоматической генерацией карточек всех 21 рангов и 4 этапов
/// - Автоматическое скрытие верхней панели ресурсов во время просмотра
/// </summary>
public class Knowledge_Manager : MonoBehaviour
{
    public static Knowledge_Manager Instance { get; private set; }

    [Header("UI Панель Знаний и Рангов")]
    public GameObject knowledgePanel;
    public TextMeshProUGUI titleText;           // Заголовок панели (Ранги Алхимии)
    public Button knowledgeCloseButton;
    public ScrollRect knowledgeScrollView;
    public Transform knowledgeContent;
    public bool isKnowledgeCompleted = false;

    [Header("Иконка Книг Знаний в верхнем UI")]
    public GameObject knowledgeIconButton; // Иконка сложенных книг слева от сундука
    public Button knowledgeButton;
    public bool autoAlignKnowledgeToChest = true;
    public Vector2 knowledgeOffsetFromChest = new Vector2(-135f, 0f);

    [Header("Верхняя панель ресурсов (скрывается при открытии)")]
    public GameObject topResourcesPanel;

    [Header("Звуки")]
    public AudioClip openKnowledgeSound;
    public AudioClip closeSound;
    public AudioClip unlockSound;

    [System.Serializable]
    public class AlchemyRankInfo
    {
        public int rankIndex;
        public string stageNameRU;
        public string rankNameRU;
        public string rankNameEN;
        public string rankNameTR;
        public int requiredMasteryExp;
        public Color rankTextColor = Color.white;
        public string rankDescriptionRU;
    }

    [Header("Список 21 Рангов Мастерства (4 Этапа)")]
    public List<AlchemyRankInfo> allRanks = new List<AlchemyRankInfo>();

    private void Awake()
    {
        Instance = this;

        if (knowledgeCloseButton != null)
        {
            knowledgeCloseButton.onClick.RemoveAllListeners();
            knowledgeCloseButton.onClick.AddListener(CloseKnowledgeUI);
        }

        if (knowledgeButton != null)
        {
            knowledgeButton.onClick.RemoveAllListeners();
            knowledgeButton.onClick.AddListener(OnKnowledgeButtonClicked);
        }

        if (knowledgeScrollView != null)
        {
            knowledgeScrollView.onValueChanged.RemoveAllListeners();
            knowledgeScrollView.onValueChanged.AddListener(OnScrollValueChanged);
        }

        InitDefaultRanks();
    }

    private void Start()
    {
        if (knowledgePanel != null) knowledgePanel.SetActive(false);
        if (knowledgeIconButton != null) knowledgeIconButton.SetActive(false);
    }

    public void InitDefaultRanks()
    {
        if (allRanks.Count > 0) return;

        // Этап I: Ученик (Ранги 1–6)
        AddRank(1, "Этап I: Ученик — Основы и базовые экстракты", "Новичок", "Novice", "Acemi", 100, Color.white, "Начало пути в алхимической лаборатории.");
        AddRank(2, "Этап I: Ученик — Основы и базовые экстракты", "Новичок-травник", "Herbalist Novice", "Bitkici Acemi", 300, new Color(0.32f, 0.75f, 0.50f, 1f), "Сбор целебных трав и приготовление базовых отваров.");
        AddRank(3, "Этап I: Ученик — Основы и базовые экстракты", "Подмастерье угля", "Coal Apprentice", "Komur Ciragi", 500, new Color(0.40f, 0.80f, 0.60f, 1f), "Контроль жара котла и очистка минеральных углей.");
        AddRank(4, "Этап I: Ученик — Основы и базовые экстракты", "Экстрактор", "Extractor", "Ekstraktor", 1000, new Color(0.45f, 0.85f, 0.70f, 1f), "Выделение чистых соков и эссенций из редких растений.");
        AddRank(5, "Этап I: Ученик — Основы и базовые экстракты", "Знаток пропорций", "Proportion Master", "Oran Ustasi", 1500, new Color(0.50f, 0.90f, 0.80f, 1f), "Идеальное соблюдение дозировок без риска взрыва.");
        AddRank(6, "Этап I: Ученик — Основы и базовые экстракты", "Сертифицированный ученик", "Certified Apprentice", "Sertifikali Cirak", 3000, new Color(0.55f, 0.95f, 0.90f, 1f), "Завершение базового обучения и допуск к сложным реактивам.");

        // Этап II: Адепт (Ранги 7–11)
        AddRank(7, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Практик масел", "Oil Practitioner", "Yag Uygulayicisi", 5000, new Color(0.30f, 0.70f, 1f, 1f), "Варка густых эфирных масел и настоек длительного действия.");
        AddRank(8, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Дистиллятор", "Distiller", "Damitici", 7000, new Color(0.35f, 0.75f, 1f, 1f), "Многоступенчатая перегонка редких магических спиртов.");
        AddRank(9, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Мастер ферментации", "Fermentation Master", "Mayalama Ustasi", 10000, new Color(0.40f, 0.80f, 1f, 1f), "Ускорение биологических реакций лунными дрожжами.");
        AddRank(10, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Каталитический химик", "Catalytic Chemist", "Katalitik Kimyager", 15000, new Color(0.50f, 0.85f, 1f, 1f), "Использование катализаторов для синтеза редких минералов.");
        AddRank(11, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Старший фармацевт", "Senior Pharmacist", "Kidemli Eczaci", 20000, new Color(0.60f, 0.90f, 1f, 1f), "Создание сильнодействующих лекарств и противоядий.");

        // Этап III: Магистр (Ранги 12–16)
        AddRank(12, "Этап III: Магистр — Эфир, пустота и кристаллы", "Эфирный экспериментатор", "Aether Experimenter", "Eter Deneycisi", 25000, new Color(0.80f, 0.50f, 1f, 1f), "Улавливание невидимых эфирных потоков в стеклянные сосуды.");
        AddRank(13, "Этап III: Магистр — Эфир, пустота и кристаллы", "Кристаллограф", "Crystallographer", "Kristalograft", 30000, new Color(0.85f, 0.55f, 1f, 1f), "Выращивание кристаллов маны идеальной геометрической формы.");
        AddRank(14, "Этап III: Магистр — Эфир, пустота и кристаллы", "Мастер трансмутации", "Transmutation Master", "Donusum Ustasi", 37000, new Color(0.90f, 0.60f, 1f, 1f), "Превращение свинца в медь и серебро силой мысли и тепла.");
        AddRank(15, "Этап III: Магистр — Эфир, пустота и кристаллы", "Вивисектор сущностей", "Essence Vivisector", "Oz Kasifi", 45000, new Color(0.95f, 0.65f, 1f, 1f), "Разделение духовной и физической материи компонентов.");
        AddRank(16, "Этап III: Магистр — Эфир, пустота и кристаллы", "Архимагистр рецептуры", "Archmagister of Formulas", "Formul Basbuyucusu", 60000, new Color(1f, 0.70f, 0.95f, 1f), "Создание собственных уникальных формул для великих свитков.");

        // Этап IV: Великий Алхимик (Ранги 17–21)
        AddRank(17, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Повелитель температур", "Lord of Temperatures", "Sicaklik Efendisi", 70000, new Color(1f, 0.80f, 0.30f, 1f), "Контроль абсолютного нуля и пламени феникса.");
        AddRank(18, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Ткач реальности", "Reality Weaver", "Gerceklik Dokuyucusu", 85000, new Color(1f, 0.85f, 0.35f, 1f), "Изменение физических свойств пространства вокруг котла.");
        AddRank(19, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Конструктор душ", "Soul Constructor", "Ruh Yapicisi", 120000, new Color(1f, 0.90f, 0.40f, 1f), "Вдохновение жизни в гомункулов и волшебных стражей.");
        AddRank(20, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Хранитель Первоматерии", "Keeper of Prima Materia", "Ilk Maddenin Bekcisi", 200000, new Color(1f, 0.95f, 0.50f, 1f), "Владение изначальной субстанцией творения Вселенной.");
        AddRank(21, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Создатель Философского камня", "Creator of Philosopher's Stone", "Felsefe Tasi Yaraticisi", 500000, new Color(1f, 0.98f, 0.60f, 1f), "Вершина мастерства: вечная жизнь и бесконечное золото.");
    }

    private void AddRank(int idx, string stage, string ru, string en, string tr, int exp, Color col, string desc = "")
    {
        allRanks.Add(new AlchemyRankInfo
        {
            rankIndex = idx,
            stageNameRU = stage,
            rankNameRU = ru,
            rankNameEN = en,
            rankNameTR = tr,
            requiredMasteryExp = exp,
            rankTextColor = col,
            rankDescriptionRU = desc
        });
    }

    public void AlignKnowledgeButtonToChest()
    {
        if (!autoAlignKnowledgeToChest) return;
        if (knowledgeIconButton != null)
        {
            GameObject refObj = null;
            if (DialogueSystem_Manager.Instance != null)
            {
                if (DialogueSystem_Manager.Instance.chestIconButton != null) refObj = DialogueSystem_Manager.Instance.chestIconButton;
                else if (DialogueSystem_Manager.Instance.smallScrollIconButton != null) refObj = DialogueSystem_Manager.Instance.smallScrollIconButton;
                else if (DialogueSystem_Manager.Instance.calendarIconButton != null) refObj = DialogueSystem_Manager.Instance.calendarIconButton;
            }

            if (refObj != null)
            {
                RectTransform refRect = refObj.GetComponent<RectTransform>();
                RectTransform knowRect = knowledgeIconButton.GetComponent<RectTransform>();
                if (refRect != null && knowRect != null)
                {
                    if (knowledgeIconButton.transform.parent != refObj.transform.parent)
                    {
                        knowledgeIconButton.transform.SetParent(refObj.transform.parent, false);
                    }

                    knowRect.anchorMin = refRect.anchorMin;
                    knowRect.anchorMax = refRect.anchorMax;
                    knowRect.pivot = refRect.pivot;
                    knowRect.sizeDelta = refRect.sizeDelta;

                    // Если отступ был маленьким, задаем безопасные -135 пикселей
                    Vector2 actualOffset = knowledgeOffsetFromChest;
                    if (actualOffset.x > -125f) actualOffset.x = -135f;

                    knowRect.anchoredPosition = refRect.anchoredPosition + actualOffset;
                }
            }

            // Очищаем стандартный текст "Button"
            TextMeshProUGUI[] tmps = knowledgeIconButton.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps) t.text = "";
            UnityEngine.UI.Text[] texts = knowledgeIconButton.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var t in texts) t.text = "";
        }
    }

    public void SetKnowledgeButtonInteractable(bool interactable)
    {
        if (knowledgeButton != null)
        {
            knowledgeButton.interactable = interactable;
        }
        else if (knowledgeIconButton != null)
        {
            Button btn = knowledgeIconButton.GetComponent<Button>();
            if (btn != null) btn.interactable = interactable;
        }
    }

    public void OnKnowledgeButtonClicked()
    {
        if (DialogueSystem_Manager.Instance != null && 
            (DialogueSystem_Manager.Instance.isCraftingInProgress || 
            (DialogueSystem_Manager.Instance.dialoguePanel != null && DialogueSystem_Manager.Instance.dialoguePanel.activeSelf)))
        {
            Debug.Log("[KNOWLEDGE] Знания заблокированы во время диалога Кота.");
            return;
        }

        OpenKnowledgeUI();
    }

    public void OpenKnowledgeUI()
    {
        if (openKnowledgeSound != null && SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(openKnowledgeSound);

        if (DialogueSystem_Manager.Instance != null && DialogueSystem_Manager.Instance.dialoguePanel != null)
        {
            DialogueSystem_Manager.Instance.dialoguePanel.SetActive(false);
        }

        // Скрываем верхнюю панель ресурсов, чтобы не накладывалась на заголовок
        HideTopResources();

        if (knowledgePanel != null)
        {
            knowledgePanel.SetActive(true);
            SetupKnowledgePanelLayout();
        }

        BuildAllRankCardsUI();

        // При открытии ставим скролл в самый верх
        if (knowledgeScrollView != null)
        {
            knowledgeScrollView.verticalNormalizedPosition = 1f;
        }

        // Если книги еще не были прочитаны до конца - блокируем крестик закрытия
        if (knowledgeCloseButton != null)
        {
            knowledgeCloseButton.interactable = isKnowledgeCompleted;
        }
    }

    private void HideTopResources()
    {
        if (topResourcesPanel != null)
        {
            topResourcesPanel.SetActive(false);
        }
        else
        {
            GameObject topPanel = GameObject.Find("TopPanel");
            if (topPanel != null) topPanel.SetActive(false);
            GameObject headerPlate = GameObject.Find("Header_Plate");
            if (headerPlate != null) headerPlate.SetActive(false);
        }
    }

    private void RestoreTopResources()
    {
        if (topResourcesPanel != null)
        {
            topResourcesPanel.SetActive(true);
        }
        else
        {
            GameObject topPanel = GameObject.Find("TopPanel");
            if (topPanel != null) topPanel.SetActive(true);
            GameObject headerPlate = GameObject.Find("Header_Plate");
            if (headerPlate != null) headerPlate.SetActive(true);
        }
    }

    /// <summary>
    /// Автоматическая настройка размеров панели знаний и структуры ScrollRect
    /// </summary>
    private void SetupKnowledgePanelLayout()
    {
        if (knowledgePanel == null) return;

        // Поиск и форматирование заголовка "Ранги Алхимии" по центру
        if (titleText == null)
        {
            TextMeshProUGUI[] tmps = knowledgePanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t.name.ToLower().Contains("title") || t.text.Contains("Знания") || t.text.Contains("Ранги"))
                {
                    titleText = t;
                    break;
                }
            }
        }

        if (titleText != null)
        {
            titleText.text = "Ранги Алхимии";
            titleText.alignment = TextAlignmentOptions.Center;
            RectTransform trt = titleText.GetComponent<RectTransform>();
            if (trt != null)
            {
                trt.anchorMin = new Vector2(0.5f, trt.anchorMin.y);
                trt.anchorMax = new Vector2(0.5f, trt.anchorMax.y);
                trt.pivot = new Vector2(0.5f, trt.pivot.y);
                trt.anchoredPosition = new Vector2(0f, trt.anchoredPosition.y);
            }
        }

        RectTransform panelRect = knowledgePanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // Убеждаемся, что панель не сплюснута
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            if (panelRect.sizeDelta.x < 500f || panelRect.sizeDelta.y < 600f)
            {
                panelRect.sizeDelta = new Vector2(720f, 860f);
            }
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localScale = Vector3.one;
        }

        // Ищем ScrollRect если не назначен
        if (knowledgeScrollView == null)
        {
            knowledgeScrollView = knowledgePanel.GetComponentInChildren<ScrollRect>(true);
        }

        // Ищем Content если не назначен
        if (knowledgeContent == null && knowledgeScrollView != null)
        {
            knowledgeContent = knowledgeScrollView.content;
        }

        if (knowledgeContent == null)
        {
            Transform found = knowledgePanel.transform.Find("Content");
            if (found == null) found = knowledgePanel.transform.Find("ScrollView/Viewport/Content");
            if (found != null) knowledgeContent = found;
        }

        if (knowledgeCloseButton == null)
        {
            Button[] btns = knowledgePanel.GetComponentsInChildren<Button>(true);
            foreach (var b in btns)
            {
                if (b.name.ToLower().Contains("close") || b.name.ToLower().Contains("exit"))
                {
                    knowledgeCloseButton = b;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Генерация карточек всех 21 рангов и 4 этапов в Content ScrollRect
    /// </summary>
    public void BuildAllRankCardsUI()
    {
        if (knowledgeContent == null) return;

        // Если карточки уже сгенерированы — только обновляем прогресс
        if (knowledgeContent.childCount >= 21)
        {
            UpdateRankCardsProgress();
            return;
        }

        // Настраиваем компонент VerticalLayoutGroup и ContentSizeFitter на Content
        VerticalLayoutGroup vlg = knowledgeContent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = knowledgeContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16f;
        vlg.padding = new RectOffset(20, 20, 20, 40);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = knowledgeContent.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = knowledgeContent.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Очищаем старые тестовые элементы
        for (int i = knowledgeContent.childCount - 1; i >= 0; i--)
        {
            Destroy(knowledgeContent.GetChild(i).gameObject);
        }

        int currentRankIndex = PlayerPrefs.GetInt("Player_Mastery_Rank", 1); // 0=Новичок (Rank 1), 1=Новичок-травник (Rank 2)...
        // В PlayerPrefs rank index может быть 0-based
        int currentMasteryExp = PlayerPrefs.GetInt("Player_Mastery_Exp", 0);

        string lastStage = "";

        for (int i = 0; i < allRanks.Count; i++)
        {
            var rank = allRanks[i];

            // Если начался новый этап — добавляем красивый золотой баннер этапа
            if (rank.stageNameRU != lastStage)
            {
                lastStage = rank.stageNameRU;
                CreateStageHeaderUI(lastStage);
            }

            // Создаем карточку ранга
            CreateRankCardUI(rank, i, currentRankIndex);
        }
    }

    private void CreateStageHeaderUI(string stageTitle)
    {
        GameObject headerObj = new GameObject("StageHeader", typeof(RectTransform), typeof(Image));
        headerObj.transform.SetParent(knowledgeContent, false);

        RectTransform rt = headerObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 44f);

        Image img = headerObj.GetComponent<Image>();
        img.color = new Color(0.22f, 0.16f, 0.32f, 0.95f);

        // Текст названия этапа
        GameObject textObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(headerObj.transform, false);

        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16, 0);
        trt.offsetMax = new Vector2(-16, 0);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = $"<color=#FFE57F><b>{stageTitle}</b></color>";
        tmp.fontSize = 18f;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateRankCardUI(AlchemyRankInfo rank, int listIndex, int currentRankIndex)
    {
        GameObject cardObj = new GameObject($"RankCard_{rank.rankIndex}", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(knowledgeContent, false);

        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 96f);

        Image bgImg = cardObj.GetComponent<Image>();

        // Определение статуса: Достигнуто / Текущий / Заблокировано
        bool isCompleted = listIndex < currentRankIndex;
        bool isCurrent = listIndex == currentRankIndex;

        if (isCurrent)
        {
            bgImg.color = new Color(0.18f, 0.28f, 0.38f, 0.95f); // Подсветка текущего ранга
        }
        else if (isCompleted)
        {
            bgImg.color = new Color(0.12f, 0.20f, 0.16f, 0.90f); // Зеленоватый фон для открытых
        }
        else
        {
            bgImg.color = new Color(0.10f, 0.09f, 0.15f, 0.85f); // Темный фон для будущих рангов
        }

        // Контейнер текста
        GameObject infoObj = new GameObject("RankInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoObj.transform.SetParent(cardObj.transform, false);

        RectTransform infoRt = infoObj.GetComponent<RectTransform>();
        infoRt.anchorMin = Vector2.zero;
        infoRt.anchorMax = Vector2.one;
        infoRt.offsetMin = new Vector2(20, 8);
        infoRt.offsetMax = new Vector2(-20, -8);

        TextMeshProUGUI infoText = infoObj.GetComponent<TextMeshProUGUI>();
        infoText.richText = true;

        string hexColor = ColorUtility.ToHtmlStringRGB(rank.rankTextColor);
        string statusBadge = "";

        if (isCompleted)
        {
            statusBadge = "<color=#4DFFBF>[ИЗУЧЕНО]</color>";
        }
        else if (isCurrent)
        {
            statusBadge = "<color=#FFE57F>[ТЕКУЩИЙ РАНГ]</color>";
        }
        else
        {
            statusBadge = "<color=#AAAAAA>[ЗАКРЫТО]</color>";
        }

        infoText.text = $"<size=20><b>#{rank.rankIndex}. <color=#{hexColor}>{rank.rankNameRU}</color></b>   {statusBadge}</size>\n" +
                        $"<size=15><color=#D1D5DB>{rank.rankDescriptionRU}</color></size>\n" +
                        $"<size=14><color=#93C5FD>Требуется опыта мастерства:</color> <b><color=#FDE047>{rank.requiredMasteryExp:N0} XP</color></b></size>";
        infoText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void UpdateRankCardsProgress()
    {
        // Обновление состояния существующих карточек
        int currentRankIndex = PlayerPrefs.GetInt("Player_Mastery_Rank", 0);
        int cardIdx = 0;

        for (int i = 0; i < knowledgeContent.childCount; i++)
        {
            Transform child = knowledgeContent.GetChild(i);
            if (child.name.StartsWith("RankCard_"))
            {
                Image bg = child.GetComponent<Image>();
                if (bg != null)
                {
                    if (cardIdx == currentRankIndex) bg.color = new Color(0.18f, 0.28f, 0.38f, 0.95f);
                    else if (cardIdx < currentRankIndex) bg.color = new Color(0.12f, 0.20f, 0.16f, 0.90f);
                    else bg.color = new Color(0.10f, 0.09f, 0.15f, 0.85f);
                }
                cardIdx++;
            }
        }
    }

    /// <summary>
    /// Проверка прокрутки: когда игрок долистал до самого низа (pos.y <= 0.08f), разблокируем крестик
    /// </summary>
    private void OnScrollValueChanged(Vector2 pos)
    {
        if (isKnowledgeCompleted) return;

        // В Unity ScrollRect verticalNormalizedPosition равен 1 вверху и 0 внизу
        if (pos.y <= 0.08f)
        {
            isKnowledgeCompleted = true;
            if (knowledgeCloseButton != null)
            {
                knowledgeCloseButton.interactable = true;
            }

            if (unlockSound != null && SettingsManager.Instance != null)
                SettingsManager.Instance.PlaySoundEffect(unlockSound);
        }
    }

    public void CloseKnowledgeUI()
    {
        if (closeSound != null && SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(closeSound);

        if (knowledgePanel != null)
        {
            knowledgePanel.SetActive(false);
        }

        // Восстанавливаем отображение верхней панели ресурсов
        RestoreTopResources();

        // Иконка книг знаний остается заблокированной во время разговора
        SetKnowledgeButtonInteractable(false);

        // Переходим к фазе диалога о Мини-играх (Колесо игр)
        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.StartMinigamesIntroductionDialogue();
        }
    }

    [ContextMenu("Сбросить Прогресс Окна Знаний (Reset Knowledge Progress)")]
    public void ResetKnowledgeProgress()
    {
        isKnowledgeCompleted = false;
        if (knowledgeCloseButton != null)
        {
            knowledgeCloseButton.interactable = false;
        }
        if (knowledgePanel != null)
        {
            knowledgePanel.SetActive(false);
        }
        if (knowledgeIconButton != null)
        {
            knowledgeIconButton.SetActive(false);
        }
    }
}

