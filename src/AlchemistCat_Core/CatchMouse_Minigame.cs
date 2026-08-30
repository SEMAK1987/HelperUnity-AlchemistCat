using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Мини-игра «Поймай мышку» (Catch The Mouse) для Колеса Мини-Игр Кота-Алхимика.
/// Реализует 3-фазную сюжетно-прогрессивную механику ловли мышей с выбором 3 уровней сложности:
/// - Меню сложности перед стартом: Легкий (x1.0), Средний (x1.5), Сложный (x2.5) с множителем всех наград.
/// - Фаза 1: Поймать 5 золотых мышей. Клик на черную или серебристую мышь сбрасывает счетчик/перезапускает фазу.
/// - Фаза 2: Поймать 10 серебряных мышей. Черных и золотых не трогать.
/// - Фаза 3: Поймать 20 черных (самых быстрых) мышей!
/// - Поддержка покадровой спрайтовой анимации (AutoSprite.io / Sorceress).
/// </summary>
public class CatchMouse_Minigame : MonoBehaviour
{
    public static CatchMouse_Minigame Instance;

    public enum DifficultyLevel { Easy, Normal, Hard }

    [Header("Главная панель мини-игры")]
    public GameObject gamePanel;                // CatchMouse_Game_Panel
    public Button closeButton;                  // Close_Button

    [Header("Меню выбора сложности (Перед игрой)")]
    public GameObject difficultySelectionPanel; // Difficulty_Selection_Panel
    public Button easyDifficultyButton;         // Easy_Button
    public Button normalDifficultyButton;       // Normal_Button
    public Button hardDifficultyButton;         // Hard_Button

    [Header("Верхняя плашка цели (Задание)")]
    public Image targetMouseDisplayImage;       // Target_Mouse_Image
    public TextMeshProUGUI targetTitleText;     // Target_Title_Text
    public TextMeshProUGUI instructionBannerText;// Инструкция/предупреждение
    public TextMeshProUGUI progressCounterText; // Progress_Counter_Text
    public TextMeshProUGUI timerText;           // Timer_Text
    public float roundTime = 15f;               // Время на фазу (Round Time в Inspector)

    [Header("Центральное уведомление перехода фаз")]
    public GameObject centralPhaseNoticePanel;  // Central_Phase_Notice_Panel
    public TextMeshProUGUI centralNoticeTitle;  // Notice_Title
    public TextMeshProUGUI centralNoticeBody;   // Notice_Body

    [Header("Одиночные Спрайты Мышек (Статика/Фоллбэк)")]
    public Sprite goldenMouseSprite;            // Золотая мышь (Золотая Мышка)
    public Sprite silverMouseSprite;            // Серебряная мышь (Серебряная Мышка)
    public Sprite blackMouseSprite;             // Черная мышь (Черная Теневая Мышка)

    [Header("Кадры бега Мышек (Spritesheet Frames)")]
    public Sprite[] goldenMouseFrames;          // Golden Mouse Frames (18)
    public Sprite[] silverMouseFrames;          // Silver Mouse Frames (17)
    public Sprite[] shadowMouseFrames;          // Shadow Mouse Frames (14)
    public float animationFps = 12f;            // Animation Fps (12)

    [Header("Спрайт Норки и Дорожки")]
    public Sprite holeSprite;                   // Норка Арка выхода мышей
    public Sprite roadSprite;                   // Дорожка перед норками

    [Header("5 Норок и Дорожка")]
    public RectTransform[] holes;               // Holes (5 элементов: Hole_1 .. Hole_5)
    public RectTransform roadTrack;             // Road_Track
    public RectTransform miceRunningLayer;      // Mice_Running_Layer

    [Header("Окно Победы и Награды")]
    public GameObject rewardPopupPanel;         // Reward_Popup_Panel
    public Image potionRewardIcon;              // Potion_Icon
    public TextMeshProUGUI rewardDescriptionText; // Reward_Description_Text
    public Button claimRewardButton;            // Claim_Reward_Button

    [Header("Звуки")]
    public AudioClip winFanfareSound;           // Win
    public AudioClip phaseCompleteSound;        // Phase complete
    public AudioClip wrongMouseSound;           // Wrong Mouse Sound
    public AudioClip mouseRunSound;             // Mouse Run Sound
    public AudioClip catchMouseSound;           // Catch Mouse Sound
    public AudioClip clickSound;                // Click Sound

    // Внутреннее состояние игры
    public enum MouseType { Golden, Silver, Black }

    private DifficultyLevel selectedDifficulty = DifficultyLevel.Normal;
    private int currentPhase = 1;               // 1 = Золотые (5), 2 = Серебряные (10), 3 = Черные (20)
    private int miceCaughtInPhase = 0;
    private int targetMiceForCurrentPhase = 5;
    private MouseType currentTargetType = MouseType.Golden;

    private float currentTimer;
    private bool isGameActive = false;
    private bool isGameWon = false;
    private bool isPhaseTransitioning = false;

    // Пул активных бегущих мышек
    private List<GameObject> activeMice = new List<GameObject>();
    private Coroutine spawnCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMinigame);

        if (claimRewardButton != null)
            claimRewardButton.onClick.AddListener(ClaimRewardAndExit);

        if (easyDifficultyButton != null)
            easyDifficultyButton.onClick.AddListener(() => SetDifficultyAndStart(DifficultyLevel.Easy));

        if (normalDifficultyButton != null)
            normalDifficultyButton.onClick.AddListener(() => SetDifficultyAndStart(DifficultyLevel.Normal));

        if (hardDifficultyButton != null)
            hardDifficultyButton.onClick.AddListener(() => SetDifficultyAndStart(DifficultyLevel.Hard));

        if (rewardPopupPanel != null)
            rewardPopupPanel.SetActive(false);

        if (centralPhaseNoticePanel != null)
            centralPhaseNoticePanel.SetActive(false);
    }

    private void OnEnable()
    {
        ShowDifficultySelection();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ClearAllMice();
    }

    /// <summary>
    /// Показ экрана выбора сложности перед стартом забега
    /// </summary>
    public void ShowDifficultySelection()
    {
        isGameActive = false;
        isGameWon = false;
        isPhaseTransitioning = false;
        ClearAllMice();

        if (rewardPopupPanel != null)
            rewardPopupPanel.SetActive(false);

        if (centralPhaseNoticePanel != null)
            centralPhaseNoticePanel.SetActive(false);

        if (difficultySelectionPanel != null)
            difficultySelectionPanel.SetActive(true);
    }

    /// <summary>
    /// Выбор сложности и запуск 1-й фазы (5 Золотых мышей)
    /// </summary>
    public void SetDifficultyAndStart(DifficultyLevel diff)
    {
        selectedDifficulty = diff;

        if (clickSound != null && SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(clickSound);

        if (difficultySelectionPanel != null)
            difficultySelectionPanel.SetActive(false);

        // Старт с Фазы 1
        currentPhase = 1;
        StartPhase(1);
    }

    /// <summary>
    /// Старт конкретной фазы мини-игры (1 = Золото 5 шт, 2 = Серебро 10 шт, 3 = Черные 20 шт)
    /// </summary>
    private void StartPhase(int phase)
    {
        currentPhase = phase;
        miceCaughtInPhase = 0;
        isPhaseTransitioning = false;
        isGameWon = false;
        isGameActive = true;

        switch (currentPhase)
        {
            case 1:
                currentTargetType = MouseType.Golden;
                targetMiceForCurrentPhase = 5;
                currentTimer = roundTime > 0 ? roundTime : 45f;
                break;
            case 2:
                currentTargetType = MouseType.Silver;
                targetMiceForCurrentPhase = 10;
                currentTimer = (roundTime > 0 ? roundTime : 45f) + 15f; // Больше времени на 10 мышей
                break;
            case 3:
                currentTargetType = MouseType.Black;
                targetMiceForCurrentPhase = 20;
                currentTimer = (roundTime > 0 ? roundTime : 45f) + 35f; // Время на 20 черных быстрых мышей
                break;
        }

        UpdateTaskUI();
        ClearAllMice();

        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(MiceSpawnLoop());
    }

    /// <summary>
    /// Обновление текстов рамок и счетчиков в UI
    /// </summary>
    private void UpdateTaskUI()
    {
        if (targetMouseDisplayImage != null)
        {
            targetMouseDisplayImage.sprite = GetMouseSprite(currentTargetType);
            targetMouseDisplayImage.preserveAspect = true;
        }

        if (targetTitleText != null)
        {
            string colorName = currentTargetType == MouseType.Golden ? "<color=#FFD166>ЗОЛОТЫХ</color>" :
                              (currentTargetType == MouseType.Silver ? "<color=#E0E1DD>СЕРЕБРЯНЫХ</color>" : "<color=#A06CD5>ТЕНЕВЫХ ЧЕРНЫХ</color>");
            targetTitleText.text = $"ЭТАП {currentPhase}/3: Поймать {targetMiceForCurrentPhase} {colorName} мышей!";
        }

        if (instructionBannerText != null)
        {
            if (currentPhase == 1)
            {
                instructionBannerText.text = "<color=#EF476F>⚠ ВНИМАНИЕ:</color> Черных и серебристых не трогать, иначе игра начнется заново!";
            }
            else if (currentPhase == 2)
            {
                instructionBannerText.text = "<color=#EF476F>⚠ ВНИМАНИЕ:</color> Черных и золотых не трогать, иначе этап сбросится!";
            }
            else
            {
                instructionBannerText.text = "<color=#FFD166>⚡ ФИНАЛ:</color> Ловите 20 самых быстрых теневых мышей! Золото и серебро не трогать!";
            }
        }

        if (progressCounterText != null)
        {
            progressCounterText.text = $"Поймано: <b>{miceCaughtInPhase} / {targetMiceForCurrentPhase}</b>";
        }
    }

    private void Update()
    {
        if (!isGameActive || isGameWon || isPhaseTransitioning) return;

        currentTimer -= Time.deltaTime;
        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            // Время истекло — сброс текущей фазы
            RestartCurrentPhase("Время вышло! Попробуйте еще раз!");
        }

        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(currentTimer);
            timerText.text = $"Время: 00:{seconds:D2}";
        }
    }

    /// <summary>
    /// Цикл выбегания мышек из норок
    /// </summary>
    private IEnumerator MiceSpawnLoop()
    {
        while (isGameActive && !isGameWon && !isPhaseTransitioning)
        {
            float spawnDelay = GetSpawnInterval();
            yield return new WaitForSeconds(spawnDelay);

            if (holes == null || holes.Length < 2) continue;

            int startHoleIndex = Random.Range(0, holes.Length);
            int endHoleIndex = Random.Range(0, holes.Length);
            while (endHoleIndex == startHoleIndex)
            {
                endHoleIndex = Random.Range(0, holes.Length);
            }

            // Вероятность появления мышки целевого типа ~45%
            MouseType spawnType;
            if (Random.value < 0.45f)
            {
                spawnType = currentTargetType;
            }
            else
            {
                int r = Random.Range(0, 3);
                spawnType = (MouseType)r;
            }

            SpawnMouseRunner(spawnType, startHoleIndex, endHoleIndex);
        }
    }

    private float GetSpawnInterval()
    {
        float baseDelay = 1.2f;
        if (currentPhase == 2) baseDelay = 0.95f;
        if (currentPhase == 3) baseDelay = 0.7f; // В 3 фазе бегают чаще!

        if (selectedDifficulty == DifficultyLevel.Hard) baseDelay *= 0.75f;
        if (selectedDifficulty == DifficultyLevel.Easy) baseDelay *= 1.25f;

        return Random.Range(baseDelay * 0.75f, baseDelay * 1.25f);
    }

    /// <summary>
    /// Создание бегущей мышки с поддержкой покадровой анимации
    /// </summary>
    private void SpawnMouseRunner(MouseType type, int startIdx, int endIdx)
    {
        if (miceRunningLayer == null || holes == null) return;

        GameObject mouseObj = new GameObject($"Mouse_{type}");
        mouseObj.transform.SetParent(miceRunningLayer, false);

        RectTransform rt = mouseObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(110f, 70f);

        Image img = mouseObj.AddComponent<Image>();
        img.sprite = GetMouseSprite(type);
        img.preserveAspect = true;

        Button btn = mouseObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        Vector2 startPos = holes[startIdx].anchoredPosition;
        Vector2 endPos = holes[endIdx].anchoredPosition;

        float roadY = roadTrack != null ? roadTrack.anchoredPosition.y : -50f;
        startPos.y = roadY;
        endPos.y = roadY;

        bool runRight = endPos.x > startPos.x;
        rt.localScale = new Vector3(runRight ? 1f : -1f, 1f, 1f);
        rt.anchoredPosition = startPos;

        btn.onClick.AddListener(() => OnMouseClicked(mouseObj, type));

        activeMice.Add(mouseObj);

        // Запуск покадровой анимации если загружены спрайт-массивы
        Sprite[] animFrames = GetAnimationFrames(type);
        if (animFrames != null && animFrames.Length > 1)
        {
            StartCoroutine(AnimateSpriteFrames(mouseObj, img, animFrames));
        }

        // Длительность перебежки (Черные мыши бегают быстрее всех!)
        float runDuration = GetRunDuration(type);
        StartCoroutine(AnimateMouseRunning(mouseObj, rt, startPos, endPos, runDuration));
    }

    private float GetRunDuration(MouseType type)
    {
        float speed = 2.2f;
        if (type == MouseType.Golden) speed = 2.4f;
        if (type == MouseType.Silver) speed = 1.9f;
        if (type == MouseType.Black) speed = 1.3f; // Теневая черная — самая быстрая!

        if (selectedDifficulty == DifficultyLevel.Hard) speed *= 0.75f;
        if (selectedDifficulty == DifficultyLevel.Easy) speed *= 1.25f;

        return Random.Range(speed * 0.85f, speed * 1.15f);
    }

    private IEnumerator AnimateSpriteFrames(GameObject mouseObj, Image img, Sprite[] frames)
    {
        int frameIndex = 0;
        float frameTime = 1f / Mathf.Max(1f, animationFps);

        while (mouseObj != null && img != null)
        {
            img.sprite = frames[frameIndex];
            frameIndex = (frameIndex + 1) % frames.Length;
            yield return new WaitForSeconds(frameTime);
        }
    }

    private IEnumerator AnimateMouseRunning(GameObject mouseObj, RectTransform rt, Vector2 start, Vector2 end, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && mouseObj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float jumpOffset = Mathf.Sin(t * Mathf.PI * 6f) * 6f;
            rt.anchoredPosition = Vector2.Lerp(start, end, t) + new Vector2(0f, jumpOffset);
            yield return null;
        }

        if (mouseObj != null)
        {
            activeMice.Remove(mouseObj);
            Destroy(mouseObj);
        }
    }

    /// <summary>
    /// Обработка клика по мышке с проверкой правильности цели
    /// </summary>
    public void OnMouseClicked(GameObject mouseObj, MouseType type)
    {
        if (!isGameActive || isGameWon || isPhaseTransitioning) return;

        if (type == currentTargetType)
        {
            // ПРАВИЛЬНАЯ МЫШКА!
            miceCaughtInPhase++;
            if (catchMouseSound != null && SettingsManager.Instance != null)
                SettingsManager.Instance.PlaySoundEffect(catchMouseSound);

            // Удаляем пойманную мышку
            if (mouseObj != null)
            {
                activeMice.Remove(mouseObj);
                Destroy(mouseObj);
            }

            UpdateTaskUI();

            // Проверка завершения текущей фазы
            if (miceCaughtInPhase >= targetMiceForCurrentPhase)
            {
                OnPhaseCompleted();
            }
        }
        else
        {
            // ОШИБКА: ИГРОК НАЖАЛ НА ЗАПРЕЩЕННУЮ МЫШКУ!
            if (wrongMouseSound != null && SettingsManager.Instance != null)
                SettingsManager.Instance.PlaySoundEffect(wrongMouseSound);

            // Сброс текущей фазы и перезапуск заново согласно правилам
            string errorReason = $"Ой! Вы поймали не ту мышь! Этап {currentPhase} сброшен заново!";
            RestartCurrentPhase(errorReason);
        }
    }

    /// <summary>
    /// Перезапуск текущей фазы при ошибке
    /// </summary>
    private void RestartCurrentPhase(string notice)
    {
        ClearAllMice();
        miceCaughtInPhase = 0;
        UpdateTaskUI();

        StartCoroutine(ShowCentralNoticeRoutine("ОШИБКА!", notice, () =>
        {
            StartPhase(currentPhase);
        }));
    }

    /// <summary>
    /// Успешное прохождение фазы и переход к следующей
    /// </summary>
    private void OnPhaseCompleted()
    {
        ClearAllMice();

        if (phaseCompleteSound != null && SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(phaseCompleteSound);

        if (currentPhase == 1)
        {
            // Переход к Фазе 2: Серебряные (10)
            StartCoroutine(ShowCentralNoticeRoutine(
                "ОТЛИЧНО! НО ЭТО ЕЩЕ НЕ ВСЁ!",
                "Вы поймали 5 золотых мышей! Теперь поймайте <b>10 серебряных</b>.\nЧерных и золотых не трогать!",
                () => StartPhase(2)
            ));
        }
        else if (currentPhase == 2)
        {
            // Переход к Фазе 3: Черные (20)
            StartCoroutine(ShowCentralNoticeRoutine(
                "МОЛОДЕЦ! ФИНАЛЬНЫЙ РЫВОК!",
                "Вы поймали 10 серебряных мышей! Теперь усиленно ловите <b>20 самых быстрых черных мышей</b>!",
                () => StartPhase(3)
            ));
        }
        else if (currentPhase == 3)
        {
            // ПОЛНАЯ ПОБЕДА ВО ВСЕХ 3 ЭТАПАХ!
            isGameWon = true;
            isGameActive = false;

            if (winFanfareSound != null && SettingsManager.Instance != null)
                SettingsManager.Instance.PlaySoundEffect(winFanfareSound);

            ShowVictoryPopup();
        }
    }

    private IEnumerator ShowCentralNoticeRoutine(string title, string body, System.Action onComplete)
    {
        isPhaseTransitioning = true;

        if (centralPhaseNoticePanel != null)
        {
            centralPhaseNoticePanel.SetActive(true);
            if (centralNoticeTitle != null) centralNoticeTitle.text = title;
            if (centralNoticeBody != null) centralNoticeBody.text = body;
        }

        yield return new WaitForSeconds(3.0f);

        if (centralPhaseNoticePanel != null)
            centralPhaseNoticePanel.SetActive(false);

        isPhaseTransitioning = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Отображение окна победы с пересчетом наград по множителю сложности
    /// </summary>
    private void ShowVictoryPopup()
    {
        if (rewardPopupPanel != null)
        {
            rewardPopupPanel.SetActive(true);
        }

        float mult = GetDifficultyMultiplier();
        int baseGold = 5000;
        int baseStones = 10;
        int baseScrolls = 2;
        int baseXP = 600;

        int finalGold = Mathf.RoundToInt(baseGold * mult);
        int finalStones = Mathf.RoundToInt(baseStones * mult);
        int finalScrolls = Mathf.RoundToInt(baseScrolls * mult);
        int finalXP = Mathf.RoundToInt(baseXP * mult);

        string diffName = selectedDifficulty == DifficultyLevel.Easy ? "Легкий (x1.0)" :
                         (selectedDifficulty == DifficultyLevel.Normal ? "Средний (x1.5)" : "Сложный (x2.5)");

        if (rewardDescriptionText != null)
        {
            rewardDescriptionText.text = $"Уровень сложности: <b><color=#FFD166>{diffName}</color></b>\n\n" +
                                         $"<b><color=#FFD166>+{finalGold:N0} Золота</color></b>\n" +
                                         $"<b><color=#A0C4FF>+{finalStones} Камней</color></b>\n" +
                                         $"<b><color=#CDB4DB>+{finalScrolls} Свитка</color></b>\n" +
                                         $"<b><color=#80FFDB>+{finalXP} Опыта Игрока</color></b>\n" +
                                         $"<b><color=#F72585>+1 Особое Зелье Мастерства</color></b>";
        }
    }

    private float GetDifficultyMultiplier()
    {
        switch (selectedDifficulty)
        {
            case DifficultyLevel.Easy: return 1.0f;
            case DifficultyLevel.Normal: return 1.5f;
            case DifficultyLevel.Hard: return 2.5f;
            default: return 1.0f;
        }
    }

    /// <summary>
    /// Выдача всех наград игроку и начисление в систему сохранения
    /// </summary>
    public void ClaimRewardAndExit()
    {
        if (clickSound != null && SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySoundEffect(clickSound);

        float mult = GetDifficultyMultiplier();
        int finalGold = Mathf.RoundToInt(5000 * mult);
        int finalStones = Mathf.RoundToInt(10 * mult);
        int finalScrolls = Mathf.RoundToInt(2 * mult);
        int finalXP = Mathf.RoundToInt(600 * mult);

        // 1. Начисление ресурсов в PlayerPrefs
        int gold = PlayerPrefs.GetInt("Player_Gold", 0) + finalGold;
        int stones = PlayerPrefs.GetInt("Player_Stones", 0) + finalStones;
        int scrolls = PlayerPrefs.GetInt("Player_Scrolls", 0) + finalScrolls;
        int xp = PlayerPrefs.GetInt("Player_XP", 0) + finalXP;

        PlayerPrefs.SetInt("Player_Gold", gold);
        PlayerPrefs.SetInt("Player_Stones", stones);
        PlayerPrefs.SetInt("Player_Scrolls", scrolls);
        PlayerPrefs.SetInt("Player_XP", xp);
        PlayerPrefs.Save();

        // 2. Добавление зелья в инвентарь
        if (RecipeCrafting_Manager.Instance != null)
        {
            RecipeCrafting_Manager.Instance.AddPotionToFirstEmptySlot("Player_Potion_XP_Mastery", $"Зелье Мастерства (+{finalXP} XP)");
        }

        // 3. Обновление ресурсов в верхнем UI
        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.RefreshResourceDisplay();
        }

        // 4. Начисление опыта аватара
        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.GainPlayerExperience(finalXP);
        }

        CloseMinigame();
    }

    public void CloseMinigame()
    {
        ClearAllMice();
        if (gamePanel != null)
            gamePanel.SetActive(false);
    }

    private void ClearAllMice()
    {
        foreach (var m in activeMice)
        {
            if (m != null) Destroy(m);
        }
        activeMice.Clear();
    }

    private Sprite GetMouseSprite(MouseType type)
    {
        switch (type)
        {
            case MouseType.Golden: return goldenMouseSprite;
            case MouseType.Silver: return silverMouseSprite;
            case MouseType.Black: return blackMouseSprite;
            default: return goldenMouseSprite;
        }
    }

    private Sprite[] GetAnimationFrames(MouseType type)
    {
        switch (type)
        {
            case MouseType.Golden: return goldenMouseFrames;
            case MouseType.Silver: return silverMouseFrames;
            case MouseType.Black: return shadowMouseFrames;
            default: return null;
        }
    }
}
