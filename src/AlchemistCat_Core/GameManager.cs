using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Глобальный игровой менеджер (GameManager) — синглтон для сохранения, загрузки и учета ресурсов.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance; // Приватное статическое поле синглтона
    public static GameManager Instance // Публичный аксессор синглтона с умным автопоиском и автосозданием
    {
        get
        {
            if (_instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                _instance = FindFirstObjectByType<GameManager>(); // Поиск в Unity 2023+
#else
                _instance = FindObjectOfType<GameManager>(); // Поиск в ранних версиях Unity
#endif
                if (_instance == null)
                {
                    // Если объект забыли повесить в сцене, он создается автоматически в _GameSystems
                    GameObject go = GameObject.Find("_GameSystems");
                    if (go == null)
                    {
                        go = new GameObject("_GameSystems");
                        DontDestroyOnLoad(go);
                    }
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    [Header("Экономика и Прогресс")]
    public int gold = 0; // Текущий запас золотых монет игрока (на старте 0 до начисления стартовой награды)
    public int crystals = 0; // Текущий запас премиальных кристаллов (на старте 0)
    public int stones = 0; // Текущее количество алхимических камней (на старте 0)
    public int scrolls = 0; // Текущее количество древних свитков (на старте 0)
    public int vipXP = 0; // Накопленный опыт VIP-системы
    public int daysActive = 1; // Количество активных дней в игре
    public int catLevel = 1; // Текущий уровень персонажа (кота)
    public int currentXP = 0; // Текущий опыт для следующего уровня
    public int xpToNextLevel = 10; // Требуемый опыт для перехода на 2 уровень (10 XP)
    public int cauldronLevel = 1; // Уровень прокачки алхимического котла
    public int potionsBrewed = 0; // Общее число сваренных зелий за игру

    [Header("Разблокированные Квесты / Игры")]
    public bool unlockedDarts = false; // Открыта ли мини-игра "Алхимический дартс"
    public bool unlockedMouseCatch = false; // Открыта ли мини-игра "Поймай мышь"

    [Header("UI Ссылки на Ресурсы (Опционально)")]
    public TextMeshProUGUI goldText; // Текстовый индикатор количества золота
    public TextMeshProUGUI crystalsText; // Текстовый индикатор количества кристаллов
    public TextMeshProUGUI stonesText; // Текстовый индикатор камней
    public TextMeshProUGUI scrollsText; // Текстовый индикатор свитков
    public TextMeshProUGUI levelText; // Текстовый индикатор уровня игрока
    public TextMeshProUGUI xpText; // Текстовый индикатор опыта (XP)
    public TextMeshProUGUI cauldronText; // Текстовый индикатор уровня котла
    public Slider xpSlider; // Графическая полоса прогресса опыта (Slider)
    public Image xpFillImage; // Полоса прогресса опыта типа Image Fill (например, Exp_Progress_Bar)

    private void Awake() // Инициализация синглтона при загрузке объекта
    {
        if (Instance != null && Instance != this) // Проверка на наличие существующего синглтона
        {
            Destroy(gameObject); // Защита от дубликатов при перезагрузке сцены
            return; // Выход из метода
        }
        Instance = this; // Назначение глобального экземпляра
    }

    private void Start() // Стартовая загрузка данных игрока и обновление UI
    {
        LoadResourcesFromPlayerPrefs(); // Загрузка всех сохраненных балансов и уровня
        UpdateUI(); // Обновление числовых показателей на экране
    }

    public void LoadResourcesFromPlayerPrefs() // Чтение баланса золота, кристаллов, камней и опыта из PlayerPrefs
    {
        gold = PlayerPrefs.GetInt("Player_Gold", 0); // Загрузка золота (по умолчанию 0)
        crystals = PlayerPrefs.GetInt("Player_Crystals", 0); // Загрузка кристаллов (по умолчанию 0)
        stones = PlayerPrefs.GetInt("Player_Stones", 0); // Загрузка камней (по умолчанию 0)
        scrolls = PlayerPrefs.GetInt("Player_Scrolls", 0); // Загрузка свитков (по умолчанию 0)
        currentXP = PlayerPrefs.GetInt("Player_XP", 0); // Загрузка текущего опыта (по умолчанию 0)
        catLevel = PlayerPrefs.GetInt("Player_Level", 1); // Загрузка уровня персонажа (по умолчанию 1)
    }

    public void SaveResourcesToPlayerPrefs() // Сохранение текущих значений ресурсов и опыта в PlayerPrefs
    {
        PlayerPrefs.SetInt("Player_Gold", gold); // Сохранение золота
        PlayerPrefs.SetInt("Player_Crystals", crystals); // Сохранение кристаллов
        PlayerPrefs.SetInt("Player_Stones", stones); // Сохранение камней
        PlayerPrefs.SetInt("Player_Scrolls", scrolls); // Сохранение свитков
        PlayerPrefs.SetInt("Player_XP", currentXP); // Сохранение опыта
        PlayerPrefs.SetInt("Player_Level", catLevel); // Сохранение уровня
        PlayerPrefs.Save(); // Запись изменений на постоянный накопитель
    }

    [ContextMenu("Сбросить Все Данные GameManager (Reset All Data)")]
    public void ResetAllData() // Полный сброс ресурсов и параметров GameManager
    {
        gold = 0; // Обнуление золота
        crystals = 0; // Обнуление кристаллов
        stones = 0; // Обнуление камней
        scrolls = 0; // Обнуление свитков
        currentXP = 0; // Обнуление опыта
        catLevel = 1; // Возврат на 1 уровень
        cauldronLevel = 1; // Базовый уровень котла
        potionsBrewed = 0; // Обнуление числа зелий
        unlockedDarts = false; // Блокировка дартса
        unlockedMouseCatch = false; // Блокировка мышей
        SaveResourcesToPlayerPrefs(); // Запись обнуленных значений в реестр
        UpdateUI(); // Обновление отображения в интерфейсе
    }

    public void AddGold(int amount) // Добавление золота на счет игрока с автосохранением и синхронизацией
    {
        LoadResourcesFromPlayerPrefs(); // Загрузка актуального баланса перед начислением
        gold += amount; // Увеличение запаса золота
        SaveResourcesToPlayerPrefs(); // Сохранение обновленного баланса
        UpdateUI(); // Обновление числовых данных в интерфейсе
        if (DialogueSystem_Manager.Instance != null) // Если диалоговый менеджер активен
        {
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources(); // Синхронизация отображения в шапке
        }
    }

    public void AddCrystals(int amount) // Добавление кристаллов с автосохранением и обновлением
    {
        LoadResourcesFromPlayerPrefs(); // Загрузка актуального баланса перед начислением
        crystals += amount; // Увеличение запаса кристаллов
        SaveResourcesToPlayerPrefs(); // Сохранение обновленного баланса
        UpdateUI(); // Обновление числовых данных в интерфейсе
        if (DialogueSystem_Manager.Instance != null) // Если диалоговый менеджер активен
        {
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources(); // Синхронизация отображения в шапке
        }
    }

    public void AddResources(int addGold, int addStones, int addScrolls, int addCrystals) // Массовое начисление всех видов ресурсов
    {
        LoadResourcesFromPlayerPrefs(); // Загрузка актуального баланса перед начислением
        gold += addGold; // Начисление золота
        stones += addStones; // Начисление камней
        scrolls += addScrolls; // Начисление свитков
        crystals += addCrystals; // Начисление кристаллов
        SaveResourcesToPlayerPrefs(); // Сохранение всех обновленных ресурсов
        UpdateUI(); // Обновление числовых данных в интерфейсе
        if (DialogueSystem_Manager.Instance != null) // Если диалоговый менеджер активен
        {
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources(); // Синхронизация отображения в шапке
        }
    }

    public void AddXP(int amount) // Начисление опыта игроку и персонажу с автосохранением
    {
        currentXP += amount; // Увеличение накопленного опыта
        if (Avatar_Manager.Instance != null) // Если менеджер аватаров активен
        {
            Avatar_Manager.Instance.GainPlayerExperience(amount); // Передача опыта в систему аватара
        }
        SaveResourcesToPlayerPrefs(); // Сохранение опыта в PlayerPrefs
        UpdateUI(); // Обновление индикаторов уровня и прогресс-бара
    }

    public void AddExperience(int amount) // Синоним начисления опыта для полной совместимости систем
    {
        AddXP(amount); // Перенаправление в основной метод начисления опыта
    }

    public void AddVipXP(int amount) // Начисление очков VIP-опыта
    {
        vipXP += amount; // Увеличение VIP очков
        SaveResourcesToPlayerPrefs(); // Сохранение прогресса
    }

    public void SpendResources(int spendGold, int spendStones, int spendScrolls, int spendCrystals) // Списание ресурсов игрока
    {
        gold = Mathf.Max(0, gold - spendGold); // Списание золота
        stones = Mathf.Max(0, stones - spendStones); // Списание камней
        scrolls = Mathf.Max(0, scrolls - spendScrolls); // Списание свитков
        crystals = Mathf.Max(0, crystals - spendCrystals); // Списание кристаллов
        SaveResourcesToPlayerPrefs(); // Сохранение нового баланса
        UpdateUI(); // Обновление интерфейса
        if (DialogueSystem_Manager.Instance != null) // Если диалоговый менеджер доступен
        {
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources(); // Синхронизация верхнего тулбара
        }
    }

    public void UnlockDarts() // Разблокировка мини-игры дартс
    {
        unlockedDarts = true; // Установка флага
        PlayerPrefs.SetInt("Minigame_Darts_Unlocked", 1); // Сохранение в PlayerPrefs
        PlayerPrefs.Save(); // Запись
    }

    public void UnlockMouseCatch() // Разблокировка мини-игры ловли мышей
    {
        unlockedMouseCatch = true; // Установка флага
        PlayerPrefs.SetInt("Minigame_MouseCatch_Unlocked", 1); // Сохранение в PlayerPrefs
        PlayerPrefs.Save(); // Запись
    }

    public void UpdateUI() // Обновление текста всех UI-элементов и слайдера опыта
    {
        if (goldText != null) goldText.text = gold.ToString(); // Отображение золота
        if (crystalsText != null) crystalsText.text = crystals.ToString(); // Отображение кристаллов
        if (stonesText != null) stonesText.text = stones.ToString(); // Отображение камней
        if (scrollsText != null) scrollsText.text = scrolls.ToString(); // Отображение свитков
        if (levelText != null) levelText.text = catLevel.ToString(); // Отображение уровня
        if (xpText != null) xpText.text = $"{currentXP}/{xpToNextLevel}"; // Отображение дроби опыта
        if (xpSlider != null) // Если полоса опыта назначена
        {
            xpSlider.maxValue = xpToNextLevel; // Установка максимума слайдера
            xpSlider.value = currentXP; // Установка текущего значения слайдера
        }
        if (xpFillImage != null) // Если назначена шкала опыта типа Image Fill
        {
            xpFillImage.fillAmount = xpToNextLevel > 0 ? (float)currentXP / xpToNextLevel : 0f; // Расчет прогресса заполнения
        }
        if (cauldronText != null) cauldronText.text = $"Ур. {cauldronLevel}"; // Отображение уровня котла
    }

    public void SyncUI() // Публичный триггер синхронизации UI
    {
        UpdateUI(); // Вызов обновления интерфейса
    }
}
