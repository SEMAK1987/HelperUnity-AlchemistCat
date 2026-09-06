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
        gold = PlayerPrefs.GetInt("Player_Gold", 5000); // Загрузка золота (по умолчанию 5000)
        crystals = PlayerPrefs.GetInt("Player_Crystals", 0); // Загрузка кристаллов (по умолчанию 0)
        stones = PlayerPrefs.GetInt("Player_Stones", 10); // Загрузка камней (по умолчанию 10)
        scrolls = PlayerPrefs.GetInt("Player_Scrolls", 3); // Загрузка свитков (по умолчанию 3)
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

    public void AddGold(int amount) // Добавление золота на счет игрока с автосохранением и синхронизацией
    {
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
        if (cauldronText != null) cauldronText.text = $"Ур. {cauldronLevel}"; // Отображение уровня котла
    }

    public void SyncUI() // Публичный триггер синхронизации UI
    {
        UpdateUI(); // Вызов обновления интерфейса
    }
}
