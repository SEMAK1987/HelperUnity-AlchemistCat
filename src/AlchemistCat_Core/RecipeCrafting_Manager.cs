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
    private static RecipeCrafting_Manager _instance; // Приватное статическое поле экземпляра
    public static RecipeCrafting_Manager Instance // Публичный аксессор синглтона с автопоиском в сцене
    {
        get // Геттер синглтона
        {
            if (_instance == null) // Если экземпляр еще не найден
            {
#if UNITY_2023_1_OR_NEWER
                _instance = FindFirstObjectByType<RecipeCrafting_Manager>(); // Поиск первого объекта в Unity 2023+
#else
                _instance = FindObjectOfType<RecipeCrafting_Manager>(); // Поиск объекта в ранних версиях Unity
#endif
                if (_instance == null) // Если все еще не найден
                {
                    RecipeCrafting_Manager[] all = Resources.FindObjectsOfTypeAll<RecipeCrafting_Manager>(); // Поиск среди всех объектов в памяти
                    foreach (var m in all) // Перебор найденных объектов
                    {
                        if (m != null && m.gameObject != null && m.gameObject.scene.isLoaded) // Проверка загруженности сцены
                        {
                            _instance = m; // Назначение найденного объекта
                            if (!_instance.gameObject.activeSelf) // Если объект выключен
                            {
                                _instance.gameObject.SetActive(true); // Включение объекта
                            }
                            break; // Выход из цикла
                        }
                    }
                }
            }
            return _instance; // Возврат экземпляра синглтона
        }
        private set // Приватный сеттер синглтона
        {
            _instance = value; // Присвоение ссылки на экземпляр
        }
    }

    [Header("0. Главная группа Стола и Котла")]
    public GameObject tableCauldronGroup; // Родительская группа Table_Cauldron_Group

    [Header("1. Большой Свиток Рецепта")]
    public GameObject recipeScrollPanel; // Панель свитка с описанием рецепта и ингредиентов
    public Button startCraftButton; // Кнопка "Начать" под рецептом
    public TextMeshProUGUI startCraftButtonText; // Текст кнопки начала крафта

    [Header("2. Стол, Котел и Маленький Кот")]
    public GameObject tableCauldronObject; // Объект котла на столе
    public Button cauldronClickButton; // Кнопка клика по котлу для взаимодействия
    public GameObject tableMiniCatObject; // Маленький котик, сидящий на столе рядом с котлом
    public Button miniCatClickButton; // Кнопка на маленьком котике для вызова подсказки
    public GameObject miniCatBubblePanel; // Облачко с диалоговой репликой маленького кота
    public TextMeshProUGUI miniCatBubbleText; // Текст подсказки котика

    [Header("3. Кнопка 'Изготовить' над Котлом")]
    public GameObject makeBadgeButtonObject; // Плашка с надписью "Изготовить" над котлом
    public Button makeBadgeButton; // Интерактивная кнопка запуска варки

    [Header("4. Шкала Прогресса Варки Зелья")]
    public GameObject craftingProgressBarContainer; // Контейнер шкалы прогресса варки
    public Image craftingProgressFill; // Заполняющаяся полоса прогресса варки
    public TextMeshProUGUI craftingTimerText; // Текст оставшегося времени (5.0s -> 0.0s)
    public float craftDurationSeconds = 5.0f; // Базовое время варки зелья в секундах

    [Header("5. Кнопка 'Забрать' и Всплывающий Опыт (Floating XP)")]
    public GameObject claimPotionButtonObject; // Кнопка "Забрать" готовое зелье
    public Button claimPotionButton; // Кнопка сбора награды
    public int firstRecipeRewardXP = 10; // Опыт за первый скрафченный рецепт (+10 XP)
    public GameObject floatingXPPrefab; // Префаб всплывающей плашки опыта
    public RectTransform floatingXPSpawnPoint; // Точка над котлом, откуда взлетает опыт
    public CanvasGroup floatingXPCanvasGroup; // Группа прозрачности для анимации растворения

    [Header("Спрайты/Значки опыта для разных рецептов (5..1000 XP)")]
    public Image floatingXPImage; // Иконка спрайта опыта
    public Sprite xpBadge5; // Значок +5 XP
    public Sprite xpBadge10; // Значок +10 XP
    public Sprite xpBadge20; // Значок +20 XP
    public Sprite xpBadge30; // Значок +30 XP
    public Sprite xpBadge50; // Значок +50 XP
    public Sprite xpBadge100; // Значок +100 XP
    public Sprite xpBadge200; // Значок +200 XP
    public Sprite xpBadge300; // Значок +300 XP
    public Sprite xpBadge500; // Значок +500 XP
    public Sprite xpBadge1000; // Значок +1000 XP

    [Header("6. Иконка Сундука в верхнем UI")]
    public GameObject chestIconButton; // Иконка сундучка слева от свитка
    public Button chestButton; // Кнопка открытия инвентаря

    [Header("7. Окно Инвентаря (100 слотов, 5 в ряд, со скроллом)")]
    public GameObject inventoryPanel; // Главная панель инвентаря
    public Button inventoryCloseButton; // Кнопка закрытия окна (крестик)
    public Transform inventorySlotsContent; // Контейнер ячеек инвентаря внутри ScrollRect
    public GameObject inventorySlotPrefab; // Префаб отдельной ячейки инвентаря
    public int totalSlots = 100; // Общее количество ячеек инвентаря
    public int columnsCount = 5; // Количество столбцов в сетке инвентаря

    [Header("8. Колба Опыта Мастерства в 1-м слоте")]
    public GameObject masteryPotionItemObject; // Колба опыта мастерства в первом слоте инвентаря
    public Button masteryPotionButton; // Кнопка применения колбы опыта (+100 XP)
    public AudioClip potionConsumeSound; // Звуковой эффект выпивания зелья
    public bool isMasteryPotionConsumed = false; // Флаг: выпита ли колба опыта

    [Header("Звуки")]
    public AudioClip craftStartSound; // Звук начала алхимической варки (кипение)
    public AudioClip craftCompleteSound; // Звук завершения приготовления зелья
    public AudioClip chestOpenSound; // Звук открытия сундука

    private Coroutine craftCoroutine; // Ссылка на запущенную корутину таймера варки

    private void Awake() // Инициализация синглтона, автопоиск элементов и привязка событий кнопок
    {
        Instance = this; // Инициализация синглтона при старте

        AutoFindAndBindTableElements(); // Автопоиск и привязка интерактивных элементов стола и котла

        if (startCraftButton != null) // Если кнопка начала крафта назначена
        {
            startCraftButton.onClick.RemoveAllListeners(); // Сброс старых обработчиков
            startCraftButton.onClick.AddListener(OnStartCraftButtonClicked); // Назначение клика начала крафта
        }

        if (cauldronClickButton != null) // Если кнопка котла назначена
        {
            cauldronClickButton.onClick.RemoveAllListeners(); // Очистка слушателей
            cauldronClickButton.onClick.AddListener(OnCauldronClicked); // Привязка клика по котлу
        }

        if (miniCatClickButton != null) // Если кнопка кота назначена
        {
            miniCatClickButton.onClick.RemoveAllListeners(); // Очистка слушателей
            miniCatClickButton.onClick.AddListener(OnMiniCatClicked); // Привязка клика по котику
        }
        
        SanitizeMiniCatObject(); // Очистка дублирующихся элементов спрайта кота

        if (makeBadgeButton != null) // Если кнопка "Изготовить" назначена
        {
            makeBadgeButton.onClick.RemoveAllListeners(); // Очистка слушателей
            makeBadgeButton.onClick.AddListener(OnMakeBadgeClicked); // Привязка старта варки
        }

        if (claimPotionButton != null) // Если кнопка "Забрать" назначена
        {
            claimPotionButton.onClick.RemoveAllListeners(); // Очистка слушателей
            claimPotionButton.onClick.AddListener(OnClaimPotionClicked); // Привязка сбора зелья
        }

        if (chestButton != null) // Если кнопка сундука назначена
        {
            chestButton.onClick.RemoveAllListeners(); // Очистка слушателей
            chestButton.onClick.AddListener(OnChestButtonClicked); // Привязка открытия сундука
        }

        if (masteryPotionButton != null) // Если кнопка колбы опыта назначена
        {
            masteryPotionButton.onClick.RemoveAllListeners(); // Очистка слушателей
            masteryPotionButton.onClick.AddListener(OnMasteryPotionClicked); // Привязка выпивания колбы
        }

        if (inventoryCloseButton != null) // Если кнопка закрытия инвентаря назначена
        {
            inventoryCloseButton.onClick.RemoveAllListeners(); // Очистка слушателей
            inventoryCloseButton.onClick.AddListener(OnInventoryCloseClicked); // Привязка закрытия инвентаря
        }

        // По умолчанию вспомогательные плашки скрыты в чистом начальном состоянии
        if (makeBadgeButtonObject != null) makeBadgeButtonObject.SetActive(false); // Скрытие плашки "Изготовить"
        if (craftingProgressBarContainer != null) craftingProgressBarContainer.SetActive(false); // Скрытие шкалы варки
        if (claimPotionButtonObject != null) claimPotionButtonObject.SetActive(false); // Скрытие кнопки "Забрать"
        if (miniCatBubblePanel != null) miniCatBubblePanel.SetActive(false); // Скрытие облачка подсказки
        if (floatingXPPrefab != null) floatingXPPrefab.SetActive(false); // Скрытие летающего опыта
        if (chestIconButton != null) chestIconButton.SetActive(false); // Скрытие иконки сундука
        if (inventoryPanel != null) inventoryPanel.SetActive(false); // Скрытие панели инвентаря
    }

    private void Start() // Вызов поиска и связывания элементов стола при старте
    {
        AutoFindAndBindTableElements(); // Повторный автопоиск компонентов в сцене
    }

    public void AutoFindAndBindTableElements() // Автоматический поиск всех элементов стола, котла и кота в сцене
    {
        if (tableCauldronGroup == null) // Если группа стола не назначена
        {
            // Ищем Table_Cauldron_Group среди всех Canvas и всех объектов сцены (включая неактивные)
#if UNITY_2023_1_OR_NEWER
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None); // Поиск всех холстов в Unity 2023+
#else
            Canvas[] allCanvases = FindObjectsOfType<Canvas>(true); // Поиск всех холстов в ранних версиях Unity
#endif
            foreach (var canvas in allCanvases) // Перебор всех холстов
            {
                Transform[] children = canvas.GetComponentsInChildren<Transform>(true); // Получение всех дочерних элементов
                foreach (var child in children) // Поиск подходящего объекта группы
                {
                    if (child.name == "Table_Cauldron_Group" || child.name == "Table_Group" || child.name == "Table") // Сверка имени
                    {
                        tableCauldronGroup = child.gameObject; // Привязка группы
                        break; // Выход
                    }
                }
                if (tableCauldronGroup != null) break; // Прерывание внешнего цикла если группа найдена
            }
        }

        if (tableCauldronGroup != null) // Если группа стола найдена
        {
            Transform[] children = tableCauldronGroup.GetComponentsInChildren<Transform>(true); // Все вложенные объекты
            foreach (var child in children) // Перебор объектов группы
            {
                if (tableCauldronObject == null && (child.name == "Cauldron_Button" || child.name == "Table_Cauldron" || child.name.Contains("Cauldron"))) // Поиск котла
                {
                    tableCauldronObject = child.gameObject; // Привязка объекта котла
                    if (cauldronClickButton == null) cauldronClickButton = child.GetComponent<Button>(); // Привязка кнопки котла
                }
                if (tableMiniCatObject == null && (child.name == "Mini_Cat_Image" || child.name == "Mini_Cat" || child.name == "Table_Mini_Cat")) // Поиск котика
                {
                    tableMiniCatObject = child.gameObject; // Привязка объекта кота
                    if (miniCatClickButton == null) miniCatClickButton = child.GetComponent<Button>(); // Привязка кнопки кота
                }
                if (miniCatBubblePanel == null && (child.name == "Mini_Cat_Bubble" || child.name.Contains("Bubble"))) // Поиск облачка реплики
                {
                    miniCatBubblePanel = child.gameObject; // Привязка панели облачка
                    if (miniCatBubbleText == null) miniCatBubbleText = child.GetComponentInChildren<TextMeshProUGUI>(true); // Привязка текста
                }
                if (makeBadgeButtonObject == null && (child.name == "Make_Badge_Button" || child.name.Contains("Make_Badge"))) // Поиск кнопки "Изготовить"
                {
                    makeBadgeButtonObject = child.gameObject; // Привязка объекта
                    if (makeBadgeButton == null) makeBadgeButton = child.GetComponent<Button>(); // Привязка компонента кнопки
                }
                if (craftingProgressBarContainer == null && (child.name == "Crafting_Progress_Bar" || child.name.Contains("Crafting_Progress"))) // Поиск шкалы прогресса
                {
                    craftingProgressBarContainer = child.gameObject; // Привязка контейнера прогресса
                    if (craftingProgressFill == null) // Поиск заполнения
                    {
                        Image[] imgs = child.GetComponentsInChildren<Image>(true); // Все изображения шкалы
                        foreach (var img in imgs) // Перебор картинок
                        {
                            if (img.type == Image.Type.Filled || img.name.Contains("Fill") || img.name.Contains("Bar")) // Поиск заполняемого спрайта
                            {
                                craftingProgressFill = img; // Привязка полосы
                                break; // Выход
                            }
                        }
                    }
                    if (craftingTimerText == null) craftingTimerText = child.GetComponentInChildren<TextMeshProUGUI>(true); // Привязка текста таймера
                }
                if (claimPotionButtonObject == null && (child.name == "Claim_Potion_Button" || child.name.Contains("Claim_Potion"))) // Поиск кнопки сбора
                {
                    claimPotionButtonObject = child.gameObject; // Привязка объекта кнопки "Забрать"
                    if (claimPotionButton == null) claimPotionButton = child.GetComponent<Button>(); // Привязка кнопки
                }
                if (floatingXPPrefab == null && (child.name == "Floating_XP_Badge" || child.name.Contains("Floating_XP"))) // Поиск плашки опыта
                {
                    floatingXPPrefab = child.gameObject; // Привязка префаба плашки
                    if (floatingXPSpawnPoint == null) floatingXPSpawnPoint = child.GetComponent<RectTransform>(); // Точка спавна
                    if (floatingXPCanvasGroup == null) floatingXPCanvasGroup = child.GetComponent<CanvasGroup>(); // CanvasGroup плашки
                    if (floatingXPImage == null) floatingXPImage = child.GetComponentInChildren<Image>(true); // Иконка плашки
                }
            }
        }

        // Автоматическая привязка кнопки выпивания Колбы Опыта Мастерства
        if (masteryPotionButton == null && masteryPotionItemObject != null) // Если кнопка колбы не найдена
        {
            masteryPotionButton = masteryPotionItemObject.GetComponent<Button>(); // Получение кнопки
            if (masteryPotionButton == null) // Если компонент отсутствует
            {
                masteryPotionButton = masteryPotionItemObject.AddComponent<Button>(); // Добавление Button
            }
            masteryPotionButton.onClick.RemoveAllListeners(); // Очистка слушателей
            masteryPotionButton.onClick.AddListener(OnMasteryPotionClicked); // Привязка выпивания колбы
        }
    }

    private Coroutine miniCatBubbleHideCoroutine; // Ссылка на корутину автоскрытия облачка котика

    private void SanitizeMiniCatObject() // Очистка и настройка кликабельности спрайта маленького кота
    {
        if (tableMiniCatObject != null) // Если объект кота существует
        {
            // 1. Отключаем лишний дочерний GameObject "Button" с белым фоном, если он был создан
            Transform childBtn = tableMiniCatObject.transform.Find("Button"); // Поиск дочерней кнопки
            if (childBtn != null) // Если найдена
            {
                Image childImg = childBtn.GetComponent<Image>(); // Ссылка на Image
                if (childImg != null) childImg.enabled = false; // Отключение отрисовки
                TextMeshProUGUI childTmp = childBtn.GetComponentInChildren<TextMeshProUGUI>(true); // Текст TMP
                if (childTmp != null) childTmp.text = ""; // Очистка текста
                UnityEngine.UI.Text childTxt = childBtn.GetComponentInChildren<UnityEngine.UI.Text>(true); // Текст Legacy
                if (childTxt != null) childTxt.text = ""; // Очистка текста
                childBtn.gameObject.SetActive(false); // Деактивация лишней кнопки
            }

            // 2. Включаем кликабельность на самом спрайте кота
            Image catImage = tableMiniCatObject.GetComponent<Image>(); // Изображение кота
            if (catImage != null) // Если изображение есть
            {
                catImage.raycastTarget = true; // Разрешение получения лучей клика
            }

            Button catBtn = tableMiniCatObject.GetComponent<Button>(); // Компонент кнопки на коте
            if (catBtn == null) // Если отсутствует
            {
                catBtn = tableMiniCatObject.AddComponent<Button>(); // Добавление Button
            }

            if (catImage != null) // Если Image назначен
            {
                catBtn.targetGraphic = catImage; // Назначение целевого графического элемента
            }

            catBtn.onClick.RemoveAllListeners(); // Сброс старых обработчиков
            catBtn.onClick.AddListener(OnMiniCatClicked); // Привязка клика по коту
        }
    }

    /// <summary>
    /// Шаг 1: Игрок открыл Старый Свиток и нажал кнопку 'Начать' внизу свитка (Скриншот 4 -> Скриншот 3)
    /// </summary>
    public void OnStartCraftButtonClicked() // Обработчик нажатия на кнопку "Начать" в окне рецепта
    {
        if (SettingsManager.Instance != null) // Если менеджер настроек доступен
            SettingsManager.Instance.PlaySoundEffect(craftStartSound); // Звук начала крафта

        if (recipeScrollPanel != null) recipeScrollPanel.SetActive(false); // Закрытие свитка рецепта

        // Панель ресурсов, аватарка, календарь и свиток остаются видимыми, но БЛОКИРУЮТСЯ (заблокированы) на время варки
        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов доступен
        {
            if (DialogueSystem_Manager.Instance.topPanel != null) // Верхняя панель ресурсов
                DialogueSystem_Manager.Instance.topPanel.SetActive(true); // Включение панели
            if (DialogueSystem_Manager.Instance.calendarIconButton != null) // Кнопка календаря
                DialogueSystem_Manager.Instance.calendarIconButton.SetActive(true); // Включение кнопки
            if (DialogueSystem_Manager.Instance.playerAvatarContainer != null) // Контейнер аватара
                DialogueSystem_Manager.Instance.playerAvatarContainer.SetActive(true); // Включение аватара
            if (DialogueSystem_Manager.Instance.smallScrollIconButton != null) // Иконка свитка
                DialogueSystem_Manager.Instance.smallScrollIconButton.SetActive(true); // Включение иконки

            DialogueSystem_Manager.Instance.isCraftingInProgress = true; // Установка флага процесса крафта
            DialogueSystem_Manager.Instance.SetCalendarButtonInteractable(false); // Блокировка календаря
            DialogueSystem_Manager.Instance.SetSmallScrollInteractable(false); // Блокировка свитка
        }

        if (Avatar_Manager.Instance != null) // Если менеджер аватара активен
        {
            Avatar_Manager.Instance.SetAvatarButtonInteractable(false); // Блокировка профиля
        }

        // Автоматически находим и привязываем всю группу стола и котла
        AutoFindAndBindTableElements(); // Поиск элементов сцены

        if (tableCauldronGroup != null) // Если группа стола найдена
        {
            tableCauldronGroup.SetActive(true); // Активация группы
        }

        // Появляется котел и маленький кот на столе (включая родительскую группу Table_Cauldron_Group при наличии)
        if (tableCauldronObject != null) // Если котел существует
        {
            tableCauldronObject.SetActive(true); // Активация котла
            if (tableCauldronObject.transform.parent != null && (tableCauldronObject.transform.parent.name.Contains("Table") || tableCauldronObject.transform.parent.name.Contains("Cauldron"))) // Родительский стол
            {
                tableCauldronObject.transform.parent.gameObject.SetActive(true); // Активация стола
            }
        }
        else // Если котел не был привязан
        {
            GameObject foundTable = GameObject.Find("Table_Cauldron_Group"); // Поиск по имени
            if (foundTable == null) foundTable = GameObject.Find("Table_Group"); // Альтернативное имя
            if (foundTable == null) foundTable = GameObject.Find("Table"); // Альтернативное имя
            if (foundTable != null) // Если найден
            {
                foundTable.SetActive(true); // Активация объекта
                tableCauldronObject = foundTable; // Привязка
            }
        }

        // Если есть cauldronClickButton, убедимся что клик назначен
        if (cauldronClickButton != null) // Если кнопка котла есть
        {
            cauldronClickButton.onClick.RemoveAllListeners(); // Очистка
            cauldronClickButton.onClick.AddListener(OnCauldronClicked); // Привязка клика
        }
        else if (tableCauldronObject != null) // Если кнопка во вложенных объектах
        {
            Button btn = tableCauldronObject.GetComponentInChildren<Button>(true); // Поиск кнопки
            if (btn != null) // Если найдена
            {
                btn.onClick.RemoveAllListeners(); // Очистка
                btn.onClick.AddListener(OnCauldronClicked); // Привязка клика
            }
        }

        if (tableMiniCatObject != null) // Если объект котика есть
        {
            tableMiniCatObject.SetActive(true); // Включение котика
            SanitizeMiniCatObject(); // Настройка кликабельности
        }
        else // Если котик не привязан
        {
            GameObject foundMiniCat = GameObject.Find("Mini_Cat"); // Поиск кота
            if (foundMiniCat == null) foundMiniCat = GameObject.Find("Table_Mini_Cat"); // Альтернативное имя
            if (foundMiniCat != null) // Если найден
            {
                foundMiniCat.SetActive(true); // Активация котика
                tableMiniCatObject = foundMiniCat; // Привязка ссылки
                SanitizeMiniCatObject(); // Настройка
            }
        }

        // Показываем бабл с подсказкой котика на столе
        ShowMiniCatBubble("Нажми на котёл, чтобы начать варить!", false); // Вызов подсказки
    }

    /// <summary>
    /// Клик по маленькому коту на столе — переключает/показывает облачко с подсказкой
    /// </summary>
    public void OnMiniCatClicked() // Клик по маленькому котику на столе
    {
        if (miniCatBubblePanel != null) // Если панель облачка есть
        {
            bool isCurrentActive = miniCatBubblePanel.activeSelf; // Проверка видимости
            if (isCurrentActive) // Если активно
            {
                HideMiniCatBubble(); // Скрытие подсказки
            }
            else // Если скрыто
            {
                ShowMiniCatBubble("Нажми на котёл, чтобы начать варить!", false); // Показ подсказки
            }
        }
    }

    public void ShowMiniCatBubble(string text, bool autoHide = false) // Отображение всплывающей подсказки над котиком
    {
        if (miniCatBubblePanel != null) // Если панель существует
        {
            miniCatBubblePanel.SetActive(true); // Активация панели
            if (miniCatBubbleText != null) // Если текстовое поле есть
            {
                miniCatBubbleText.text = text; // Установка текста подсказки
            }

            if (miniCatBubbleHideCoroutine != null) StopCoroutine(miniCatBubbleHideCoroutine); // Остановка таймера
            if (autoHide) // Если требуется авто-скрытие
            {
                miniCatBubbleHideCoroutine = StartCoroutine(AutoHideMiniCatBubbleRoutine(3.5f)); // Запуск корутины таймера
            }
        }
    }

    public void HideMiniCatBubble() // Скрытие подсказки маленького котика
    {
        if (miniCatBubbleHideCoroutine != null) // Если корутина активна
        {
            StopCoroutine(miniCatBubbleHideCoroutine); // Остановка корутины
            miniCatBubbleHideCoroutine = null; // Обнуление ссылки
        }
        if (miniCatBubblePanel != null) // Если панель есть
        {
            miniCatBubblePanel.SetActive(false); // Деактивация панели
        }
    }

    private IEnumerator AutoHideMiniCatBubbleRoutine(float delay) // Корутина таймера автоматического скрытия облачка
    {
        yield return new WaitForSeconds(delay); // Ожидание задержки
        if (miniCatBubblePanel != null) // Если панель существует
        {
            miniCatBubblePanel.SetActive(false); // Скрытие облачка
        }
        miniCatBubbleHideCoroutine = null; // Обнуление ссылки
    }

    /// <summary>
    /// Шаг 2: Нажатие на котел на столе -> скрывается облачко кота, появляется плашка 'Изготовить'
    /// </summary>
    public void OnCauldronClicked() // Нажатие на котел для перехода к варке
    {
        if (SettingsManager.Instance != null) // Если менеджер настроек доступен
            SettingsManager.Instance.PlaySoundEffect(craftStartSound); // Звук взаимодействия

        HideMiniCatBubble(); // Скрытие облачка подсказки
        if (makeBadgeButtonObject != null) makeBadgeButtonObject.SetActive(true); // Появление кнопки "Изготовить"
    }

    /// <summary>
    /// Шаг 3: Нажатие на 'Изготовить' -> запуск шкалы варки на 5 секунд
    /// </summary>
    public void OnMakeBadgeClicked() // Запуск процесса алхимического приготовления зелья
    {
        if (makeBadgeButtonObject != null) makeBadgeButtonObject.SetActive(false); // Скрытие плашки "Изготовить"

        if (craftCoroutine != null) StopCoroutine(craftCoroutine); // Остановка предыдущей варки
        craftCoroutine = StartCoroutine(CraftingProgressRoutine()); // Запуск корутины таймера варки
    }

    private IEnumerator CraftingProgressRoutine() // Корутина 5-секундного процесса варки со шкалой прогресса
    {
        if (craftingProgressBarContainer != null) craftingProgressBarContainer.SetActive(true); // Показ полосы прогресса
        if (craftingProgressFill != null) craftingProgressFill.fillAmount = 0f; // Сброс заполнения полосы на ноль

        float elapsed = 0f; // Прошедшее время
        while (elapsed < craftDurationSeconds) // Цикл варки
        {
            elapsed += Time.deltaTime; // Прибавление кадра
            float ratio = Mathf.Clamp01(elapsed / craftDurationSeconds); // Вычисление процента заполнения
            if (craftingProgressFill != null) craftingProgressFill.fillAmount = ratio; // Обновление полосы

            float remaining = Mathf.Max(0f, craftDurationSeconds - elapsed); // Оставшееся время
            if (craftingTimerText != null) craftingTimerText.text = $"{remaining:F1}s"; // Обновление таймера на экране

            yield return null; // Ожидание следующего кадра
        }

        if (craftingProgressFill != null) craftingProgressFill.fillAmount = 1f; // Полное заполнение полосы
        if (craftingTimerText != null) craftingTimerText.text = "0.0s"; // Обнуление таймера

        yield return new WaitForSeconds(0.2f); // Небольшая пауза

        if (craftingProgressBarContainer != null) craftingProgressBarContainer.SetActive(false); // Скрытие шкалы варки

        if (SettingsManager.Instance != null) // Если менеджер звуков доступен
            SettingsManager.Instance.PlaySoundEffect(craftCompleteSound); // Звук успешной варки

        // Появляется кнопка 'Забрать'
        if (claimPotionButtonObject != null) claimPotionButtonObject.SetActive(true); // Появление кнопки сбора награды
    }

    /// <summary>
    /// Шаг 4: Нажатие 'Забрать' -> скрытие кнопки, плавный вылет кружка опыта (+10 XP) вверх, затем скрытие котла и начисление XP
    /// </summary>
    public void OnClaimPotionClicked() // Сбор готового сваренного зелья и запуск полета значка опыта
    {
        if (claimPotionButtonObject != null) claimPotionButtonObject.SetActive(false); // Скрытие кнопки "Забрать"
        if (miniCatBubblePanel != null) miniCatBubblePanel.SetActive(false); // Скрытие облачка котика

        StartCoroutine(FlyFloatingXPAndProceed(firstRecipeRewardXP)); // Запуск анимации взлета опыта
    }

    private IEnumerator FlyFloatingXPAndProceed(int xpAmount) // Корутина плавной анимации взлета кружка опыта вверх и начисления XP
    {
        GameObject activeFloatingXP = floatingXPPrefab; // Ссылка на объект плашки
        bool isDynamic = false; // Флаг динамически созданного объекта

        // Если префаб/объект не привязан в инспекторе, создаем красивый динамический кружок опыта
        if (activeFloatingXP == null) // Если префаб отсутствует
        {
            Transform parentCanvas = transform; // Родитель по умолчанию
            Canvas rootCanvas = GetComponentInParent<Canvas>(); // Поиск корневого холста
            if (rootCanvas != null) parentCanvas = rootCanvas.transform; // Использование холста как родителя

            activeFloatingXP = new GameObject("Dynamic_Floating_XP", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)); // Создание объекта плашки
            activeFloatingXP.transform.SetParent(parentCanvas, false); // Назначение родителя
            isDynamic = true; // Пометка динамического создания

            RectTransform drt = activeFloatingXP.GetComponent<RectTransform>(); // Компонент RectTransform
            drt.sizeDelta = new Vector2(76f, 76f); // Размер значка опыта
            drt.anchorMin = new Vector2(0.5f, 0.5f); // Центровка
            drt.anchorMax = new Vector2(0.5f, 0.5f); // Центровка
            drt.pivot = new Vector2(0.5f, 0.5f); // Центровка

            Vector2 spawn = Vector2.zero; // Стартовая точка
            if (floatingXPSpawnPoint != null) // Если точка спавна указана
                spawn = floatingXPSpawnPoint.anchoredPosition; // Координаты точки
            else if (tableCauldronObject != null) // Иначе над котлом
            {
                RectTransform crt = tableCauldronObject.GetComponent<RectTransform>(); // Координаты котла
                if (crt != null) spawn = crt.anchoredPosition + new Vector2(0f, 60f); // Смещение вверх на 60 единиц
            }
            drt.anchoredPosition = spawn; // Установка стартовой позиции

            Image dImg = activeFloatingXP.GetComponent<Image>(); // Компонент изображения
            Sprite s = GetSpriteForXp(xpAmount); // Получение спрайта для количества опыта
            if (s != null) // Если спрайт найден
            {
                dImg.sprite = s; // Установка спрайта
            }
            else // Иначе фоновый цвет
            {
                dImg.color = new Color(0.2f, 0.85f, 0.4f, 1f); // Зеленый оттенок опыта
            }

            // Добавляем красивый текст внутри
            GameObject textObj = new GameObject("XP_Text", typeof(RectTransform), typeof(TextMeshProUGUI)); // Создание текста
            textObj.transform.SetParent(activeFloatingXP.transform, false); // Назначение дочерним
            RectTransform trt = textObj.GetComponent<RectTransform>(); // RectTransform текста
            trt.anchorMin = Vector2.zero; // Растягивание по родителю
            trt.anchorMax = Vector2.one; // Растягивание
            trt.offsetMin = Vector2.zero; // Нулевые отступы
            trt.offsetMax = Vector2.zero; // Нулевые отступы

            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>(); // Компонент TMP
            txt.text = $"+{xpAmount} XP"; // Отображение количества опыта
            txt.alignment = TextAlignmentOptions.Center; // Выравнивание по центру
            txt.fontSize = 20f; // Размер шрифта
            txt.color = Color.white; // Белый цвет текста
            txt.fontStyle = FontStyles.Bold; // Жирное начертание
        }

        if (activeFloatingXP != null) // Если объект готов к показу
        {
            activeFloatingXP.SetActive(true); // Включение объекта

            Image targetImg = floatingXPImage; // Поиск изображения
            if (targetImg == null) // Если не привязано
            {
                targetImg = activeFloatingXP.GetComponent<Image>(); // Поиск на самом объекте
                if (targetImg == null) targetImg = activeFloatingXP.GetComponentInChildren<Image>(true); // Поиск в дочерних
            }

            if (targetImg != null) // Если изображение найдено
            {
                targetImg.preserveAspect = true; // Сохранение пропорций
                if (floatingXPImage != null) // Если иконка привязана
                {
                    Sprite chosenSprite = GetSpriteForXp(xpAmount); // Подбор спрайта опыта
                    if (chosenSprite != null) targetImg.sprite = chosenSprite; // Назначение спрайта
                }
            }

            RectTransform rt = activeFloatingXP.GetComponent<RectTransform>(); // Ссылка на RectTransform
            if (rt != null) // Если есть
            {
                // Сохраняем исходные размеры объекта, настроенные пользователем в Inspector
                rt.localScale = Vector3.one; // Базовый масштаб
            }

            CanvasGroup cg = activeFloatingXP.GetComponent<CanvasGroup>(); // Ссылка на CanvasGroup
            if (cg == null) cg = activeFloatingXP.AddComponent<CanvasGroup>(); // Добавление при отсутствии

            Vector2 startPos = rt != null ? rt.anchoredPosition : Vector2.zero; // Стартовая позиция
            if (floatingXPSpawnPoint != null) startPos = floatingXPSpawnPoint.anchoredPosition; // Позиция из точки спавна
            if (rt != null) rt.anchoredPosition = startPos; // Установка позиции

            float duration = 1.6f; // Длительность полета
            float elapsed = 0f; // Таймер анимации

            while (elapsed < duration) // Анимационный цикл взлета
            {
                elapsed += Time.deltaTime; // Прибавление кадра
                float t = Mathf.Clamp01(elapsed / duration); // Нормализованное время от 0 до 1

                // Плавный взлет вверх с замедлением к концу
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f); // Функция плавности взлета
                if (rt != null) // Если RectTransform есть
                {
                    rt.anchoredPosition = startPos + new Vector2(0f, easeOut * 150f); // Подъем на 150 пикселей вверх
                    float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f; // Пульсация масштаба
                    rt.localScale = new Vector3(scale, scale, 1f); // Обновление масштаба
                }

                if (cg != null) // Если есть CanvasGroup
                {
                    cg.alpha = (t > 0.6f) ? Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f) : 1f; // Растворение в конце полета
                }

                yield return null; // Ожидание следующего кадра
            }

            if (isDynamic) // Если объект был динамическим
            {
                Destroy(activeFloatingXP); // Удаление временного объекта
            }
            else // Если использовался объект сцены
            {
                activeFloatingXP.SetActive(false); // Скрытие объекта
                if (cg != null) cg.alpha = 1f; // Восстановление непрозрачности
                if (rt != null) rt.anchoredPosition = startPos; // Сброс позиции
            }
        }
        else // Если плашки не было
        {
            yield return new WaitForSeconds(0.4f); // Небольшая задержка
        }

        // Скрываем котел и помощника только после завершения полета кружка опыта
        if (tableCauldronObject != null) tableCauldronObject.SetActive(false); // Скрытие котла
        if (tableMiniCatObject != null) tableMiniCatObject.SetActive(false); // Скрытие котика

        // Начисление опыта в профиль (10/10 XP -> повышает уровень до 2 Ур. 0/20 XP)
        if (Avatar_Manager.Instance != null) // Если менеджер аватара активен
        {
            Avatar_Manager.Instance.AddExperience(xpAmount); // Начисление заработанного опыта
        }

        // Запуск финальной фазы диалога с основным котом
        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов активен
        {
            DialogueSystem_Manager.Instance.isCraftingInProgress = false; // Сброс состояния варки
            DialogueSystem_Manager.Instance.StartPostCraftChestDialogue(); // Запуск диалога о сундуке
        }
    }

    private Sprite GetSpriteForXp(int xp) // Получение спрайта значка опыта в зависимости от количества начисляемого XP
    {
        switch (xp) // Выбор значка по числу опыта
        {
            case 5: return xpBadge5; // +5 XP
            case 10: return xpBadge10; // +10 XP
            case 20: return xpBadge20; // +20 XP
            case 30: return xpBadge30; // +30 XP
            case 50: return xpBadge50; // +50 XP
            case 100: return xpBadge100; // +100 XP
            case 200: return xpBadge200; // +200 XP
            case 300: return xpBadge300; // +300 XP
            case 500: return xpBadge500; // +500 XP
            case 1000: return xpBadge1000; // +1000 XP
            default: return xpBadge10; // По умолчанию +10 XP
        }
    }

    /// <summary>
    /// Шаг 5: Нажатие на сундучок в верхнем левом меню
    /// </summary>
    public void OnChestButtonClicked() // Нажатие на иконку сундука в верхнем меню
    {
        if (SettingsManager.Instance != null) // Если менеджер настроек активен
            SettingsManager.Instance.PlaySoundEffect(chestOpenSound); // Звук открытия сундука

        OpenInventory(); // Открытие панели инвентаря
    }

    [ContextMenu("Сбросить Прогресс Крафта и Сундука (Reset Crafting & Chest)")]
    public void ResetCraftingAndChestProgress() // Сброс прогресса первого крафта и колбы сундука для отладки
    {
        PlayerPrefs.DeleteKey("Mastery_Flask_Consumed"); // Удаление флага выпитой колбы
        PlayerPrefs.DeleteKey("First_Recipe_Done"); // Удаление флага завершения рецепта
        PlayerPrefs.DeleteKey("Tutorial_Recipe_Done"); // Удаление флага туториала
        PlayerPrefs.Save(); // Сохранение на диск
        isMasteryPotionConsumed = false; // Сброс флага
        if (masteryPotionItemObject != null) masteryPotionItemObject.SetActive(true); // Включение колбы
        if (inventoryCloseButton != null) inventoryCloseButton.interactable = false; // Блокировка кнопки выхода
        Debug.Log("[RecipeCrafting_Manager] Прогресс крафта и колбы сундука успешно сброшен!"); // Логирование
    }

    public void OpenInventory() // Открытие панели инвентаря сундука и инициализация слотов
    {
        if (DialogueSystem_Manager.Instance != null && DialogueSystem_Manager.Instance.dialoguePanel != null) // Если открыт диалог
        {
            DialogueSystem_Manager.Instance.dialoguePanel.SetActive(false); // Скрытие диалога
        }

        // Скрываем верхнюю панель ресурсов, чтобы не накладывалась на заголовок сундука
        HideTopResources(); // Скрытие верхней панели

        if (inventoryPanel != null) // Если панель инвентаря есть
        {
            inventoryPanel.SetActive(true); // Включение окна инвентаря
            SanitizeInventoryTitle(); // Корректировка заголовка
        }

        EnsureInventorySlots(); // Проверка и создание 100 ячеек инвентаря

        // Проверяем, выпита ли колба опыта мастерства
        isMasteryPotionConsumed = PlayerPrefs.GetInt("Mastery_Flask_Consumed", 0) == 1; // Загрузка статуса колбы

        // Проверяем/привязываем колбу в 1-м слоте
        EnsureMasteryPotionInFirstSlot(); // Создание/поиск колбы мастерства

        if (masteryPotionItemObject != null) // Если объект колбы есть
        {
            masteryPotionItemObject.SetActive(!isMasteryPotionConsumed); // Видимость колбы зависит от того, выпита ли она
        }

        // Если колба еще не выпита - блокируем крестик закрытия. Если выпита - разблокируем
        if (inventoryCloseButton != null) // Если кнопка закрытия есть
        {
            inventoryCloseButton.interactable = isMasteryPotionConsumed; // Блокировка/разблокировка выхода
        }
    }

    private void HideTopResources() // Скрытие верхней панели ресурсов при открытии полноэкранных окон
    {
        GameObject topPanel = GameObject.Find("TopPanel"); // Поиск верхней панели
        if (topPanel != null) topPanel.SetActive(false); // Отключение
        GameObject headerPlate = GameObject.Find("Header_Plate"); // Поиск плашки заголовка
        if (headerPlate != null) headerPlate.SetActive(false); // Отключение
    }

    private void RestoreTopResources() // Восстановление отображения верхней панели ресурсов после закрытия окон
    {
        GameObject topPanel = GameObject.Find("TopPanel"); // Поиск верхней панели
        if (topPanel != null) topPanel.SetActive(true); // Включение панели
        GameObject headerPlate = GameObject.Find("Header_Plate"); // Поиск плашки заголовка
        if (headerPlate != null) headerPlate.SetActive(true); // Включение плашки
    }

    private void SanitizeInventoryTitle() // Очистка и центровка текста заголовка окна сундука
    {
        if (inventoryPanel == null) return; // Пропуск если панели нет
        TextMeshProUGUI[] tmps = inventoryPanel.GetComponentsInChildren<TextMeshProUGUI>(true); // Все TMP заголовки
        foreach (var t in tmps) // Перебор текстовых полей
        {
            if (t.name.Contains("Title") || t.text.Contains("Инвентарь") || t.text.Contains("Сундук")) // Поиск заголовка
            {
                t.text = t.text.Replace("(Инвентарь)", "").Replace("Инвентарь", "").Trim(); // Удаление лишних слов
                if (string.IsNullOrEmpty(t.text)) t.text = "Сундук Алхимика"; // Дефолтный текст
                t.alignment = TextAlignmentOptions.Center; // Выравнивание по центру

                RectTransform rt = t.GetComponent<RectTransform>(); // RectTransform заголовка
                if (rt != null) // Настройка якорей
                {
                    rt.anchorMin = new Vector2(0.5f, 1f); // Верхний центр
                    rt.anchorMax = new Vector2(0.5f, 1f); // Верхний центр
                    rt.pivot = new Vector2(0.5f, 1f); // Верхний центр
                    rt.anchoredPosition = new Vector2(0f, -22f); // Отступ сверху
                }
            }
        }
        UnityEngine.UI.Text[] texts = inventoryPanel.GetComponentsInChildren<UnityEngine.UI.Text>(true); // Legacy Text поля
        foreach (var t in texts) // Перебор текстовых полей
        {
            if (t.name.Contains("Title") || t.text.Contains("Инвентарь") || t.text.Contains("Сундук")) // Поиск заголовка
            {
                t.text = t.text.Replace("(Инвентарь)", "").Replace("Инвентарь", "").Trim(); // Удаление лишних слов
                if (string.IsNullOrEmpty(t.text)) t.text = "Сундук Алхимика"; // Дефолтный текст
                t.alignment = TextAnchor.MiddleCenter; // Выравнивание по центру

                RectTransform rt = t.GetComponent<RectTransform>(); // RectTransform заголовка
                if (rt != null) // Настройка якорей
                {
                    rt.anchorMin = new Vector2(0.5f, 1f); // Верхний центр
                    rt.anchorMax = new Vector2(0.5f, 1f); // Верхний центр
                    rt.pivot = new Vector2(0.5f, 1f); // Верхний центр
                    rt.anchoredPosition = new Vector2(0f, -22f); // Отступ сверху
                }
            }
        }
    }

    private void EnsureMasteryPotionInFirstSlot() // Создание или привязка объекта колбы опыта мастерства в первом слоте инвентаря
    {
        if (inventorySlotsContent != null && inventorySlotsContent.childCount > 0) // Если слоты существуют
        {
            Transform firstSlot = inventorySlotsContent.GetChild(0); // Получение первого слота

            if (masteryPotionItemObject == null) // Если колба еще не привязана
            {
                // Ищем существующий объект колбы внутри 1-го слота
                Transform found = firstSlot.Find("Mastery_Potion_Flask"); // Поиск по имени
                if (found == null) found = firstSlot.Find("Potion_Item"); // Альтернативное имя
                if (found == null) found = firstSlot.Find("Flask"); // Альтернативное имя

                if (found != null) // Если найден объект
                {
                    masteryPotionItemObject = found.gameObject; // Привязка ссылки
                }
                else // Иначе создаем новую плашку колбы
                {
                    // Создаем плашку колбы опыта мастерства в первом слоте
                    GameObject flaskObj = new GameObject("Mastery_Potion_Flask", typeof(RectTransform), typeof(Image), typeof(Button)); // Создание объекта
                    flaskObj.transform.SetParent(firstSlot, false); // Вложение в 1-й слот

                    RectTransform frt = flaskObj.GetComponent<RectTransform>(); // RectTransform колбы
                    frt.anchorMin = Vector2.zero; // Растягивание по слоту
                    frt.anchorMax = Vector2.one; // Растягивание
                    frt.offsetMin = new Vector2(8, 8); // Отступ
                    frt.offsetMax = new Vector2(-8, -8); // Отступ

                    Image fImg = flaskObj.GetComponent<Image>(); // Изображение колбы
                    if (xpBadge100 != null) // Если спрайт +100 XP доступен
                    {
                        fImg.sprite = xpBadge100; // Назначение спрайта
                    }
                    else if (floatingXPImage != null && floatingXPImage.sprite != null) // Иначе спрайт плашки
                    {
                        fImg.sprite = floatingXPImage.sprite; // Назначение спрайта
                    }
                    else // Иначе цвет
                    {
                        fImg.color = new Color(0.2f, 0.9f, 0.6f, 0.95f); // Бирюзовый оттенок
                    }

                    masteryPotionItemObject = flaskObj; // Сохранение ссылки на колбу
                }
            }

            if (masteryPotionItemObject != null) // Если колба активна
            {
                masteryPotionButton = masteryPotionItemObject.GetComponent<Button>(); // Получение Button
                if (masteryPotionButton == null) masteryPotionButton = masteryPotionItemObject.AddComponent<Button>(); // Добавление Button
                masteryPotionButton.onClick.RemoveAllListeners(); // Очистка слушателей
                masteryPotionButton.onClick.AddListener(OnMasteryPotionClicked); // Привязка клика выпивания
            }
        }
    }

    /// <summary>
    /// Автоматическая генерация или проверка 100 ячеек инвентаря
    /// </summary>
    public void EnsureInventorySlots() // Автоматическая генерация и проверка сетки 100 ячеек инвентаря (5 колонок)
    {
        if (inventorySlotsContent == null) return; // Пропуск если контейнер не задан

        // Настраиваем сетку (GridLayoutGroup) на 5 колонок если компонент есть
        GridLayoutGroup grid = inventorySlotsContent.GetComponent<GridLayoutGroup>(); // Компонент сетки
        if (grid != null) // Если сетка найдена
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // Фиксированное число колонок
            grid.constraintCount = columnsCount; // 5 колонок
        }

        // Если в инвентаре уже есть дочерние слоты и их меньше 100, дополняем при наличии префаба или клонируя первый слот
        if (inventorySlotPrefab != null) // Если назначен префаб слота
        {
            int currentChildCount = inventorySlotsContent.childCount; // Текущее число слотов
            for (int i = currentChildCount; i < totalSlots; i++) // Досоздание до 100 слотов
            {
                GameObject newSlot = Instantiate(inventorySlotPrefab, inventorySlotsContent); // Создание слота
                newSlot.name = $"Inventory_Slot_Hex_{i + 1}"; // Имя слота
            }
        }
        else if (inventorySlotsContent.childCount > 0 && inventorySlotsContent.childCount < totalSlots) // Клонирование первого слота
        {
            GameObject template = inventorySlotsContent.GetChild(0).gameObject; // Шаблон слота
            int currentChildCount = inventorySlotsContent.childCount; // Текущее количество слотов
            for (int i = currentChildCount; i < totalSlots; i++) // Клонирование до 100 слотов
            {
                GameObject newSlot = Instantiate(template, inventorySlotsContent); // Создание клона
                newSlot.name = $"Inventory_Slot_Hex_{i + 1}"; // Имя слота
            }
        }
    }

    /// <summary>
    /// Шаг 6: Игрок нажимает на Колбу Опыта Мастерства в 1-м слоте (+100 XP)
    /// </summary>
    public void OnMasteryPotionClicked() // Нажатие на колбу опыта мастерства (+100 XP) и повышение ранга
    {
        if (isMasteryPotionConsumed) return; // Выход если уже выпита
        isMasteryPotionConsumed = true; // Установка флага выпивания

        if (potionConsumeSound != null && SettingsManager.Instance != null) // Звук выпивания
            SettingsManager.Instance.PlaySoundEffect(potionConsumeSound); // Воспроизведение звука
        else if (craftCompleteSound != null && SettingsManager.Instance != null) // Альтернативный звук
            SettingsManager.Instance.PlaySoundEffect(craftCompleteSound); // Воспроизведение звука

        // Колба пропадает из слота
        if (masteryPotionItemObject != null) // Если объект колбы есть
        {
            masteryPotionItemObject.SetActive(false); // Скрытие колбы из слота
        }

        // Начисляем 100 опыта мастерства (переход с Новичка на Новичок-травник)
        if (Avatar_Manager.Instance != null) // Если менеджер аватара активен
        {
            Avatar_Manager.Instance.AddMasteryExperience(100); // Начисление +100 опыта мастерства
        }

        // Активируем кнопку-крестик выхода из инвентаря
        if (inventoryCloseButton != null) // Если кнопка закрытия есть
        {
            inventoryCloseButton.interactable = true; // Разблокировка кнопки выхода
        }
    }

    /// <summary>
    /// Шаг 7: Игрок нажимает крестик выхода из инвентаря -> блокировка сундука, переход к диалогу о Знаниях
    /// </summary>
    public void OnInventoryCloseClicked() // Закрытие инвентаря, возврат панели ресурсов и переход к диалогу о Знаниях
    {
        if (inventoryPanel != null) // Если панель инвентаря есть
        {
            inventoryPanel.SetActive(false); // Скрытие инвентаря
        }

        // Восстанавливаем отображение верхней панели ресурсов
        RestoreTopResources(); // Возврат верхней панели ресурсов

        if (DialogueSystem_Manager.Instance != null) // Если менеджер диалогов активен
        {
            // Блокируем сундук
            DialogueSystem_Manager.Instance.SetChestButtonInteractable(false); // Блокировка кнопки сундука

            // Запускаем диалог про раздел 'Знания' и повышение до 'Новичок-травник'
            DialogueSystem_Manager.Instance.StartPostMasteryKnowledgeDialogue(); // Запуск диалога
        }
    }

    /// <summary>
    /// Добавление зелья или предмета в первый свободный слот инвентаря
    /// </summary>
    public void AddPotionToFirstEmptySlot(string potionId, string potionTitle) // Сохранение добавленного зелья или предмета в память инвентаря
    {
        int currentCount = PlayerPrefs.GetInt($"Item_Count_{potionId}", 0); // Текущее количество предмета
        PlayerPrefs.SetInt($"Item_Count_{potionId}", currentCount + 1); // Увеличение счетчика на 1
        PlayerPrefs.SetString($"Item_Name_{potionId}", potionTitle); // Сохранение имени предмета
        PlayerPrefs.Save(); // Запись в реестр PlayerPrefs
        Debug.Log($"[INVENTORY] Награда {potionTitle} ({potionId}) успешно добавлена в инвентарь (Количество: {currentCount + 1})."); // Логирование
    }
}
