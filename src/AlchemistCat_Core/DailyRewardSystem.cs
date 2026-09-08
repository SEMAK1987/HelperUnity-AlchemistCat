using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Циклическая система ежедневных наград:
/// - Обычные дни: простые ресурсы (Золото, Камни, Свитки)
/// - Конец 1-й и 2-й недели (День 7, 14): небольшое число кристаллов (1-2 шт)
/// - Крупные вехи без пропусков (1, 3, 6, 8, 12 месяцев): щедрые награды с кристаллами
/// </summary>
public class DailyRewardSystem : MonoBehaviour
{
    [Header("UI Ссылки (Назначаются в Инспекторе или ищутся автоматически)")]
    public Button claimButton; // Кнопка "Забрать награду"
    public Text timerText; // Текстовое поле таймера обратного отсчета (стандартный UI Text)
    public TextMeshProUGUI timerTextTMP; // Текстовое поле таймера обратного отсчета (TextMeshPro)
    public Text statusText; // Текстовое поле статуса (стандартный UI Text)
    public TextMeshProUGUI statusTextTMP; // Текстовое поле статуса (TextMeshPro)
    [Tooltip("Массив из 7 слотов дней (День 1 - День 7). Если пуст, находится автоматически.")]
    public Transform[] calendarDaySlots; // Массив UI-контейнеров для 7 дней недели наград

    public static DailyRewardSystem Instance { get; private set; } // Публичный синглтон для глобального доступа и сброса

    private int currentStreak = 0; // Текущая серия непрерывных заходов в игру (дни 1-7)
    private int totalContinuousDays = 0; // Общий непрерывный счетчик дней без пропусков
    private DateTime lastClaimTime; // Время и дата последнего получения награды

    private void Awake() // Инициализация синглтона
    {
        if (Instance != null && Instance != this) // Проверка на дубликат
        {
            Destroy(gameObject); // Защита от дубликатов
            return;
        }
        Instance = this; // Назначение глобального экземпляра
    }

    private void Start() // Инициализация компонентов при старте сцены
    {
        ValidateInspectorReferences(); // Проверка и автопоиск привязанных UI компонентов
        LoadDailyData(); // Загрузка сохраненного дня и времени последнего захода
        CheckDailyStatus(); // Проверка доступности награды на сегодняшний день
    }

    private void Update() // Покадровое обновление таймера
    {
        CheckDailyStatus(); // Постоянное обновление таймера обратного отсчета в реальном времени
    }

    private void ValidateInspectorReferences() // Проверка и автопоиск привязанных UI компонентов
    {
        if (claimButton == null) // Если кнопка сбора не задана
        {
            claimButton = GetComponentInChildren<Button>(true); // Поиск кнопки в дочерних объектах
            if (claimButton == null) // Если не найдена
            {
                Button[] buttons = GetComponentsInChildren<Button>(true); // Получение всех кнопок
                foreach (var b in buttons) // Перебор кнопок
                {
                    if (b.name.ToLower().Contains("claim") || b.name.ToLower().Contains("reward") || b.name.ToLower().Contains("button") || b.name.ToLower().Contains("close")) // Поиск по ключевым именам
                    {
                        claimButton = b; // Назначение найденной кнопки
                        break; // Выход из цикла
                    }
                }
            }
        }

        if (timerText == null && timerTextTMP == null) // Если текст таймера не задан
        {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true); // Поиск TextMeshPro
            foreach (var t in tmps) // Перебор
            {
                if (t.name.ToLower().Contains("timer") || t.name.ToLower().Contains("time")) // Поиск по имени
                {
                    timerTextTMP = t; // Привязка TMP
                    break;
                }
            }
            if (timerTextTMP == null) // Если TMP не найден
            {
                Text[] texts = GetComponentsInChildren<Text>(true); // Получение всех стандартных текстов
                foreach (var t in texts) // Перебор текстов
                {
                    if (t.name.ToLower().Contains("timer") || t.name.ToLower().Contains("time")) // Поиск по имени
                    {
                        timerText = t; // Привязка текста
                        break; // Выход
                    }
                }
            }
        }

        if (statusText == null && statusTextTMP == null) // Если текст статуса не задан
        {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true); // Поиск TextMeshPro
            foreach (var t in tmps) // Перебор
            {
                if (t.name.ToLower().Contains("status") || t.name.ToLower().Contains("title") || t.name.ToLower().Contains("info") || t.name.ToLower().Contains("reward")) // Поиск по имени
                {
                    statusTextTMP = t; // Привязка TMP
                    break;
                }
            }
            if (statusTextTMP == null) // Если TMP не найден
            {
                Text[] texts = GetComponentsInChildren<Text>(true); // Получение всех текстов
                foreach (var t in texts) // Перебор текстов
                {
                    if (t.name.ToLower().Contains("status") || t.name.ToLower().Contains("title") || t.name.ToLower().Contains("info")) // Поиск по имени
                    {
                        statusText = t; // Привязка текста
                        break; // Выход
                    }
                }
            }
        }

        if (calendarDaySlots == null || calendarDaySlots.Length == 0) // Если массив слотов пуст
        {
            System.Collections.Generic.List<Transform> foundSlots = new System.Collections.Generic.List<Transform>(); // Временный список
            foreach (Transform child in transform) // Перебор дочерних трансформаций
            {
                if (child.name.ToLower().Contains("day") || child.name.ToLower().Contains("slot") || child.name.ToLower().Contains("item") || child.name.ToLower().Contains("cell")) // Поиск слотов
                {
                    foundSlots.Add(child); // Добавление найденного слота
                }
            }
            if (foundSlots.Count > 0) // Если слоты найдены
            {
                calendarDaySlots = foundSlots.ToArray(); // Преобразование в массив
            }
        }
    }

    private void CheckDailyStatus() // Проверка текущего статуса ежедневной награды
    {
        TimeSpan difference = DateTime.Now - lastClaimTime; // Разница во времени с последнего захода
        bool isRewardReady = false; // Флаг готовности награды

        if (difference.TotalHours >= 24 && difference.TotalHours < 48) // Если прошло от 24 до 48 часов
        {
            isRewardReady = true; // Награда готова к получению
            if (claimButton != null) claimButton.interactable = true; // Активация кнопки сбора
            if (timerText != null) timerText.text = "Новая награда готова!"; // Текст готовности
        }
        else if (difference.TotalHours >= 48) // Если пропущено более 48 часов
        {
            // Сброс серии за пропуск дня
            currentStreak = 0; // Сброс 7-дневной серии
            totalContinuousDays = 0; // Сброс непрерывного общего счетчика
            isRewardReady = true; // Награда 1-го дня готова
            if (claimButton != null) claimButton.interactable = true; // Активация кнопки
            if (timerText != null) timerText.text = "Серия сброшена! Заберите День 1."; // Уведомление о сбросе
        }
        else // Если 24 часа еще не прошло
        {
            isRewardReady = false; // Награда пока не готова
            if (claimButton != null) claimButton.interactable = false; // Блокировка кнопки
            TimeSpan timeToWait = TimeSpan.FromHours(24) - difference; // Вычисление оставшегося времени
            if (timerText != null) // Обновление текста таймера
            {
                timerText.text = string.Format("До награды: {0:D2}:{1:D2}:{2:D2}", 
                    timeToWait.Hours, timeToWait.Minutes, timeToWait.Seconds); // Форматирование чч:мм:сс
            }
        }

        UpdateCalendarVisuals(isRewardReady); // Обновление подсветки слотов календаря
    }

    public void ClaimReward() // Метод сбора ежедневной награды игроком
    {
        currentStreak = (currentStreak % 7) + 1; // Цикл 7 дней недели
        totalContinuousDays++; // Увеличение общего непрерывного стажа
        lastClaimTime = DateTime.Now; // Фиксация точного времени получения

        // Начисление базовых наград за день (Золото, Камни, Свитки; 1-2 Кристалла на 7 день недели)
        if (GameManager.Instance != null) // Проверка доступности менеджера игры
        {
            switch (currentStreak) // Награда в зависимости от дня серии
            {
                case 1: GameManager.Instance.AddGold(500); break; // День 1: +500 золота
                case 2: GameManager.Instance.AddGold(1000); break; // День 2: +1000 золота
                case 3: GameManager.Instance.AddResources(1500, 5, 2, 0); break; // День 3: +1500 золота, +5 камней, +2 свитка
                case 4: GameManager.Instance.AddGold(2500); break; // День 4: +2500 золота
                case 5: 
                    GameManager.Instance.AddVipXP(10); // День 5: +10 VIP опыта
                    GameManager.Instance.AddResources(3000, 10, 5, 0); // День 5: +3000 золота, +10 камней, +5 свитков
                    GameManager.Instance.UnlockDarts(); // Разблокировка миниигры в дартс
                    SetStatusText("Вам открыт ДАРТС!"); // Сообщение в UI
                    break;
                case 6: GameManager.Instance.AddResources(5000, 15, 8, 0); break; // День 6: +5000 золота, +15 камней, +8 свитков
                case 7: 
                    // День 7: Недельный сундук (Золото, Камни, Свитки и 2 кристалла)
                    GameManager.Instance.AddResources(10000, 25, 15, 2); // 2 кристалла в конце недели
                    SetStatusText("Вы получили Недельный Сундук (+2 кристалла)!"); // Уведомление
                    break;
            }

            // Проверка крупных вех непрерывного посещения без пропусков
            CheckMilestoneStreakRewards(totalContinuousDays); // Проверка вех 1, 3, 6, 8, 12 месяцев

            // Общий счетчик игровых дней
            GameManager.Instance.daysActive++; // Увеличение общего числа активных дней
            if (GameManager.Instance.daysActive % 10 == 0) // Каждые 10 дней игры
            {
                GameManager.Instance.UnlockMouseCatch(); // Разблокировка ловли мышей
                SetStatusText("Открыта игра: ЛОВЛЯ МЫШЕЙ!"); // Уведомление игрока
            }
        }
        else
        {
            Debug.LogWarning($"[DailyRewardSystem] GameManager.Instance не найден. Имитация начисления за день {currentStreak}."); // Лог
            SetStatusText($"Забрана награда дня {currentStreak} (Тестовый режим)"); // UI статус
        }

        SaveDailyData(); // Сохранение обновленной серии и времени
    }

    private void CheckMilestoneStreakRewards(int continuousDays) // Начисление супер-наград за непрерывные серии месяцев
    {
        string milestoneKey = $"Milestone_Claimed_{continuousDays}"; // Ключ проверки вехи
        if (PlayerPrefs.GetInt(milestoneKey, 0) == 1) return; // Награда уже получена

        if (continuousDays == 30) // 1 месяц непрерывной игры (30 дней)
        {
            GameManager.Instance.AddResources(30000, 50, 30, 15); // +15 Кристаллов, +30000 Золота, +50 Камней, +30 Свитков
            PlayerPrefs.SetInt(milestoneKey, 1); // Сохранение факта выдачи
            SetStatusText("ВЕХА 1 МЕСЯЦ: +15 Кристаллов и ресурсы!"); // UI текст
        }
        else if (continuousDays == 90) // 3 месяца непрерывной игры (90 дней)
        {
            GameManager.Instance.AddResources(100000, 150, 80, 50); // +50 Кристаллов, +100000 Золота, +150 Камней, +80 Свитков
            PlayerPrefs.SetInt(milestoneKey, 1); // Сохранение факта выдачи
            SetStatusText("ВЕХА 3 МЕСЯЦА: +50 Кристаллов и ресурсы!"); // UI текст
        }
        else if (continuousDays == 180) // Полугодичная веха (6 месяцев / 180 дней)
        {
            GameManager.Instance.AddResources(250000, 300, 150, 120); // +120 Кристаллов, +250000 Золота, +300 Камней, +150 Свитков
            PlayerPrefs.SetInt(milestoneKey, 1); // Сохранение факта выдачи
            SetStatusText("ПОЛУГОДИЧНАЯ ВЕХА (6 мес): +120 Кристаллов!"); // UI текст
        }
        else if (continuousDays == 240) // 8 месяцев непрерывной игры (240 дней)
        {
            GameManager.Instance.AddResources(400000, 450, 220, 180); // +180 Кристаллов, +400000 Золота, +450 Камней, +220 Свитков
            PlayerPrefs.SetInt(milestoneKey, 1); // Сохранение факта выдачи
            SetStatusText("ВЕХА 8 МЕСЯЦЕВ: +180 Кристаллов!"); // UI текст
        }
        else if (continuousDays == 365) // 12 месяцев (1 ГОД непрерывной игры)
        {
            GameManager.Instance.AddResources(1000000, 1000, 500, 300); // +300 Кристаллов, +1 000 000 Золота, +1000 Камней, +500 Свитков
            PlayerPrefs.SetInt(milestoneKey, 1); // Сохранение факта выдачи
            SetStatusText("ГОДОВАЯ ВЕХА (12 мес): +300 Кристаллов и 1 000 000 Золота!"); // UI текст
        }
    }

    private void SetTimerText(string text) // Универсальный вывод текста таймера
    {
        if (timerText != null) timerText.text = text; // Обычный UI Text
        if (timerTextTMP != null) timerTextTMP.text = text; // TextMeshPro
    }

    private void SetStatusText(string text) // Универсальный вывод текста статуса
    {
        if (statusText != null) statusText.text = text; // Обычный UI Text
        if (statusTextTMP != null) statusTextTMP.text = text; // TextMeshPro
    }

    private void UpdateCalendarVisuals(bool isRewardReady) // Отрисовка цветовых статусов ячеек календаря
    {
        if (calendarDaySlots == null) return; // Пропуск если слоты не назначены

        for (int i = 0; i < calendarDaySlots.Length; i++) // Проход по всем 7 дням
        {
            if (calendarDaySlots[i] == null) continue; // Защита от пустых ссылок
            
            Image slotImage = calendarDaySlots[i].GetComponent<Image>(); // Получение Image компонента
            if (slotImage == null) continue; // Пропуск при отсутствии

            if (i < currentStreak) // Уже пройденные дни
            {
                slotImage.color = Color.green; // Зеленый - получено
            }
            else if (i == currentStreak && isRewardReady) // Текущий готовый день
            {
                slotImage.color = Color.yellow; // Желтый - готово к получению
            }
            else // Будущие дни
            {
                slotImage.color = Color.gray; // Серый - закрыто
            }
        }
    }

    private void LoadDailyData() // Чтение сохраненных данных ежедневного входа
    {
        currentStreak = PlayerPrefs.GetInt("DailyStreak", 0); // Загрузка номера дня серии
        totalContinuousDays = PlayerPrefs.GetInt("Daily_TotalContinuousDays", 0); // Загрузка непрерывных дней
        string lastClaimStr = PlayerPrefs.GetString("LastDailyClaim", ""); // Загрузка строки времени
        if (!string.IsNullOrEmpty(lastClaimStr)) // Если дата сохранена
        {
            lastClaimTime = DateTime.Parse(lastClaimStr); // Парсинг даты последнего сбора
        }
        else
        {
            lastClaimTime = DateTime.Now.AddDays(-2); // Инициализация 2 дня назад для мгновенной доступности
        }
    }

    private void SaveDailyData() // Запись прогресса ежедневного входа
    {
        PlayerPrefs.SetInt("DailyStreak", currentStreak); // Сохранение дня серии
        PlayerPrefs.SetInt("Daily_TotalContinuousDays", totalContinuousDays); // Сохранение непрерывных дней
        PlayerPrefs.SetString("LastDailyClaim", lastClaimTime.ToString()); // Сохранение времени сбора
        PlayerPrefs.Save(); // Запись на постоянный диск
    }

    [ContextMenu("Сбросить Прогресс Наград (Reset Daily Rewards)")]
    public void ResetDailyRewards() // Сброс системы наград к начальному состоянию
    {
        currentStreak = 0; // Обнуление серии
        totalContinuousDays = 0; // Обнуление непрерывных дней
        lastClaimTime = DateTime.Now.AddDays(-2); // Готовность к новому сбору
        PlayerPrefs.DeleteKey("DailyStreak"); // Удаление ключа серии
        PlayerPrefs.DeleteKey("Daily_TotalContinuousDays"); // Удаление ключа дней
        PlayerPrefs.DeleteKey("LastDailyClaim"); // Удаление даты сбора
        PlayerPrefs.Save(); // Сохранение
        CheckDailyStatus(); // Обновление UI
    }
}
