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
    public static Knowledge_Manager Instance { get; private set; } // Статический синглтон окна знаний

    [Header("UI Панель Знаний и Рангов")]
    public GameObject knowledgePanel; // Главная панель окна "Знания и Ранги"
    public TextMeshProUGUI titleText; // Заголовок панели (Ранги Алхимии)
    public Button knowledgeCloseButton; // Кнопка закрытия окна знаний
    public ScrollRect knowledgeScrollView; // Компонент прокрутки списка рангов (ScrollRect)
    public Transform knowledgeContent; // Внутренний контейнер списка карточек рангов (Content)
    public bool isKnowledgeCompleted = false; // Достигнут ли максимальный 21 ранг мастерства

    [Header("Иконка Книг Знаний в верхнем UI")]
    public GameObject knowledgeIconButton; // Иконка сложенных книг слева от сундука в верхнем интерфейсе
    public Button knowledgeButton; // Интерактивная кнопка открытия знаний
    public bool autoAlignKnowledgeToChest = true; // Автоматическое позиционирование рядом с сундуком
    public Vector2 knowledgeOffsetFromChest = new Vector2(-135f, 0f); // Смещение по X/Y относительно сундука

    [Header("Верхняя панель ресурсов (скрывается при открытии)")]
    public GameObject topResourcesPanel; // Ссылка на панель ресурсов, скрываемую для чистоты интерфейса

    [Header("Звуки")]
    public AudioClip openKnowledgeSound; // Звук шелеста страниц при открытии книги знаний
    public AudioClip closeSound; // Звук закрытия окна
    public AudioClip unlockSound; // Звук открытия нового ранга алхимии

    [System.Serializable]
    public class AlchemyRankInfo
    {
        public int rankIndex; // Порядковый номер ранга (1 - 21)
        public string stageNameRU; // Название этапа алхимии (I - IV)
        public string rankNameRU; // Русское название ранга
        public string rankNameEN; // Английское название ранга
        public string rankNameTR; // Турецкое название ранга
        public int requiredMasteryExp; // Необходимый опыт мастерства для открытия
        public Color rankTextColor = Color.white; // Цвет шрифта названия ранга
        public string rankDescriptionRU; // Описание достижений ранга
    }

    [Header("Список 21 Рангов Мастерства (4 Этапа)")]
    public List<AlchemyRankInfo> allRanks = new List<AlchemyRankInfo>(); // База данных всех рангов

    private void Awake() // Инициализация синглтона, подписок на события кнопок и скролла
    {
        Instance = this; // Инициализация синглтона

        if (knowledgeCloseButton != null) // Если кнопка закрытия окна задана
        {
            knowledgeCloseButton.onClick.RemoveAllListeners(); // Очистка старых событий
            knowledgeCloseButton.onClick.AddListener(CloseKnowledgeUI); // Назначение закрытия окна
        }

        if (knowledgeButton != null) // Если кнопка вызова знаний задана
        {
            knowledgeButton.onClick.RemoveAllListeners(); // Очистка старых событий
            knowledgeButton.onClick.AddListener(OnKnowledgeButtonClicked); // Назначение открытия окна
        }

        if (knowledgeScrollView != null) // Если компонент прокрутки ScrollRect задан
        {
            knowledgeScrollView.onValueChanged.RemoveAllListeners(); // Очистка обработчиков скролла
            knowledgeScrollView.onValueChanged.AddListener(OnScrollValueChanged); // Реакция на прокрутку списка
        }

        InitDefaultRanks(); // Инициализация базы данных 21 ранга алхимии
    }

    private void Start() // Скрытие панелей при начальном старте игры
    {
        if (knowledgePanel != null) knowledgePanel.SetActive(false); // Прячем окно знаний на старте игры
        if (knowledgeIconButton != null) knowledgeIconButton.SetActive(false); // Прячем иконку до первого открытия
    }

    public void InitDefaultRanks() // Наполнение структуры данных 21 алхимическим рангом и 4 этапами
    {
        if (allRanks.Count > 0) return; // Пропуск если база уже заполнена

        // Этап I: Ученик (Ранги 1–6)
        AddRank(1, "Этап I: Ученик — Основы и базовые экстракты", "Новичок", "Novice", "Acemi", 100, Color.white, "Начало пути в алхимической лаборатории."); // Ранг 1
        AddRank(2, "Этап I: Ученик — Основы и базовые экстракты", "Новичок-травник", "Herbalist Novice", "Bitkici Acemi", 300, new Color(0.32f, 0.75f, 0.50f, 1f), "Сбор целебных трав и приготовление базовых отваров."); // Ранг 2
        AddRank(3, "Этап I: Ученик — Основы и базовые экстракты", "Подмастерье угля", "Coal Apprentice", "Komur Ciragi", 500, new Color(0.40f, 0.80f, 0.60f, 1f), "Контроль жара котла и очистка минеральных углей."); // Ранг 3
        AddRank(4, "Этап I: Ученик — Основы и базовые экстракты", "Экстрактор", "Extractor", "Ekstraktor", 1000, new Color(0.45f, 0.85f, 0.70f, 1f), "Выделение чистых соков и эссенций из редких растений."); // Ранг 4
        AddRank(5, "Этап I: Ученик — Основы и базовые экстракты", "Знаток пропорций", "Proportion Master", "Oran Ustasi", 1500, new Color(0.50f, 0.90f, 0.80f, 1f), "Идеальное соблюдение дозировок без риска взрыва."); // Ранг 5
        AddRank(6, "Этап I: Ученик — Основы и базовые экстракты", "Сертифицированный ученик", "Certified Apprentice", "Sertifikali Cirak", 3000, new Color(0.55f, 0.95f, 0.90f, 1f), "Завершение базового обучения и допуск к сложным реактивам."); // Ранг 6

        // Этап II: Адепт (Ранги 7–11)
        AddRank(7, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Практик масел", "Oil Practitioner", "Yag Uygulayicisi", 5000, new Color(0.30f, 0.70f, 1f, 1f), "Варка густых эфирных масел и настоек длительного действия."); // Ранг 7
        AddRank(8, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Дистиллятор", "Distiller", "Damitici", 7000, new Color(0.35f, 0.75f, 1f, 1f), "Многоступенчатая перегонка редких магических спиртов."); // Ранг 8
        AddRank(9, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Мастер ферментации", "Fermentation Master", "Mayalama Ustasi", 10000, new Color(0.40f, 0.80f, 1f, 1f), "Ускорение биологических реакций лунными дрожжами."); // Ранг 9
        AddRank(10, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Каталитический химик", "Catalytic Chemist", "Katalitik Kimyager", 15000, new Color(0.50f, 0.85f, 1f, 1f), "Использование катализаторов для синтеза редких минералов."); // Ранг 10
        AddRank(11, "Этап II: Адепт — Нестабильные субстанции и катализаторы", "Старший фармацевт", "Senior Pharmacist", "Kidemli Eczaci", 20000, new Color(0.60f, 0.90f, 1f, 1f), "Создание сильнодействующих лекарств и противоядий."); // Ранг 11

        // Этап III: Магистр (Ранги 12–16)
        AddRank(12, "Этап III: Магистр — Эфир, пустота и кристаллы", "Эфирный экспериментатор", "Aether Experimenter", "Eter Deneycisi", 25000, new Color(0.80f, 0.50f, 1f, 1f), "Улавливание невидимых эфирных потоков в стеклянные сосуды."); // Ранг 12
        AddRank(13, "Этап III: Магистр — Эфир, пустота и кристаллы", "Кристаллограф", "Crystallographer", "Kristalograft", 30000, new Color(0.85f, 0.55f, 1f, 1f), "Выращивание кристаллов маны идеальной геометрической формы."); // Ранг 13
        AddRank(14, "Этап III: Магистр — Эфир, пустота и кристаллы", "Мастер трансмутации", "Transmutation Master", "Donusum Ustasi", 37000, new Color(0.90f, 0.60f, 1f, 1f), "Превращение свинца в медь и серебро силой мысли и тепла."); // Ранг 14
        AddRank(15, "Этап III: Магистр — Эфир, пустота и кристаллы", "Вивисектор сущностей", "Essence Vivisector", "Oz Kasifi", 45000, new Color(0.95f, 0.65f, 1f, 1f), "Разделение духовной и физической материи компонентов."); // Ранг 15
        AddRank(16, "Этап III: Магистр — Эфир, пустота и кристаллы", "Архимагистр рецептуры", "Archmagister of Formulas", "Formul Basbuyucusu", 60000, new Color(1f, 0.70f, 0.95f, 1f), "Создание собственных уникальных формул для великих свитков."); // Ранг 16

        // Этап IV: Великий Алхимик (Ранги 17–21)
        AddRank(17, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Повелитель температур", "Lord of Temperatures", "Sicaklik Efendisi", 70000, new Color(1f, 0.80f, 0.30f, 1f), "Контроль абсолютного нуля и пламени феникса."); // Ранг 17
        AddRank(18, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Ткач реальности", "Reality Weaver", "Gerceklik Dokuyucusu", 85000, new Color(1f, 0.85f, 0.35f, 1f), "Изменение физических свойств пространства вокруг котла."); // Ранг 18
        AddRank(19, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Конструктор душ", "Soul Constructor", "Ruh Yapicisi", 120000, new Color(1f, 0.90f, 0.40f, 1f), "Вдохновение жизни в гомункулов и волшебных стражей."); // Ранг 19
        AddRank(20, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Хранитель Первоматерии", "Keeper of Prima Materia", "Ilk Maddenin Bekcisi", 200000, new Color(1f, 0.95f, 0.50f, 1f), "Владение изначальной субстанцией творения Вселенной."); // Ранг 20
        AddRank(21, "Этап IV: Великий Алхимик — Легенды и Первозданная магия", "Создатель Философского камня", "Creator of Philosopher's Stone", "Felsefe Tasi Yaraticisi", 1000000, new Color(1f, 0.98f, 0.60f, 1f), "Вершина мастерства: вечная жизнь и бесконечное золото."); // Ранг 21
    }

    private void AddRank(int idx, string stage, string ru, string en, string tr, int exp, Color col, string desc = "") // Вспомогательный метод добавления ранга в коллекцию
    {
        allRanks.Add(new AlchemyRankInfo // Создание нового объекта ранга
        {
            rankIndex = idx, // Номер ранга
            stageNameRU = stage, // Название этапа
            rankNameRU = ru, // Имя ранга RU
            rankNameEN = en, // Имя ранга EN
            rankNameTR = tr, // Имя ранга TR
            requiredMasteryExp = exp, // Требуемый опыт
            rankTextColor = col, // Цвет
            rankDescriptionRU = desc // Описание
        });
    }

    public void AlignKnowledgeButtonToChest() // Привязка и выравнивание иконки книг рядом с сундуком
    {
        if (!autoAlignKnowledgeToChest) return; // Проверка разрешения выравнивания
        if (knowledgeIconButton != null) // Если иконка существует
        {
            GameObject refObj = null; // Опорный объект сундука/свитка
            if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов активен
            {
                if (DialogueSystem_Manager.Instance.chestIconButton != null) refObj = DialogueSystem_Manager.Instance.chestIconButton; // Сундук
                else if (DialogueSystem_Manager.Instance.smallScrollIconButton != null) refObj = DialogueSystem_Manager.Instance.smallScrollIconButton; // Свиток
                else if (DialogueSystem_Manager.Instance.calendarIconButton != null) refObj = DialogueSystem_Manager.Instance.calendarIconButton; // Календарь
            }

            if (refObj != null) // Если опорная иконка найдена
            {
                RectTransform refRect = refObj.GetComponent<RectTransform>(); // RectTransform сундука
                RectTransform knowRect = knowledgeIconButton.GetComponent<RectTransform>(); // RectTransform иконки знаний
                if (refRect != null && knowRect != null) // Если оба компонента найдены
                {
                    if (knowledgeIconButton.transform.parent != refObj.transform.parent) // Если разные родители
                    {
                        knowledgeIconButton.transform.SetParent(refObj.transform.parent, false); // Перенос к общему родителю
                    }

                    knowRect.anchorMin = refRect.anchorMin; // Выравнивание anchorMin
                    knowRect.anchorMax = refRect.anchorMax; // Выравнивание anchorMax
                    knowRect.pivot = refRect.pivot; // Выравнивание pivot
                    knowRect.sizeDelta = refRect.sizeDelta; // Выравнивание размеров

                    // Если отступ был маленьким, задаем безопасные -135 пикселей
                    Vector2 actualOffset = knowledgeOffsetFromChest; // Получение заданного смещения
                    if (actualOffset.x > -125f) actualOffset.x = -135f; // Безопасное смещение

                    knowRect.anchoredPosition = refRect.anchoredPosition + actualOffset; // Установка финальной позиции
                }
            }

            // Очищаем стандартный текст "Button"
            TextMeshProUGUI[] tmps = knowledgeIconButton.GetComponentsInChildren<TextMeshProUGUI>(true); // Поиск TMP
            foreach (var t in tmps) t.text = ""; // Очистка текста
            UnityEngine.UI.Text[] texts = knowledgeIconButton.GetComponentsInChildren<UnityEngine.UI.Text>(true); // Поиск Text
            foreach (var t in texts) t.text = ""; // Очистка текста
        }
    }

    public void SetKnowledgeButtonInteractable(bool interactable) // Включение или выключение кликабельности кнопки знаний
    {
        if (knowledgeButton != null) // Если кнопка назначена
        {
            knowledgeButton.interactable = interactable; // Установка состояния
        }
        else if (knowledgeIconButton != null) // Если есть только иконка
        {
            Button btn = knowledgeIconButton.GetComponent<Button>(); // Поиск компонента Button
            if (btn != null) btn.interactable = interactable; // Установка состояния
        }
    }

    public void OnKnowledgeButtonClicked() // Обработка клика по иконке книг знаний
    {
        if (DialogueSystem_Manager.Instance != null && // Если менеджер диалогов активен
            (DialogueSystem_Manager.Instance.isCraftingInProgress || // Идет ли варка
            (DialogueSystem_Manager.Instance.dialoguePanel != null && DialogueSystem_Manager.Instance.dialoguePanel.activeSelf))) // Открыт ли диалог
        {
            Debug.Log("[KNOWLEDGE] Знания заблокированы во время диалога Кота."); // Логирование блокировки
            return; // Выход
        }

        OpenKnowledgeUI(); // Открытие окна знаний
    }

    public void OpenKnowledgeUI() // Открытие окна знаний, скрытие ресурсов и генерация карточек
    {
        if (openKnowledgeSound != null && SettingsManager.Instance != null) // Если звук назначен
            SettingsManager.Instance.PlaySoundEffect(openKnowledgeSound); // Воспроизведение звука страниц

        if (DialogueSystem_Manager.Instance != null && DialogueSystem_Manager.Instance.dialoguePanel != null) // Если диалог открыт
        {
            DialogueSystem_Manager.Instance.dialoguePanel.SetActive(false); // Скрытие диалога
        }

        // Скрываем верхнюю панель ресурсов, чтобы не накладывалась на заголовок
        HideTopResources(); // Скрытие шапки с ресурсами

        if (knowledgePanel != null) // Если панель знаний задана
        {
            knowledgePanel.SetActive(true); // Включение окна
            SetupKnowledgePanelLayout(); // Настройка верстки и якорей
        }

        BuildAllRankCardsUI(); // Построение списка карточек всех 21 рангов

        // При открытии ставим скролл в самый верх
        if (knowledgeScrollView != null) // Если скролл назначен
        {
            knowledgeScrollView.verticalNormalizedPosition = 1f; // Позиция в самом верху
        }

        // Если книги еще не были прочитаны до конца - блокируем крестик закрытия
        if (knowledgeCloseButton != null) // Если кнопка закрытия есть
        {
            knowledgeCloseButton.interactable = isKnowledgeCompleted; // Блокировка пока не доскроллено до конца
        }
    }

    private void HideTopResources() // Скрытие верхней панели ресурсов для удобного просмотра знаний
    {
        if (topResourcesPanel != null) // Если панель назначена явно
        {
            topResourcesPanel.SetActive(false); // Скрытие панели
        }
        else // Фоллбэк поиск по именам объектов
        {
            GameObject topPanel = GameObject.Find("TopPanel"); // Поиск TopPanel
            if (topPanel != null) topPanel.SetActive(false); // Скрытие
            GameObject headerPlate = GameObject.Find("Header_Plate"); // Поиск Header_Plate
            if (headerPlate != null) headerPlate.SetActive(false); // Скрытие
        }
    }

    private void RestoreTopResources() // Восстановление отображения панели ресурсов после закрытия знаний
    {
        if (topResourcesPanel != null) // Если панель назначена явно
        {
            topResourcesPanel.SetActive(true); // Включение панели
        }
        else // Фоллбэк поиск по объектам
        {
            GameObject topPanel = GameObject.Find("TopPanel"); // Поиск TopPanel
            if (topPanel != null) topPanel.SetActive(true); // Включение
            GameObject headerPlate = GameObject.Find("Header_Plate"); // Поиск Header_Plate
            if (headerPlate != null) headerPlate.SetActive(true); // Включение
        }
    }

    /// <summary>
    /// Автоматическая настройка размеров панели знаний и структуры ScrollRect
    /// </summary>
    private void SetupKnowledgePanelLayout() // Автоматическая настройка геометрии панели, заголовка и связей ScrollRect
    {
        if (knowledgePanel == null) return; // Проверка наличия панели

        // Поиск и форматирование заголовка "Ранги Алхимии" по центру
        if (titleText == null) // Если заголовок не назначен
        {
            TextMeshProUGUI[] tmps = knowledgePanel.GetComponentsInChildren<TextMeshProUGUI>(true); // Поиск TMP
            foreach (var t in tmps) // Перебор текстовых объектов
            {
                if (t.name.ToLower().Contains("title") || t.text.Contains("Знания") || t.text.Contains("Ранги")) // Поиск по ключевым словам
                {
                    titleText = t; // Присвоение заголовка
                    break; // Прерывание цикла
                }
            }
        }

        if (titleText != null) // Если заголовок найден
        {
            titleText.text = "Ранги Алхимии"; // Установка текста заголовка
            titleText.alignment = TextAlignmentOptions.Center; // Выравнивание по центру
            RectTransform trt = titleText.GetComponent<RectTransform>(); // RectTransform заголовка
            if (trt != null) // Если трансформ есть
            {
                trt.anchorMin = new Vector2(0.5f, trt.anchorMin.y); // Центрирование
                trt.anchorMax = new Vector2(0.5f, trt.anchorMax.y); // Центрирование
                trt.pivot = new Vector2(0.5f, trt.pivot.y); // Центрирование
                trt.anchoredPosition = new Vector2(0f, trt.anchoredPosition.y); // Смещение в центр
            }
        }

        RectTransform panelRect = knowledgePanel.GetComponent<RectTransform>(); // RectTransform главной панели
        if (panelRect != null) // Если найден
        {
            // Убеждаемся, что панель не сплюснута
            panelRect.anchorMin = new Vector2(0.5f, 0.5f); // Якорь по центру
            panelRect.anchorMax = new Vector2(0.5f, 0.5f); // Якорь по центру
            panelRect.pivot = new Vector2(0.5f, 0.5f); // Пивот по центру
            if (panelRect.sizeDelta.x < 500f || panelRect.sizeDelta.y < 600f) // Проверка минимальных габаритов
            {
                panelRect.sizeDelta = new Vector2(720f, 860f); // Установка комфортного размера 720x860
            }
            panelRect.anchoredPosition = Vector2.zero; // Центрирование на экране
            panelRect.localScale = Vector3.one; // Масштаб 1:1
        }

        // Ищем ScrollRect если не назначен
        if (knowledgeScrollView == null) // Если скролл не назначен
        {
            knowledgeScrollView = knowledgePanel.GetComponentInChildren<ScrollRect>(true); // Поиск компонента ScrollRect
        }

        // Ищем Content если не назначен
        if (knowledgeContent == null && knowledgeScrollView != null) // Если Content пуст
        {
            knowledgeContent = knowledgeScrollView.content; // Взятие из ScrollRect
        }

        if (knowledgeContent == null) // Фоллбэк поиск
        {
            Transform found = knowledgePanel.transform.Find("Content"); // Поиск прямым путем
            if (found == null) found = knowledgePanel.transform.Find("ScrollView/Viewport/Content"); // Поиск через Viewport
            if (found != null) knowledgeContent = found; // Назначение найденного Content
        }

        if (knowledgeCloseButton == null) // Если кнопка закрытия не назначена
        {
            Button[] btns = knowledgePanel.GetComponentsInChildren<Button>(true); // Поиск кнопок
            foreach (var b in btns) // Перебор кнопок
            {
                if (b.name.ToLower().Contains("close") || b.name.ToLower().Contains("exit")) // Поиск по имени
                {
                    knowledgeCloseButton = b; // Назначение кнопки закрытия
                    break; // Выход
                }
            }
        }
    }

    /// <summary>
    /// Генерация карточек всех 21 рангов и 4 этапов в Content ScrollRect
    /// </summary>
    public void BuildAllRankCardsUI() // Генерация карточек всех 21 рангов и 4 этапов в Content ScrollRect
    {
        if (knowledgeContent == null) return; // Проверка наличия контейнера

        // Если карточки уже сгенерированы — только обновляем прогресс
        if (knowledgeContent.childCount >= 21) // Если карточки уже есть
        {
            UpdateRankCardsProgress(); // Обновление подсветки
            return; // Выход
        }

        // Настраиваем компонент VerticalLayoutGroup и ContentSizeFitter на Content
        VerticalLayoutGroup vlg = knowledgeContent.GetComponent<VerticalLayoutGroup>(); // Компонент вертикальной компоновки
        if (vlg == null) vlg = knowledgeContent.gameObject.AddComponent<VerticalLayoutGroup>(); // Добавление при отсутствии
        vlg.spacing = 16f; // Отступ между карточками 16px
        vlg.padding = new RectOffset(20, 20, 20, 40); // Внутренние отступы
        vlg.childControlWidth = true; // Авто-ширина дочерних
        vlg.childControlHeight = false; // Фиксированная высота
        vlg.childForceExpandWidth = true; // Растягивание по ширине
        vlg.childForceExpandHeight = false; // Запрет растягивания по высоте

        ContentSizeFitter csf = knowledgeContent.GetComponent<ContentSizeFitter>(); // Компонент авто-размера
        if (csf == null) csf = knowledgeContent.gameObject.AddComponent<ContentSizeFitter>(); // Добавление при отсутствии
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Подгонка высоты под контент

        // Очищаем старые тестовые элементы
        for (int i = knowledgeContent.childCount - 1; i >= 0; i--) // Цикл очистки
        {
            Destroy(knowledgeContent.GetChild(i).gameObject); // Удаление дочернего объекта
        }

        int currentRankIndex = PlayerPrefs.GetInt("Player_Mastery_Rank", 1); // 0=Новичок (Rank 1), 1=Новичок-травник (Rank 2)...
        // В PlayerPrefs rank index может быть 0-based
        int currentMasteryExp = PlayerPrefs.GetInt("Player_Mastery_Exp", 0); // Загрузка накопленного опыта

        string lastStage = ""; // Трекер смены этапа

        for (int i = 0; i < allRanks.Count; i++) // Перебор 21 ранга
        {
            var rank = allRanks[i]; // Получение конфигурации ранга

            // Если начался новый этап — добавляем красивый золотой баннер этапа
            if (rank.stageNameRU != lastStage) // Проверка смены этапа
            {
                lastStage = rank.stageNameRU; // Обновление текущего этапа
                CreateStageHeaderUI(lastStage); // Создание шапки этапа
            }

            // Создаем карточку ранга
            CreateRankCardUI(rank, i, currentRankIndex); // Создание UI-карточки ранга
        }
    }

    private void CreateStageHeaderUI(string stageTitle) // Создание золотистого разделительного баннера этапа алхимии
    {
        GameObject headerObj = new GameObject("StageHeader", typeof(RectTransform), typeof(Image)); // Создание объекта баннера
        headerObj.transform.SetParent(knowledgeContent, false); // Добавление в Content

        RectTransform rt = headerObj.GetComponent<RectTransform>(); // RectTransform
        rt.sizeDelta = new Vector2(0f, 44f); // Высота баннера 44px

        Image img = headerObj.GetComponent<Image>(); // Image
        img.color = new Color(0.22f, 0.16f, 0.32f, 0.95f); // Фиолетово-магический фон

        // Текст названия этапа
        GameObject textObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI)); // Создание объекта текста
        textObj.transform.SetParent(headerObj.transform, false); // Добавление в баннер

        RectTransform trt = textObj.GetComponent<RectTransform>(); // RectTransform текста
        trt.anchorMin = Vector2.zero; // Растягивание
        trt.anchorMax = Vector2.one; // Растягивание
        trt.offsetMin = new Vector2(16, 0); // Отступ слева
        trt.offsetMax = new Vector2(-16, 0); // Отступ справа

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>(); // TextMeshProUGUI
        tmp.text = $"<color=#FFE57F><b>{stageTitle}</b></color>"; // Золотой текст названия этапа
        tmp.fontSize = 18f; // Размер шрифта 18
        tmp.alignment = TextAlignmentOptions.Center; // Выравнивание по центру
    }

    private void CreateRankCardUI(AlchemyRankInfo rank, int listIndex, int currentRankIndex) // Создание визуальной карточки конкретного ранга с индикатором статуса
    {
        GameObject cardObj = new GameObject($"RankCard_{rank.rankIndex}", typeof(RectTransform), typeof(Image)); // Создание карточки
        cardObj.transform.SetParent(knowledgeContent, false); // Размещение в Content

        RectTransform rt = cardObj.GetComponent<RectTransform>(); // RectTransform
        rt.sizeDelta = new Vector2(0f, 96f); // Высота карточки 96px

        Image bgImg = cardObj.GetComponent<Image>(); // Image фона

        // Определение статуса: Достигнуто / Текущий / Заблокировано
        bool isCompleted = listIndex < currentRankIndex; // Пройден ли ранг
        bool isCurrent = listIndex == currentRankIndex; // Текущий ли ранг

        if (isCurrent) // Если это текущий изучаемый ранг
        {
            bgImg.color = new Color(0.18f, 0.28f, 0.38f, 0.95f); // Подсветка текущего ранга
        }
        else if (isCompleted) // Если ранг уже изучен
        {
            bgImg.color = new Color(0.12f, 0.20f, 0.16f, 0.90f); // Зеленоватый фон для открытых
        }
        else // Если ранг еще заблокирован
        {
            bgImg.color = new Color(0.10f, 0.09f, 0.15f, 0.85f); // Темный фон для будущих рангов
        }

        // Контейнер текста
        GameObject infoObj = new GameObject("RankInfo", typeof(RectTransform), typeof(TextMeshProUGUI)); // Создание текстового блока
        infoObj.transform.SetParent(cardObj.transform, false); // Размещение в карточке

        RectTransform infoRt = infoObj.GetComponent<RectTransform>(); // RectTransform текста
        infoRt.anchorMin = Vector2.zero; // Растягивание
        infoRt.anchorMax = Vector2.one; // Растягивание
        infoRt.offsetMin = new Vector2(20, 8); // Отступы слева и снизу
        infoRt.offsetMax = new Vector2(-20, -8); // Отступы справа и сверху

        TextMeshProUGUI infoText = infoObj.GetComponent<TextMeshProUGUI>(); // TextMeshProUGUI
        infoText.richText = true; // Включение Rich Text

        string hexColor = ColorUtility.ToHtmlStringRGB(rank.rankTextColor); // Конвертация цвета в HEX
        string statusBadge = ""; // Текстовый бейдж статуса

        if (isCompleted) // Изучено
        {
            statusBadge = "<color=#4DFFBF>[ИЗУЧЕНО]</color>"; // Зеленый бейдж
        }
        else if (isCurrent) // Текущий
        {
            statusBadge = "<color=#FFE57F>[ТЕКУЩИЙ РАНГ]</color>"; // Золотой бейдж
        }
        else // Закрыто
        {
            statusBadge = "<color=#AAAAAA>[ЗАКРЫТО]</color>"; // Серый бейдж
        }

        infoText.text = $"<size=20><b>#{rank.rankIndex}. <color=#{hexColor}>{rank.rankNameRU}</color></b>   {statusBadge}</size>\n" + // Номер и название
                        $"<size=15><color=#D1D5DB>{rank.rankDescriptionRU}</color></size>\n" + // Описание достижений
                        $"<size=14><color=#93C5FD>Требуется опыта мастерства:</color> <b><color=#FDE047>{rank.requiredMasteryExp:N0} XP</color></b></size>"; // Опыт
        infoText.alignment = TextAlignmentOptions.MidlineLeft; // Выравнивание по левому краю по вертикали по центру
    }

    private void UpdateRankCardsProgress() // Обновление цветовой подсветки существующих карточек рангов
    {
        // Обновление состояния существующих карточек
        int currentRankIndex = PlayerPrefs.GetInt("Player_Mastery_Rank", 0); // Загрузка индекса текущего ранга
        int cardIdx = 0; // Счетчик карточек

        for (int i = 0; i < knowledgeContent.childCount; i++) // Перебор дочерних объектов Content
        {
            Transform child = knowledgeContent.GetChild(i); // Получение дочернего элемента
            if (child.name.StartsWith("RankCard_")) // Если это карточка ранга
            {
                Image bg = child.GetComponent<Image>(); // Получение Image фона
                if (bg != null) // Если Image найден
                {
                    if (cardIdx == currentRankIndex) bg.color = new Color(0.18f, 0.28f, 0.38f, 0.95f); // Текущий ранг
                    else if (cardIdx < currentRankIndex) bg.color = new Color(0.12f, 0.20f, 0.16f, 0.90f); // Пройденный ранг
                    else bg.color = new Color(0.10f, 0.09f, 0.15f, 0.85f); // Закрытый ранг
                }
                cardIdx++; // Инкремент счетчика карточек
            }
        }
    }

    /// <summary>
    /// Проверка прокрутки: когда игрок долистал до самого низа (pos.y <= 0.08f), разблокируем крестик
    /// </summary>
    private void OnScrollValueChanged(Vector2 pos) // Проверка прокрутки: разблокировка кнопки закрытия в самом низу списка
    {
        if (isKnowledgeCompleted) return; // Если уже прочитано — выход

        // В Unity ScrollRect verticalNormalizedPosition равен 1 вверху и 0 внизу
        if (pos.y <= 0.08f) // Если долистано почти до конца списка (ниже 8%)
        {
            isKnowledgeCompleted = true; // Фиксация прочтения всех рангов
            if (knowledgeCloseButton != null) // Если кнопка закрытия назначена
            {
                knowledgeCloseButton.interactable = true; // Разблокировка кнопки выхода
            }

            if (unlockSound != null && SettingsManager.Instance != null) // Если звук разблокировки есть
                SettingsManager.Instance.PlaySoundEffect(unlockSound); // Воспроизведение звука
        }
    }

    public void CloseKnowledgeUI() // Закрытие окна знаний, возврат ресурсов и переход к диалогу мини-игр
    {
        if (closeSound != null && SettingsManager.Instance != null) // Если звук закрытия назначен
            SettingsManager.Instance.PlaySoundEffect(closeSound); // Воспроизведение звука закрытия

        if (knowledgePanel != null) // Если панель знаний есть
        {
            knowledgePanel.SetActive(false); // Скрытие панели
        }

        // Восстанавливаем отображение верхней панели ресурсов
        RestoreTopResources(); // Возврат верхней панели ресурсов

        // Иконка книг знаний остается заблокированной во время разговора
        SetKnowledgeButtonInteractable(false); // Блокировка кнопки до конца диалога

        // Переходим к фазе диалога о Мини-играх (Колесо игр)
        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов активен
        {
            DialogueSystem_Manager.Instance.StartMinigamesIntroductionDialogue(); // Запуск знакомства с мини-играми
        }
    }

    [ContextMenu("Сбросить Прогресс Окна Знаний (Reset Knowledge Progress)")]
    public void ResetKnowledgeProgress() // Сброс состояния окна знаний для тестирования через контекстное меню Unity
    {
        isKnowledgeCompleted = false; // Сброс флага прочтения
        if (knowledgeCloseButton != null) // Если кнопка закрытия есть
        {
            knowledgeCloseButton.interactable = false; // Блокировка кнопки
        }
        if (knowledgePanel != null) // Если панель есть
        {
            knowledgePanel.SetActive(false); // Скрытие панели
        }
        if (knowledgeIconButton != null) // Если иконка есть
        {
            knowledgeIconButton.SetActive(false); // Скрытие иконки
        }
    }
}

