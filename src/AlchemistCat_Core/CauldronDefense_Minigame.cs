using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Мини-игра «Защита Алхимических Котлов» (Cauldron Rain Defense).
/// Игрок спасает горящие котлы от дождевых туч, вовремя раскрывая магические силовые зонтики.
/// </summary>
public class CauldronDefense_Minigame : MonoBehaviour
{
    public static CauldronDefense_Minigame Instance { get; private set; } // Статический синглтон мини-игры защиты котлов

    [Header("=== Основные панели игры ===")]
    [SerializeField] private GameObject gameRootPanel; // Корневой объект всей мини-игры
    [SerializeField] private GameObject difficultyPanel; // Окно выбора уровня сложности
    [SerializeField] private GameObject activeStagePanel; // Игровое поле с котлами и тучами
    [SerializeField] private GameObject resultSummaryPanel; // Итоговое окно победы / поражения
    [SerializeField] private Button closeGameButton; // Кнопка закрытия мини-игры

    [Header("=== Справочник / Подсказка игры ===")]
    [SerializeField] private Button guideButton; // Кнопка вызова значка подсказки (?)
    [SerializeField] private GameObject guidePopupPanel; // Всплывающее окно со справочником игры
    [SerializeField] private Button guideCloseButton; // Крестик закрытия окна подсказки

    [Header("=== Жизни и Счетчики ===")]
    [SerializeField] private Image[] heartIcons; // 3 сердечка жизней игрока
    [SerializeField] private TextMeshProUGUI defenseCounterText; // Счетчик успешных защит (например, Защищено: 4 / 10)
    [SerializeField] private TextMeshProUGUI levelDifficultyText; // Текст текущей сложности

    [Header("=== Котлы и Защитные Зонтики ===")]
    [SerializeField] private GameObject[] cauldronRoots; // 5 слотов для котлов (активируются 3, 4 или 5 в зависимости от сложности)
    [SerializeField] private Button[] cauldronButtons; // Кнопки под котлами для активации зонтиков
    [SerializeField] private GameObject[] umbrellaVisuals; // Анимированные магические зонтики над каждым котлом
    [SerializeField] private GameObject[] cloudVisuals; // Грозовые тучки над каждым котлом
    [SerializeField] private ParticleSystem[] rainParticles; // Частицы дождя под каждой тучкой

    [Header("=== Итоговое окно наград ===")]
    [SerializeField] private TextMeshProUGUI resultTitleText; // Заголовок (Победа! / Попробуй снова!)
    [SerializeField] private TextMeshProUGUI rewardGoldText; // Награда золотом
    [SerializeField] private TextMeshProUGUI rewardPlayerXpText; // Награда Опыт Игрока
    [SerializeField] private TextMeshProUGUI rewardMasteryXpText; // Награда Опыт Мастерства
    [SerializeField] private Button claimRewardsButton; // Кнопка «Забрать все в рюкзак»

    [Header("=== Интеграция с диалогами и переход в Лабораторию ===")]
    [SerializeField] private GameObject laboratoryGamePanel; // Ссылка на панель «Laboratory_Minigame»
    [SerializeField] private GameObject dialogueContainer; // Контейнер окна диалога с Котом
    [SerializeField] private TextMeshProUGUI dialogueText; // Текст диалога Кота

    public enum DefenseDifficulty { Easy, Medium, Hard }
    private DefenseDifficulty currentDifficulty = DefenseDifficulty.Easy;

    private int maxLives = 3; // Количество жизней
    private int currentLives = 3; // Текущие жизни
    private int targetDefenses = 10; // Целевое число отраженных туч (10, 15, 20)
    private int currentDefenses = 0; // Текущее число защит
    private bool isPlaying = false; // Флаг активного раунда
    private bool isPausedByGuide = false; // Флаг паузы при открытом справочнике

    private float cloudInterval = 2.2f; // Интервал между тучами
    private int activeCloudIndex = -1; // Индекс котла с активной тучей

    private void Awake()
    {
        if (Instance == null) Instance = this; // Инициализация синглтона
        else Destroy(gameObject); // Уничтожение дубликата

        if (guideButton) guideButton.onClick.AddListener(OpenGuide); // Привязка кнопки подсказки
        if (guideCloseButton) guideCloseButton.onClick.AddListener(CloseGuide); // Привязка крестика
        if (closeGameButton) closeGameButton.onClick.AddListener(CloseGame); // Привязка выхода
        if (claimRewardsButton) claimRewardsButton.onClick.AddListener(ClaimAllRewards); // Привязка сбора

        if (cauldronButtons != null)
        {
            for (int i = 0; i < cauldronButtons.Length; i++)
            {
                int index = i;
                if (cauldronButtons[i] != null)
                {
                    cauldronButtons[i].onClick.AddListener(() => OnCauldronShieldClicked(index)); // Клик по зонтику
                }
            }
        }
    }

    /// <summary>
    /// Открытие окна мини-игры «Защита Котлов» с включением меню сложности
    /// </summary>
    public void OpenMinigame()
    {
        gameObject.SetActive(true); // Активация игрового объекта
        Transform p = transform.parent;
        while (p != null)
        {
            p.gameObject.SetActive(true); // Включение всех родительских панелей
            p = p.parent;
        }

        if (gameRootPanel != null) gameRootPanel.SetActive(true); // Активация корневой панели
        ShowDifficultySelection(); // Показ выбора сложности
    }

    /// <summary>
    /// Показ экрана выбора сложности
    /// </summary>
    public void ShowDifficultySelection()
    {
        isPlaying = false; // Остановка игры
        if (difficultyPanel) difficultyPanel.SetActive(true); // Включение меню сложности
        if (activeStagePanel) activeStagePanel.SetActive(false); // Выключение игрового поля
        if (resultSummaryPanel) resultSummaryPanel.SetActive(false); // Выключение окна наград
        if (guidePopupPanel) guidePopupPanel.SetActive(false); // Выключение справочника
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

    private void StartGameWithDifficulty(DefenseDifficulty difficulty, int activeCauldrons, int targetCount, float spawnInterval)
    {
        currentDifficulty = difficulty;
        targetDefenses = targetCount;
        currentDefenses = 0;
        currentLives = maxLives;
        cloudInterval = spawnInterval;
        isPlaying = true;
        isPausedByGuide = false;

        if (difficultyPanel) difficultyPanel.SetActive(false); // Скрытие выбора сложности
        if (activeStagePanel) activeStagePanel.SetActive(true); // Открытие игрового поля
        if (resultSummaryPanel) resultSummaryPanel.SetActive(false); // Скрытие итогов

        if (cauldronRoots != null)
        {
            for (int i = 0; i < cauldronRoots.Length; i++)
            {
                if (cauldronRoots[i] != null)
                {
                    cauldronRoots[i].SetActive(i < activeCauldrons);
                }
                if (umbrellaVisuals != null && umbrellaVisuals.Length > i && umbrellaVisuals[i] != null) umbrellaVisuals[i].SetActive(false);
                if (cloudVisuals != null && cloudVisuals.Length > i && cloudVisuals[i] != null) cloudVisuals[i].SetActive(false);
            }
        }

        UpdateLivesUI(); // Обновление сердечек
        UpdateCounterUI(); // Обновление счетчика
        StartCoroutine(DefenseGameLoop(activeCauldrons)); // Запуск игрового цикла
    }

    private IEnumerator DefenseGameLoop(int activeCauldrons)
    {
        while (isPlaying && currentLives > 0 && currentDefenses < targetDefenses)
        {
            if (isPausedByGuide)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(cloudInterval);

            if (!isPlaying || isPausedByGuide) continue;

            activeCloudIndex = Random.Range(0, activeCauldrons);
            if (cloudVisuals != null && cloudVisuals.Length > activeCloudIndex && cloudVisuals[activeCloudIndex] != null)
            {
                cloudVisuals[activeCloudIndex].SetActive(true); // Появление тучки
            }
            if (rainParticles != null && rainParticles.Length > activeCloudIndex && rainParticles[activeCloudIndex] != null)
            {
                rainParticles[activeCloudIndex].Play(); // Начало дождя
            }

            float reactionTime = currentDifficulty == DefenseDifficulty.Easy ? 1.8f : (currentDifficulty == DefenseDifficulty.Medium ? 1.4f : 1.0f);
            float elapsed = 0f;
            bool protectedInTime = false;

            while (elapsed < reactionTime)
            {
                if (!isPausedByGuide) elapsed += Time.deltaTime;
                if (umbrellaVisuals != null && umbrellaVisuals.Length > activeCloudIndex && umbrellaVisuals[activeCloudIndex] != null && umbrellaVisuals[activeCloudIndex].activeSelf)
                {
                    protectedInTime = true;
                    break;
                }
                yield return null;
            }

            if (protectedInTime)
            {
                currentDefenses++;
                UpdateCounterUI();
            }
            else
            {
                currentLives--;
                UpdateLivesUI();
            }

            yield return new WaitForSeconds(0.4f);
            if (cloudVisuals != null && cloudVisuals.Length > activeCloudIndex && cloudVisuals[activeCloudIndex] != null)
                cloudVisuals[activeCloudIndex].SetActive(false);
            if (umbrellaVisuals != null && umbrellaVisuals.Length > activeCloudIndex && umbrellaVisuals[activeCloudIndex] != null)
                umbrellaVisuals[activeCloudIndex].SetActive(false);
            if (rainParticles != null && rainParticles.Length > activeCloudIndex && rainParticles[activeCloudIndex] != null)
                rainParticles[activeCloudIndex].Stop();

            activeCloudIndex = -1;
        }

        EndGame();
    }

    private void OnCauldronShieldClicked(int index) // Клик по защите котла
    {
        if (!isPlaying || isPausedByGuide) return;
        if (umbrellaVisuals != null && umbrellaVisuals.Length > index && umbrellaVisuals[index] != null)
        {
            umbrellaVisuals[index].SetActive(true);
            StartCoroutine(AutoHideUmbrella(index, 0.9f));
        }
    }

    private IEnumerator AutoHideUmbrella(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (umbrellaVisuals != null && umbrellaVisuals.Length > index && umbrellaVisuals[index] != null)
        {
            umbrellaVisuals[index].SetActive(false);
        }
    }

    private void UpdateLivesUI() // Обновление 3 сердечек
    {
        if (heartIcons == null) return;
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
            {
                heartIcons[i].color = (i < currentLives) ? Color.white : new Color(0.25f, 0.25f, 0.25f, 0.5f);
            }
        }
    }

    private void UpdateCounterUI() // Обновление счетчика побед
    {
        if (defenseCounterText)
        {
            defenseCounterText.text = $"Защищено: {currentDefenses} / {targetDefenses}";
        }
    }

    private void EndGame() // Завершение игры
    {
        isPlaying = false;
        if (activeStagePanel) activeStagePanel.SetActive(false);
        if (resultSummaryPanel) resultSummaryPanel.SetActive(true);

        bool won = currentLives > 0;
        string playerName = PlayerPrefs.GetString("PlayerName", "Алхимик"); // Запоминаем имя игрока

        if (resultTitleText)
        {
            resultTitleText.text = won ? $"Блестяще, {playerName}! Котлы спасены!" : $"Огонь погас! Попробуй снова, {playerName}!";
        }

        int gold = won ? (currentDifficulty == DefenseDifficulty.Easy ? 5000 : (currentDifficulty == DefenseDifficulty.Medium ? 8000 : 15000)) : 500;
        int playerXp = won ? (currentDifficulty == DefenseDifficulty.Easy ? 100 : (currentDifficulty == DefenseDifficulty.Medium ? 500 : 2000)) : 50;
        int masteryXp = won ? (currentDifficulty == DefenseDifficulty.Easy ? 100 : (currentDifficulty == DefenseDifficulty.Medium ? 300 : 1000)) : 25;

        if (rewardGoldText) rewardGoldText.text = $"+{gold:N0} Золота";
        if (rewardPlayerXpText) rewardPlayerXpText.text = $"+1 Зелье Опыта Игрока (+{playerXp} XP)";
        if (rewardMasteryXpText) rewardMasteryXpText.text = $"+1 Зелье Мастерства (+{masteryXp} XP)";
    }

    public void OpenGuide() // Открытие справочника
    {
        isPausedByGuide = true;
        if (guidePopupPanel) guidePopupPanel.SetActive(true);
        if (closeGameButton) closeGameButton.interactable = false;
    }

    public void CloseGuide() // Закрытие справочника
    {
        isPausedByGuide = false;
        if (guidePopupPanel) guidePopupPanel.SetActive(false);
        if (closeGameButton) closeGameButton.interactable = true;
    }

    public void ClaimAllRewards() // Сбор наград и запуск Лаборатории с диалогом Кота
    {
        bool won = currentLives > 0; // Флаг победы
        int gold = won ? (currentDifficulty == DefenseDifficulty.Easy ? 5000 : (currentDifficulty == DefenseDifficulty.Medium ? 8000 : 15000)) : 500; // Расчет золота
        int playerXp = won ? (currentDifficulty == DefenseDifficulty.Easy ? 100 : (currentDifficulty == DefenseDifficulty.Medium ? 500 : 2000)) : 50; // Расчет опыта игрока
        int masteryXp = won ? (currentDifficulty == DefenseDifficulty.Easy ? 100 : (currentDifficulty == DefenseDifficulty.Medium ? 300 : 1000)) : 25; // Расчет опыта мастерства

        if (Avatar_Manager.Instance != null) // Если менеджер аватара активен
        {
            Avatar_Manager.Instance.AddGold(gold); // Начисление золота
            Avatar_Manager.Instance.AddExperience(playerXp); // Начисление опыта
        }

        if (resultSummaryPanel) resultSummaryPanel.SetActive(false); // Скрытие окна итогов
        if (gameRootPanel) gameRootPanel.SetActive(false); // Скрытие игры защиты котлов
        else gameObject.SetActive(false); // Запасное скрытие

        string playerName = PlayerPrefs.GetString("PlayerName", PlayerPrefs.GetString("Player_Name", "Алхимик")); // Чтение имени игрока

        // Запуск централизованного диалога Кота и переход в «Алхимическую Лабораторию»
        if (DialogueSystem_Manager.Instance != null) // Если центральный менеджер диалогов доступен
        {
            DialogueSystem_Manager.Instance.StartPostCauldronDefenseDialogue(); // Запуск диалога Кота и презентации Лаборатории
            Debug.Log($"Защита котлов завершена! Запущен централизованный диалог Кота о Лаборатории для игрока {playerName}."); // Лог
            return; // Выход
        }

        // Фоллбэк: если диалог настроен локально
        if (dialogueContainer != null && dialogueText != null) // Если диалог настроен
        {
            dialogueContainer.SetActive(true); // Включение окна диалога
            dialogueText.text = $"Мурр-мяу, {playerName}! Все котлы спасены от грозы, а награды уже в твоем рюкзаке!\n\nА теперь добро пожаловать в нашу святыню — «Алхимическую Лабораторию»!\nЗдесь 5 рабочих колб и 10 дозаторов магических эссенций. Смешивай пропорции, открывай 40 тайных рецептов и выгодно продавай готовые зелья на Мировом Рынке!"; // Реплика Кота
        }

        // Открытие Лаборатории напрямую
        if (laboratoryGamePanel != null) // Если ссылка на лабораторию подключена
        {
            laboratoryGamePanel.SetActive(true); // Включение панели Лаборатории
            Debug.Log($"Защита котлов завершена! Открыта панель Лаборатории для игрока {playerName}."); // Лог
        }
        else if (Laboratory_Mixer.Instance != null) // Запасной запуск через синглтон
        {
            Laboratory_Mixer.Instance.gameObject.SetActive(true); // Включение
        }
    }

    public void CloseGame() // Закрытие окна
    {
        isPlaying = false;
        if (gameRootPanel) gameRootPanel.SetActive(false);
    }
}
