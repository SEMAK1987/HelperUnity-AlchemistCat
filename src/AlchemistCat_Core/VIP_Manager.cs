using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VIP_Manager : MonoBehaviour
{
    public static VIP_Manager Instance; // Статический синглтон для доступа к VIP системе из других скриптов

    [Header("=== VIP Данные ===")]
    public int currentVIPLevel = 0; // Текущий уровень VIP (от 0 до 30)
    public int currentVIP_XP = 0; // Текущее количество очков опыта VIP
    public bool isDivineRealmActive = false; // Активен ли режим "Божественное Царство" (VIP 30)

    [Header("=== UI Элементы ===")]
    public GameObject vipPanel; // Главная панель окна VIP системы
    public Button vipOpenIconButton; // Кнопка-иконка открытия VIP в верхнем интерфейсе
    public TextMeshProUGUI vipLevelText; // Текст для вывода текущего уровня VIP
    public Slider vipXpSlider; // Полоса прогресса (слайдер) для шкалы опыта VIP
    public TextMeshProUGUI vipXpProgressText; // Текстовый индикатор опыта "текущий / максимум"
    public Button divineRealmButton; // Кнопка активации бонуса "Божественное Царство"

    // Массив порогов опыта для достижения каждого из 30 уровней VIP
    private readonly int[] vipXpRequirements = new int[30]
    {
        100, 300, 600, 1000, 2500, 5000, 8000, 12000, 18000, 25000,
        35000, 50000, 70000, 95000, 125000, 160000, 200000, 250000, 310000, 380000,
        460000, 550000, 650000, 750000, 850000, 900000, 940000, 970000, 990000, 1000000
    };

    private void Awake() // Инициализация синглтона и загрузка данных
    {
        Instance = this; // Инициализация синглтона
        LoadVIPData(); // Загрузка сохраненного VIP прогресса из памяти
    }

    public void UnlockVIPFeature() // Разблокировка кнопки VIP в интерфейсе
    {
        if (vipOpenIconButton != null) // Если кнопка задана
            vipOpenIconButton.gameObject.SetActive(true); // Включаем кнопку VIP при достижении 2 ранга
    }

    public void AddVIP_XP(int amount) // Начисление очков опыта VIP
    {
        currentVIP_XP += amount; // Прибавляем начисленный опыт VIP
        CheckLevelUp(); // Проверяем, повысился ли уровень VIP
        SaveVIPData(); // Сохраняем новые данные в память
        UpdateUI(); // Обновляем отображение на экране
    }

    public void ProcessMonthlyCalendarBonus(int missedDays) // Начисление бонуса за календарный месяц
    {
        if (missedDays == 0) AddVIP_XP(100); // 0 пропусков в календаре: +100 VIP опыта
        else if (missedDays == 1) AddVIP_XP(50); // 1 пропуск в календаре: +50 VIP опыта
        // 2 и более пропусков: 0 очков
    }

    public void ProcessFullYearBonus() // Начисление супер-бонуса за полный год
    {
        AddVIP_XP(5000); // Закрыты все 12 месяцев года: +5000 VIP опыта
    }

    private void CheckLevelUp() // Проверка повышения ранга VIP
    {
        for (int i = 0; i < 30; i++) // Проходим по всем 30 рангам
        {
            if (currentVIP_XP >= vipXpRequirements[i]) // Если опыта достаточно
            {
                currentVIPLevel = i + 1; // Устанавливаем новый уровень VIP
            }
        }
    }

    public void OpenVIPPanel() // Открытие окна VIP меню
    {
        if (vipPanel != null) // Если окно существует
        {
            vipPanel.SetActive(true); // Открываем панель VIP
            UpdateUI(); // Обновляем все тексты и слайдеры
        }
    }

    public void CloseVIPPanel() // Закрытие окна VIP меню
    {
        if (vipPanel != null) // Если окно задано
            vipPanel.SetActive(false); // Закрываем окно VIP
    }

    public void ActivateDivineRealmMode() // Активация режима 30 ранга
    {
        if (currentVIPLevel >= 30 && !isDivineRealmActive) // Проверка: достигнут ли 30 ранг VIP
        {
            isDivineRealmActive = true; // Включаем режим удвоения бонусов
            Debug.Log("Режим 'Божественное Царство' активирован!"); // Вывод в консоль
        }
    }

    private void UpdateUI() // Обновление текста уровня и слайдера
    {
        if (vipLevelText != null) vipLevelText.text = $"VIP {currentVIPLevel}"; // Вывод номера уровня
        if (vipXpSlider != null) // Если слайдер задан
        {
            int maxXP = currentVIPLevel < 30 ? vipXpRequirements[currentVIPLevel] : vipXpRequirements[29]; // Получаем порог опыта
            vipXpSlider.maxValue = maxXP; // Задаем максимум слайдера
            vipXpSlider.value = currentVIP_XP; // Задаем текущее значение
            if (vipXpProgressText != null) vipXpProgressText.text = $"{currentVIP_XP} / {maxXP} XP"; // Вывод текста опыта
        }
    }

    private void SaveVIPData() // Сохранение данных VIP в PlayerPrefs
    {
        PlayerPrefs.SetInt("VIP_Level", currentVIPLevel); // Сохраняем уровень в PlayerPrefs
        PlayerPrefs.SetInt("VIP_XP", currentVIP_XP); // Сохраняем опыт в PlayerPrefs
        PlayerPrefs.Save(); // Записываем на диск
    }

    private void LoadVIPData() // Загрузка данных VIP из PlayerPrefs
    {
        currentVIPLevel = PlayerPrefs.GetInt("VIP_Level", 0); // Считываем уровень
        currentVIP_XP = PlayerPrefs.GetInt("VIP_XP", 0); // Считываем опыт
        UpdateUI(); // Обновляем интерфейс
    }
}
