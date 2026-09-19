using System.Collections; // Подключение пространств имен коллекций
using System.Collections.Generic; // Подключение обобщенных коллекций
using UnityEngine; // Подключение базового функционала Unity
using UnityEngine.UI; // Подключение стандартных компонентов UI
using TMPro; // Подключение расширенного текста TextMeshPro

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core v18.12.98)
/// Мини-игра «Защита Алхимических Котлов» (Cauldron Rain Defense).
/// Игрок спасает бурлящие котлы от налетающих грозовых туч, вовремя раскрывая защитные силовые зонтики.
/// </summary>
public class CauldronDefense_Minigame : MonoBehaviour // Основной класс мини-игры защиты котлов
{
    public static CauldronDefense_Minigame Instance { get; private set; } // Статический синглтон мини-игры

    [Header("=== Основные корневые панели ===")]
    public GameObject gameRootPanel; // Корневой объект всей мини-игры (CauldronDefense_Game_Panel)
    public GameObject difficultyPanel; // Окно выбора уровня сложности (Difficulty_Select_Panel)
    public GameObject activeStagePanel; // Игровое поле с котлами и тучами (Active_Defense_Stage_Panel)
    public GameObject resultSummaryPanel; // Итоговое окно победы / поражения (Result_Summary_Panel)
    public GameObject topBar; // Верхняя панель заголовка и индикаторов (Top_Bar)
    public Button closeGameButton; // Кнопка закрытия мини-игры (Close_Button)

    [Header("=== Кнопки выбора сложности ===")]
    [SerializeField] private Button easyButton; // Кнопка легкой сложности (Easy_Button)
    [SerializeField] private Button mediumButton; // Кнопка средней сложности (Medium_Button)
    [SerializeField] private Button hardButton; // Кнопка сложной сложности (Hard_Button)

    [Header("=== Справочник / Подсказка игры ===")]
    [SerializeField] private Button guideButton; // Кнопка вызова значка подсказки (?)
    [SerializeField] private GameObject guidePopupPanel; // Всплывающее окно со справочником игры
    [SerializeField] private Button guideCloseButton; // Крестик закрытия окна подсказки

    [Header("=== Жизни и Счетчики (Геймплейные индикаторы) ===")]
    [SerializeField] private GameObject heartsContainer; // Контейнер 3 сердечек (Hearts_Container)
    [SerializeField] private Image[] heartIcons; // 3 сердечка жизней игрока
    [SerializeField] private TextMeshProUGUI defenseCounterText; // Счетчик успешных защит (Защищено: 0 / 10)
    [SerializeField] private TextMeshProUGUI levelDifficultyText; // Текст текущей сложности

    [Header("=== Котлы и Защитные Зонтики ===")]
    [SerializeField] private GameObject[] cauldronRoots; // Слоты для котлов (3, 4 или 5 в зависимости от сложности)
    [SerializeField] private Button[] cauldronButtons; // Кнопки под котлами для активации зонтиков
    [SerializeField] private GameObject[] umbrellaVisuals; // Магические зонтики над каждым котлом
    [SerializeField] private GameObject[] cloudVisuals; // Грозовые тучки над каждым котлом
    [SerializeField] private ParticleSystem[] rainParticles; // Частицы дождя под каждой тучкой

    [Header("=== Итоговое окно наград ===")]
    [SerializeField] private TextMeshProUGUI resultTitleText; // Заголовок (Победа! / Попробуй снова!)
    [SerializeField] private TextMeshProUGUI rewardGoldText; // Награда золотом
    [SerializeField] private TextMeshProUGUI rewardPlayerXpText; // Награда Опыт Игрока
    [SerializeField] private TextMeshProUGUI rewardMasteryXpText; // Награда Опыт Мастерства
    [SerializeField] private Button claimRewardsButton; // Кнопка «Забрать все в рюкзак»

    [Header("=== Интеграция с диалогами и переход в Лабораторию ===")]
    [SerializeField] private GameObject laboratoryGamePanel; // Панель «Laboratory_Mixer»
    [SerializeField] private GameObject dialogueContainer; // Контейнер окна диалога с Котом
    [SerializeField] private TextMeshProUGUI dialogueText; // Текст диалога Кота

    public enum DefenseDifficulty { Easy, Medium, Hard } // Перечисление уровней сложности
    private DefenseDifficulty currentDifficulty = DefenseDifficulty.Easy; // Выбранная сложность

    private int maxLives = 3; // Максимальное количество жизней
    private int currentLives = 3; // Текущие жизни
    private int targetDefenses = 10; // Целевое число отраженных туч (10, 15, 20)
    private int currentDefenses = 0; // Текущее число защит
    private bool isPlaying = false; // Флаг активного геймплея
    private bool isPausedByGuide = false; // Флаг паузы при открытом справочнике

    private float cloudInterval = 2.2f; // Интервал между тучами
    private int activeCloudIndex = -1; // Индекс котла с активной тучей
    private Coroutine gameLoopCoroutine; // Ссылка на корутину игрового цикла

    private void Awake() // Инициализация ссылок и обработчиков событий
    {
        if (Instance == null) Instance = this; // Назначение синглтона
        else if (Instance != this) { Destroy(gameObject); return; } // Защита от дубликатов

        EnsureUIHierarchy(); // Автоматический поиск и привязка компонентов UI

        if (guideButton != null) guideButton.onClick.AddListener(OpenGuide); // Привязка кнопки подсказки
        if (guideCloseButton != null) guideCloseButton.onClick.AddListener(CloseGuide); // Привязка закрытия подсказки
        if (closeGameButton != null) closeGameButton.onClick.AddListener(CloseGame); // Привязка выхода из игры
        if (claimRewardsButton != null) claimRewardsButton.onClick.AddListener(ClaimAllRewards); // Привязка кнопки сбора наград

        if (easyButton != null) easyButton.onClick.AddListener(StartEasyMode); // Привязка легкого режима
        if (mediumButton != null) mediumButton.onClick.AddListener(StartMediumMode); // Привязка среднего режима
        if (hardButton != null) hardButton.onClick.AddListener(StartHardMode); // Привязка сложного режима

        if (cauldronButtons != null) // Привязка кликов по котлам
        {
            for (int i = 0; i < cauldronButtons.Length; i++)
            {
                int index = i; // Локальная копия индекса для замыкания
                if (cauldronButtons[i] != null)
                {
                    cauldronButtons[i].onClick.AddListener(() => OnCauldronShieldClicked(index)); // Активация зонтика
                }
            }
        }
    }

    private void OnEnable() // Событие включения объекта
    {
        EnsureUIHierarchy(); // Повторная валидация иерархии
        DeactivateOtherMinigames(); // Отключение всех соседних мини-игр (мышей, рыбалки, поиска)
        if (!isPlaying) // Если игра еще не запущена
        {
            ShowDifficultySelection(); // Гарантированный показ только окна выбора сложности
        }
    }

    private void OnDisable() // Событие отключения объекта
    {
        isPlaying = false; // Сброс состояния игры
        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine); // Остановка игрового цикла
    }

    /// <summary>
    /// Полное отключение всех посторонних панелей и скриптов мини-игр (Поймай Мышь, Рыбалка, Поиск Предметов, Лаборатория)
    /// для исключения наложения чужих элементов интерфейса.
    /// </summary>
    public void DeactivateOtherMinigames() // Полная очистка соседних мини-игр
    {
        // 1. Отключение синглтонов других мини-игр
        if (CatchMouse_Minigame.Instance != null && CatchMouse_Minigame.Instance.gameObject != gameObject)
        {
            CatchMouse_Minigame.Instance.CloseMinigame(); // Закрытие мышек
            CatchMouse_Minigame.Instance.gameObject.SetActive(false); // Деактивация объекта мышей
        }
        if (AlchemyFishing_Minigame.Instance != null && AlchemyFishing_Minigame.Instance.gameObject != gameObject)
        {
            if (AlchemyFishing_Minigame.Instance.rootFishingGamePanel != null)
                AlchemyFishing_Minigame.Instance.rootFishingGamePanel.SetActive(false); // Скрытие панели рыбалки
            AlchemyFishing_Minigame.Instance.gameObject.SetActive(false); // Отключение объекта рыбалки
        }
        if (HiddenObject_Minigame.Instance != null && HiddenObject_Minigame.Instance.gameObject != gameObject)
        {
            HiddenObject_Minigame.Instance.gameObject.SetActive(false); // Отключение объекта поиска предметов
        }
        if (Laboratory_Mixer.Instance != null && Laboratory_Mixer.Instance.gameObject != gameObject)
        {
            if (Laboratory_Mixer.Instance.laboratoryRootPanel != null)
                Laboratory_Mixer.Instance.laboratoryRootPanel.SetActive(false); // Скрытие панели лаборатории
            Laboratory_Mixer.Instance.gameObject.SetActive(false); // Отключение лаборатории
        }

        // 2. Отключение всех соседних дочерних объектов внутри родительской панели (MinigamesPanel)
        if (transform.parent != null)
        {
            for (int i = 0; i < transform.parent.childCount; i++)
            {
                Transform sibling = transform.parent.GetChild(i);
                if (sibling != null && sibling.gameObject != gameObject)
                {
                    sibling.gameObject.SetActive(false); // Деактивация соседних панелей
                }
            }
        }

        // 3. Запасной поиск по именам игровых объектов в сцене
        string[] otherPanelNames = {
            "CatchMouse_Game_Panel", "CatchMouse_Panel", "CatchMousePanel",
            "AlchemyFishing_Game_Panel", "AlchemyFishing_Panel", "Fishing_Minigame_Panel",
            "HiddenObject_Game_Panel", "HiddenObject_Panel", "HiddenObjectPanel",
            "Laboratory_Game_Panel", "Laboratory_Panel", "LaboratoryPanel", "Laboratory_Minigame"
        };
        foreach (var name in otherPanelNames)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == name && go != gameObject && go.scene.isLoaded)
                {
                    go.SetActive(false); // Гарантированное выключение постороннего окна
                }
            }
        }
    }

    /// <summary>
    /// Автоматический поиск и инициализация всех компонентов интерфейса
    /// </summary>
    public void EnsureUIHierarchy() // Поиск и привязка UI элементов
    {
        if (gameRootPanel == null) gameRootPanel = gameObject; // Корневой контейнер

        // 1. Поиск панели выбора сложности
        if (difficultyPanel == null)
        {
            Transform t = transform.Find("Difficulty_Select_Panel");
            if (t == null) t = transform.Find("Difficulty_Selection_Panel");
            if (t == null) t = transform.Find("DifficultyPanel");
            if (t != null) difficultyPanel = t.gameObject;
        }

        // 2. Поиск игрового поля
        if (activeStagePanel == null)
        {
            Transform t = transform.Find("Active_Defense_Stage_Panel");
            if (t == null) t = transform.Find("Active_Stage_Panel");
            if (t == null) t = transform.Find("ActiveStagePanel");
            if (t != null) activeStagePanel = t.gameObject;
        }

        // 3. Поиск итоговой панели наград
        if (resultSummaryPanel == null)
        {
            Transform t = transform.Find("Result_Summary_Panel");
            if (t == null) t = transform.Find("ResultSummaryPanel");
            if (t != null) resultSummaryPanel = t.gameObject;
        }

        // 4. Поиск справочника
        if (guidePopupPanel == null)
        {
            Transform t = transform.Find("Guide_Popup_Panel");
            if (t == null) t = transform.Find("GuidePopupPanel");
            if (t != null) guidePopupPanel = t.gameObject;
        }

        // 5. Поиск верхней панели
        if (topBar == null)
        {
            Transform t = transform.Find("Top_Bar");
            if (t == null) t = transform.Find("TopBar");
            if (t != null) topBar = t.gameObject;
        }

        // 6. Поиск кнопок сложности внутри панели сложности
        if (difficultyPanel != null)
        {
            if (easyButton == null) easyButton = FindChildButton(difficultyPanel.transform, "Easy_Button", "EasyButton", "Button_Easy");
            if (mediumButton == null) mediumButton = FindChildButton(difficultyPanel.transform, "Medium_Button", "MediumButton", "Button_Medium");
            if (hardButton == null) hardButton = FindChildButton(difficultyPanel.transform, "Hard_Button", "HardButton", "Button_Hard");
        }

        // 7. Поиск кнопок управления
        if (closeGameButton == null && topBar != null)
        {
            closeGameButton = FindChildButton(topBar.transform, "Close_Button", "CloseButton", "Exit_Button");
        }
        if (closeGameButton == null)
        {
            closeGameButton = FindChildButton(transform, "Close_Button", "CloseButton", "Exit_Button");
        }

        if (guideButton == null && topBar != null)
        {
            guideButton = FindChildButton(topBar.transform, "Guide_Button", "GuideButton", "Help_Button");
        }

        // 8. Поиск счетчиков и жизней
        if (defenseCounterText == null)
        {
            Transform t = (topBar != null) ? topBar.transform.Find("Wave_Counter_Text") : null;
            if (t == null && topBar != null) t = topBar.transform.Find("Defense_Counter_Text");
            if (t == null) t = transform.Find("Top_Bar/Wave_Counter_Text");
            if (t != null) defenseCounterText = t.GetComponent<TextMeshProUGUI>();
        }

        if (heartsContainer == null)
        {
            Transform t = (topBar != null) ? topBar.transform.Find("Hearts_Container") : null;
            if (t == null && topBar != null) t = topBar.transform.Find("Hearts");
            if (t == null) t = transform.Find("Top_Bar/Hearts_Container");
            if (t != null) heartsContainer = t.gameObject;
        }

        if (heartsContainer != null && (heartIcons == null || heartIcons.Length == 0))
        {
            heartIcons = heartsContainer.GetComponentsInChildren<Image>(true);
        }

        // 9. Поиск элементов итогового окна
        if (resultSummaryPanel != null)
        {
            if (claimRewardsButton == null)
                claimRewardsButton = FindChildButton(resultSummaryPanel.transform, "Claim_Rewards_Button", "ClaimButton", "Claim_All_Button");

            if (resultTitleText == null)
            {
                Transform t = resultSummaryPanel.transform.Find("Result_Title_Text");
                if (t != null) resultTitleText = t.GetComponent<TextMeshProUGUI>();
            }
            if (rewardGoldText == null)
            {
                Transform t = resultSummaryPanel.transform.Find("Reward_Gold_Text");
                if (t != null) rewardGoldText = t.GetComponent<TextMeshProUGUI>();
            }
            if (rewardPlayerXpText == null)
            {
                Transform t = resultSummaryPanel.transform.Find("Reward_Player_XP_Text");
                if (t != null) rewardPlayerXpText = t.GetComponent<TextMeshProUGUI>();
            }
            if (rewardMasteryXpText == null)
            {
                Transform t = resultSummaryPanel.transform.Find("Reward_Mastery_XP_Text");
                if (t != null) rewardMasteryXpText = t.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private Button FindChildButton(Transform parent, params string[] possibleNames) // Утилита безопасного поиска кнопки
    {
        if (parent == null) return null;
        foreach (var n in possibleNames)
        {
            Transform child = parent.Find(n);
            if (child != null)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null) return btn;
            }
        }
        foreach (Button b in parent.GetComponentsInChildren<Button>(true))
        {
            foreach (var n in possibleNames)
            {
                if (b.gameObject.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return b;
            }
        }
        return null;
    }

    /// <summary>
    /// Открытие окна мини-игры «Защита Котлов» с показом только меню сложности
    /// </summary>
    public void OpenMinigame() // Открытие мини-игры
    {
        EnsureUIHierarchy(); // Проверка привязок
        DeactivateOtherMinigames(); // Гарантированное отключение мышей и других игр
        gameObject.SetActive(true); // Активация объекта
        Transform p = transform.parent;
        while (p != null)
        {
            p.gameObject.SetActive(true); // Включение родителей
            p = p.parent;
        }

        if (gameRootPanel != null) gameRootPanel.SetActive(true); // Включение корневой панели
        ShowDifficultySelection(); // Включение исключительно меню выбора сложности
    }

    /// <summary>
    /// Показ экрана выбора сложности с полным скрытием геймплейных счетчиков и полей
    /// </summary>
    public void ShowDifficultySelection() // Показ меню сложности
    {
        isPlaying = false; // Остановка игрового процесса
        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine); // Остановка корутины
        DeactivateOtherMinigames(); // Гарантированное выключение посторонних панелей (мышей и рыбалки)

        // 1. Активация панели выбора сложности на переднем плане
        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(true); // Включение окна сложности
            difficultyPanel.transform.SetAsLastSibling(); // Вынос на передний план
        }

        // 2. Скрытие игрового поля и окон результатов
        if (activeStagePanel != null) activeStagePanel.SetActive(false); // Скрытие поля с котлами
        if (resultSummaryPanel != null) resultSummaryPanel.SetActive(false); // Скрытие наград
        if (guidePopupPanel != null) guidePopupPanel.SetActive(false); // Скрытие подсказки

        // 3. Скрытие элементов активного геймплея из верхней панели (жизни и счетчик защит)
        if (heartsContainer != null) heartsContainer.SetActive(false); // Скрытие 3 сердечек
        if (defenseCounterText != null) defenseCounterText.gameObject.SetActive(false); // Скрытие счетчика "Защищено"
        if (levelDifficultyText != null) levelDifficultyText.gameObject.SetActive(false); // Скрытие текста сложности

        // 4. Гарантия активности верхней полосы с кнопками выхода и справки
        if (topBar != null) topBar.SetActive(true); // Включение полосы заголовка
        if (closeGameButton != null) closeGameButton.gameObject.SetActive(true); // Включение крестика закрытия
        if (guideButton != null) guideButton.gameObject.SetActive(true); // Включение знака вопроса
    }

    public void StartEasyMode() // Запуск легкого режима (3 котла, 10 защит)
    {
        StartGameWithDifficulty(DefenseDifficulty.Easy, 3, 10, 2.5f);
    }

    public void StartMediumMode() // Запуск среднего режима (4 котла, 15 защит)
    {
        StartGameWithDifficulty(DefenseDifficulty.Medium, 4, 15, 1.9f);
    }

    public void StartHardMode() // Запуск сложного режима (5 котлов, 20 защит)
    {
        StartGameWithDifficulty(DefenseDifficulty.Hard, 5, 20, 1.4f);
    }

    private void StartGameWithDifficulty(DefenseDifficulty difficulty, int activeCauldrons, int targetCount, float spawnInterval) // Старт раунда
    {
        EnsureUIHierarchy(); // Проверка ссылок
        currentDifficulty = difficulty; // Присвоение сложности
        targetDefenses = targetCount; // Количество отражений
        currentDefenses = 0; // Сброс счетчика
        currentLives = maxLives; // Восстановление жизней
        cloudInterval = spawnInterval; // Интервал туч
        isPlaying = true; // Активация флага игры
        isPausedByGuide = false; // Снятие паузы

        // 1. Скрытие окна сложности и открытие поля геймплея
        if (difficultyPanel != null) difficultyPanel.SetActive(false); // Скрытие меню сложности
        if (activeStagePanel != null)
        {
            activeStagePanel.SetActive(true); // Включение поля с котлами
            activeStagePanel.transform.SetAsLastSibling(); // Вынос на передний план
        }
        if (resultSummaryPanel != null) resultSummaryPanel.SetActive(false); // Скрытие наград

        // 2. Включение геймплейных счетчиков в верхней панели
        if (heartsContainer != null) heartsContainer.SetActive(true); // Отображение жизней
        if (defenseCounterText != null) defenseCounterText.gameObject.SetActive(true); // Отображение счетчика
        if (levelDifficultyText != null)
        {
            levelDifficultyText.gameObject.SetActive(true); // Отображение текста
            levelDifficultyText.text = difficulty == DefenseDifficulty.Easy ? "Легкий" : (difficulty == DefenseDifficulty.Medium ? "Средний" : "Сложный");
        }

        // 3. Активация котлов в соответствии с уровнем сложности
        if (cauldronRoots != null)
        {
            for (int i = 0; i < cauldronRoots.Length; i++)
            {
                if (cauldronRoots[i] != null)
                {
                    cauldronRoots[i].SetActive(i < activeCauldrons); // Включение нужного числа котлов
                }
                if (umbrellaVisuals != null && umbrellaVisuals.Length > i && umbrellaVisuals[i] != null) umbrellaVisuals[i].SetActive(false);
                if (cloudVisuals != null && cloudVisuals.Length > i && cloudVisuals[i] != null) cloudVisuals[i].SetActive(false);
            }
        }

        UpdateLivesUI(); // Обновление сердечек
        UpdateCounterUI(); // Обновление текста защит

        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine); // Остановка старого цикла
        gameLoopCoroutine = StartCoroutine(DefenseGameLoop(activeCauldrons)); // Запуск игрового цикла
    }

    private IEnumerator DefenseGameLoop(int activeCauldrons) // Игровой цикл появления туч
    {
        while (isPlaying && currentLives > 0 && currentDefenses < targetDefenses)
        {
            if (isPausedByGuide)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(cloudInterval); // Ожидание интервала спавна

            if (!isPlaying || isPausedByGuide) continue; // Проверка состояния

            activeCloudIndex = Random.Range(0, activeCauldrons); // Случайный выбор атакуемого котла
            if (cloudVisuals != null && cloudVisuals.Length > activeCloudIndex && cloudVisuals[activeCloudIndex] != null)
            {
                cloudVisuals[activeCloudIndex].SetActive(true); // Появление тучки
            }
            if (rainParticles != null && rainParticles.Length > activeCloudIndex && rainParticles[activeCloudIndex] != null)
            {
                rainParticles[activeCloudIndex].Play(); // Начало дождя
            }

            float reactionTime = currentDifficulty == DefenseDifficulty.Easy ? 1.8f : (currentDifficulty == DefenseDifficulty.Medium ? 1.4f : 1.0f); // Время реакции
            float elapsed = 0f;
            bool protectedInTime = false;

            while (elapsed < reactionTime) // Окно времени для нажатия зонтика
            {
                if (!isPausedByGuide) elapsed += Time.deltaTime;
                if (umbrellaVisuals != null && umbrellaVisuals.Length > activeCloudIndex && umbrellaVisuals[activeCloudIndex] != null && umbrellaVisuals[activeCloudIndex].activeSelf)
                {
                    protectedInTime = true; // Успешная защита
                    break;
                }
                yield return null;
            }

            if (protectedInTime) // Если игрок успел спасти котел
            {
                currentDefenses++; // Увеличение числа отраженных туч
                UpdateCounterUI(); // Обновление счетчика
            }
            else // Если дождь залил котел
            {
                currentLives--; // Потеря жизни
                UpdateLivesUI(); // Обновление сердечек
            }

            yield return new WaitForSeconds(0.4f); // Пауза после атаки
            if (cloudVisuals != null && cloudVisuals.Length > activeCloudIndex && cloudVisuals[activeCloudIndex] != null)
                cloudVisuals[activeCloudIndex].SetActive(false); // Скрытие тучи
            if (umbrellaVisuals != null && umbrellaVisuals.Length > activeCloudIndex && umbrellaVisuals[activeCloudIndex] != null)
                umbrellaVisuals[activeCloudIndex].SetActive(false); // Скрытие зонтика
            if (rainParticles != null && rainParticles.Length > activeCloudIndex && rainParticles[activeCloudIndex] != null)
                rainParticles[activeCloudIndex].Stop(); // Остановка дождя

            activeCloudIndex = -1; // Сброс индекса
        }

        EndGame(); // Завершение игры
    }

    private void OnCauldronShieldClicked(int index) // Клик по зонтику котла
    {
        if (!isPlaying || isPausedByGuide) return; // Игнорирование при паузе
        if (umbrellaVisuals != null && umbrellaVisuals.Length > index && umbrellaVisuals[index] != null)
        {
            umbrellaVisuals[index].SetActive(true); // Раскрытие зонтика
            StartCoroutine(AutoHideUmbrella(index, 0.9f)); // Авто-скрытие зонтика через 0.9 сек
        }
    }

    private IEnumerator AutoHideUmbrella(int index, float delay) // Корутина авто-скрытия зонтика
    {
        yield return new WaitForSeconds(delay); // Ожидание
        if (umbrellaVisuals != null && umbrellaVisuals.Length > index && umbrellaVisuals[index] != null)
        {
            umbrellaVisuals[index].SetActive(false); // Скрытие зонтика
        }
    }

    private void UpdateLivesUI() // Обновление 3 сердечек
    {
        if (heartIcons == null) return;
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
            {
                heartIcons[i].color = (i < currentLives) ? Color.white : new Color(0.25f, 0.25f, 0.25f, 0.5f); // Затемнение при потере жизни
            }
        }
    }

    private void UpdateCounterUI() // Обновление счетчика защит
    {
        if (defenseCounterText != null)
        {
            defenseCounterText.text = $"Защищено: {currentDefenses} / {targetDefenses}"; // Отображение прогресса
        }
    }

    private void EndGame() // Завершение игры
    {
        isPlaying = false; // Остановка
        if (activeStagePanel != null) activeStagePanel.SetActive(false); // Скрытие поля
        if (resultSummaryPanel != null)
        {
            resultSummaryPanel.SetActive(true); // Показ окна результатов
            resultSummaryPanel.transform.SetAsLastSibling(); // Вынос вперед
        }

        bool won = currentLives > 0; // Флаг победы
        string playerName = PlayerPrefs.GetString("Player_Name", PlayerPrefs.GetString("Alchemist_Player_Name", "Алхимик")); // Чтение имени

        if (resultTitleText != null)
        {
            resultTitleText.text = won ? $"Блестяще, {playerName}! Котлы спасены!" : $"Огонь погас! Попробуй снова, {playerName}!"; // Заголовок
        }

        int gold = won ? (currentDifficulty == DefenseDifficulty.Easy ? 5000 : (currentDifficulty == DefenseDifficulty.Medium ? 8000 : 15000)) : 500; // Награда золотом
        int playerXp = won ? (currentDifficulty == DefenseDifficulty.Easy ? 100 : (currentDifficulty == DefenseDifficulty.Medium ? 500 : 2000)) : 50; // Опыт игрока
        int masteryXp = won ? (currentDifficulty == DefenseDifficulty.Easy ? 100 : (currentDifficulty == DefenseDifficulty.Medium ? 300 : 1000)) : 25; // Опыт мастерства

        if (rewardGoldText != null) rewardGoldText.text = $"+{gold:N0} Золота";
        if (rewardPlayerXpText != null) rewardPlayerXpText.text = $"+{playerXp} XP (Опыт Игрока)";
        if (rewardMasteryXpText != null) rewardMasteryXpText.text = $"+{masteryXp} XP (Опыт Мастерства)";
    }

    public void OpenGuide() // Открытие справочника
    {
        isPausedByGuide = true; // Установка паузы
        if (guidePopupPanel != null)
        {
            guidePopupPanel.SetActive(true); // Показ подсказки
            guidePopupPanel.transform.SetAsLastSibling(); // Вынос вперед
        }
        if (closeGameButton != null) closeGameButton.interactable = false; // Блокировка выхода
    }

    public void CloseGuide() // Закрытие справочника
    {
        isPausedByGuide = false; // Снятие паузы
        if (guidePopupPanel != null) guidePopupPanel.SetActive(false); // Скрытие подсказки
        if (closeGameButton != null) closeGameButton.interactable = true; // Разблокировка выхода
    }

    public void ClaimAllRewards() // Сбор наград и переход к диалогу о Лаборатории
    {
        bool won = currentLives > 0; // Флаг победы
        int gold = won ? (currentDifficulty == DefenseDifficulty.Easy ? 5000 : (currentDifficulty == DefenseDifficulty.Medium ? 8000 : 15000)) : 500; // Золото
        int playerXp = won ? (currentDifficulty == DefenseDifficulty.Easy ? 100 : (currentDifficulty == DefenseDifficulty.Medium ? 500 : 2000)) : 50; // Опыт игрока
        int masteryXp = won ? (currentDifficulty == DefenseDifficulty.Easy ? 100 : (currentDifficulty == DefenseDifficulty.Medium ? 300 : 1000)) : 25; // Опыт мастерства

        if (Avatar_Manager.Instance != null) // Начисление в профиль
        {
            Avatar_Manager.Instance.GainPlayerExperience(playerXp); // Начисление опыта
        }

        // Сохранение золота в PlayerPrefs
        int currentGold = PlayerPrefs.GetInt("Player_Gold", 0) + gold;
        PlayerPrefs.SetInt("Player_Gold", currentGold);
        PlayerPrefs.SetInt("Minigame_CauldronDefense_Completed", 1); // Отметка прохождения
        PlayerPrefs.Save(); // Сохранение

        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources(); // Синхронизация интерфейса
        }

        // Отправка зелий в инвентарь
        if (Inventory_Manager.Instance != null && won)
        {
            int savedCauldronsCount = currentDifficulty == DefenseDifficulty.Easy ? 3 : (currentDifficulty == DefenseDifficulty.Medium ? 4 : 5);
            Inventory_Manager.Instance.AddItem("defense_potion_shield", "Зелье Защиты Котлов", 1, masteryXp, null, new Color(0.35f, 0.75f, 1f)); // Зелье в сундук
            Inventory_Manager.Instance.AddItem("cauldron_essence_charge", "Очищенная Эссенция Котла", savedCauldronsCount, 25, null, new Color(0.9f, 0.4f, 0.8f)); // Эссенция в сундук
        }

        if (resultSummaryPanel != null) resultSummaryPanel.SetActive(false); // Скрытие итогов
        if (gameRootPanel != null) gameRootPanel.SetActive(false); // Скрытие игры
        else gameObject.SetActive(false); // Запасное скрытие

        string playerName = PlayerPrefs.GetString("Player_Name", PlayerPrefs.GetString("Alchemist_Player_Name", "Алхимик")); // Чтение имени

        // Запуск диалога Кота об Алхимической Лаборатории
        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление HUD
            DialogueSystem_Manager.Instance.StartPostCauldronDefenseDialogue(); // Запуск диалога Кота
            Debug.Log($"Защита котлов завершена! Запущен диалог Кота о Лаборатории для игрока {playerName}.");
            return;
        }

        // Фоллбэк: открытие Лаборатории напрямую
        if (laboratoryGamePanel != null)
        {
            laboratoryGamePanel.SetActive(true);
        }
        else if (Laboratory_Mixer.Instance != null)
        {
            Laboratory_Mixer.Instance.gameObject.SetActive(true);
        }
    }

    public void CloseGame() // Закрытие мини-игры
    {
        isPlaying = false; // Остановка игры
        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine); // Остановка корутины
        if (gameRootPanel != null) gameRootPanel.SetActive(false); // Скрытие корневой панели
        else gameObject.SetActive(false); // Скрытие объекта

        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame(); // Восстановление HUD
        }
    }

    public void CloseMinigame() // Унифицированный метод закрытия мини-игры
    {
        CloseGame(); // Вызов основного метода закрытия
    }
}
