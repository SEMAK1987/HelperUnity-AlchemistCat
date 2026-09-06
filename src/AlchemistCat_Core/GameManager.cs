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
    public static GameManager Instance { get; private set; } // Статический синглтон для доступа к игровым ресурсам

    [Header("Экономика и Прогресс")]
    public int gold = 0; // Текущий запас золотых монет игрока
    public int crystals = 10; // Текущий запас премиальных кристаллов
    public int stones = 10; // Текущее количество алхимических камней
    public int scrolls = 3; // Текущее количество древних свитков
    public int vipXP = 0; // Накопленный опыт VIP-системы
    public int daysActive = 1; // Количество активных дней в игре
    public int catLevel = 1; // Текущий уровень персонажа (кота)
    public int currentXP = 0; // Текущий опыт для следующего уровня
    public int xpToNextLevel = 100; // Требуемый опыт для перехода на следующий уровень
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Защита от дубликатов при перезагрузке сцены
            return;
        }
        Instance = this; // Назначение глобального экземпляра
    }

    private void Start()
    {
        LoadResourcesFromPlayerPrefs(); // Загрузка всех сохраненных балансов и уровня
        UpdateUI(); // Обновление числовых показателей на экране
    }

    public void LoadResourcesFromPlayerPrefs()
    {
        gold = PlayerPrefs.GetInt("Player_Gold", 5000);
        crystals = PlayerPrefs.GetInt("Player_Crystals", 0);
        stones = PlayerPrefs.GetInt("Player_Stones", 10);
        scrolls = PlayerPrefs.GetInt("Player_Scrolls", 3);
        currentXP = PlayerPrefs.GetInt("Player_XP", 0);
        catLevel = PlayerPrefs.GetInt("Player_Level", 1);
    }

    public void SaveResourcesToPlayerPrefs()
    {
        PlayerPrefs.SetInt("Player_Gold", gold);
        PlayerPrefs.SetInt("Player_Crystals", crystals);
        PlayerPrefs.SetInt("Player_Stones", stones);
        PlayerPrefs.SetInt("Player_Scrolls", scrolls);
        PlayerPrefs.SetInt("Player_XP", currentXP);
        PlayerPrefs.SetInt("Player_Level", catLevel);
        PlayerPrefs.Save();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        SaveResourcesToPlayerPrefs();
        UpdateUI();
        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources();
        }
    }

    public void AddCrystals(int amount)
    {
        crystals += amount;
        SaveResourcesToPlayerPrefs();
        UpdateUI();
        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources();
        }
    }

    public void AddResources(int addGold, int addStones, int addScrolls, int addCrystals)
    {
        gold += addGold;
        stones += addStones;
        scrolls += addScrolls;
        crystals += addCrystals;
        SaveResourcesToPlayerPrefs();
        UpdateUI();
        if (DialogueSystem_Manager.Instance != null)
        {
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources();
        }
    }

    public void AddXP(int amount)
    {
        currentXP += amount;
        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.GainPlayerExperience(amount);
        }
        SaveResourcesToPlayerPrefs();
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (goldText != null) goldText.text = gold.ToString();
        if (crystalsText != null) crystalsText.text = crystals.ToString();
        if (stonesText != null) stonesText.text = stones.ToString();
        if (scrollsText != null) scrollsText.text = scrolls.ToString();
        if (levelText != null) levelText.text = catLevel.ToString();
        if (xpText != null) xpText.text = $"{currentXP}/{xpToNextLevel}";
        if (xpSlider != null)
        {
            xpSlider.maxValue = xpToNextLevel;
            xpSlider.value = currentXP;
        }
        if (cauldronText != null) cauldronText.text = $"Ур. {cauldronLevel}";
    }

    public void SyncUI()
    {
        UpdateUI();
    }
}
