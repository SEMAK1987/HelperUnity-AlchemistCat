using UnityEngine;
using UnityEngine.UI;
using System;

public class DailyRewardSystem : MonoBehaviour
{
    [Header("UI Ссылки (Назначаются в Инспекторе)")]
    public Button claimButton; // Кнопка "Забрать награду"
    public Text timerText; // Текстовое поле таймера обратного отсчета до следующей награды
    public Text statusText; // Текстовое поле статуса (состояние дня / уведомления)
    [Tooltip("Массив из 7 слотов дней (День 1 - День 7)")]
    public Transform[] calendarDaySlots; // Массив UI-контейнеров для 7 дней недели наград

    private int currentStreak = 0; // Текущая серия непрерывных заходов в игру (дни 1-7)
    private DateTime lastClaimTime; // Время и дата последнего получения награды

    private void Start()
    {
        ValidateInspectorReferences(); // Проверка и автопоиск привязанных UI компонентов
        LoadDailyData(); // Загрузка сохраненного дня и времени последнего захода
        CheckDailyStatus(); // Проверка доступности награды на сегодняшний день
    }

    private void Update()
    {
        CheckDailyStatus(); // Постоянное обновление таймера обратного отсчета в реальном времени
    }

    private void ValidateInspectorReferences() // Проверка и автопоиск привязанных UI компонентов
    {
        // Попытка авто-поиска кнопок и текстов в дочерних объектах, если они не заданы в Инспекторе
        if (claimButton == null) // Если кнопка сбора не задана
        {
            claimButton = GetComponentInChildren<Button>(true); // Поиск кнопки в дочерних объектах
            if (claimButton == null) // Если не найдена
            {
                Button[] buttons = GetComponentsInChildren<Button>(true); // Получение всех кнопок
                foreach (var b in buttons) // Перебор кнопок
                {
                    if (b.name.ToLower().Contains("claim") || b.name.ToLower().Contains("reward") || b.name.ToLower().Contains("button")) // Поиск по ключевым именам
                    {
                        claimButton = b; // Назначение найденной кнопки
                        break; // Выход из цикла
                    }
                }
            }
        }

        if (timerText == null) // Если текст таймера не задан
        {
            Text[] texts = GetComponentsInChildren<Text>(true); // Получение всех текстовых полей
            foreach (var t in texts) // Перебор текстов
            {
                if (t.name.ToLower().Contains("timer") || t.name.ToLower().Contains("time")) // Поиск по имени
                {
                    timerText = t; // Привязка текста
                    break; // Выход
                }
            }
        }

        if (statusText == null) // Если текст статуса не задан
        {
            Text[] texts = GetComponentsInChildren<Text>(true); // Получение всех текстов
            foreach (var t in texts) // Перебор текстов
            {
                if (t.name.ToLower().Contains("status") || t.name.ToLower().Contains("info") || t.name.ToLower().Contains("log")) // Поиск по имени
                {
                    statusText = t; // Привязка текста
                    break; // Выход
                }
            }
        }

        if (calendarDaySlots == null || calendarDaySlots.Length == 0) // Если массив слотов пуст
        {
            // Пытаемся найти дочерние объекты, представляющие собой дни календаря
            System.Collections.Generic.List<Transform> foundSlots = new System.Collections.Generic.List<Transform>(); // Временный список
            foreach (Transform child in transform) // Перебор дочерних трансформаций
            {
                if (child.name.ToLower().Contains("day") || child.name.ToLower().Contains("slot") || child.name.ToLower().Contains("calendar")) // Поиск слотов
                {
                    foundSlots.Add(child); // Добавление найденного слота
                }
            }
            if (foundSlots.Count > 0) // Если слоты найдены
            {
                calendarDaySlots = foundSlots.ToArray(); // Преобразование в массив
            }
        }

        // Выводим только мягкие информативные предупреждения, чтобы не засорять консоль красными ошибками
        if (claimButton == null) // Предупреждение о кнопке
            Debug.Log("[DailyRewardSystem] Мягкое уведомление: Кнопка 'Claim Button' не назначена. Система наград будет работать в фоновом режиме."); // Лог
        if (timerText == null) // Предупреждение о таймере
            Debug.Log("[DailyRewardSystem] Мягкое уведомление: Текстовое поле 'Timer Text' отсутствует. Отсчет времени будет скрыт."); // Лог
        if (statusText == null) // Предупреждение о статусе
            Debug.Log("[DailyRewardSystem] Мягкое уведомление: Текстовое поле 'Status Text' не назначено."); // Лог
        if (calendarDaySlots == null || calendarDaySlots.Length == 0) // Предупреждение о слотах
            Debug.Log("[DailyRewardSystem] Мягкое уведомление: Массив слотов дней 'Calendar Day Slots' пуст."); // Лог
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
            currentStreak = 0; // Сброс серии заходов
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
        currentStreak = (currentStreak % 7) + 1; // Цикл 7 дней
        lastClaimTime = DateTime.Now; // Фиксация точного времени получения

        // Начисление наград
        if (GameManager.Instance != null) // Проверка доступности менеджера игры
        {
            // Начисление золота и кристаллов
            switch (currentStreak) // Награда в зависимости от дня серии
            {
                case 1: GameManager.Instance.AddGold(100); break; // День 1: +100 золота
                case 2: GameManager.Instance.AddGold(250); break; // День 2: +250 золота
                case 3: GameManager.Instance.AddCrystals(1); break; // День 3: +1 кристалл
                case 4: GameManager.Instance.AddGold(500); break; // День 4: +500 золота
                case 5: 
                    GameManager.Instance.AddVipXP(10); // День 5: +10 VIP опыта
                    if (MinigamesManager.Instance != null) // Проверка менеджера игр
                        MinigamesManager.Instance.UnlockDarts(); // Разблокировка миниигры в дартс
                    if (statusText != null) statusText.text = "Вам открыт ДАРТС!"; // Сообщение в UI
                    break;
                case 6: GameManager.Instance.AddGold(1000); break; // День 6: +1000 золота
                case 7: 
                    GameManager.Instance.AddCrystals(10); // День 7: +10 кристаллов
                    if (statusText != null) statusText.text = "Вы получили Золотой Сундук!"; // Сообщение супер-награды
                    break;
            }

            // Дополнительная проверка на активность дней
            GameManager.Instance.daysActive++; // Увеличение общего числа активных дней
            if (GameManager.Instance.daysActive % 10 == 0) // Каждые 10 дней игры
            {
                if (MinigamesManager.Instance != null) // Проверка менеджера игр
                    MinigamesManager.Instance.UnlockMouseCatch(); // Разблокировка ловли мышей
                if (statusText != null) statusText.text = "Открыта игра: ЛОВЛЯ МЫШЕЙ!"; // Уведомление игрока
            }
        }
        else
        {
            // Запасная заглушка, если GameManager отсутствует (для тестов вне основной сцены)
            Debug.LogWarning($"[DailyRewardSystem] GameManager.Instance не найден. Имитация начисления за день {currentStreak}."); // Лог
            if (statusText != null) statusText.text = $"Забрана награда дня {currentStreak} (Тестовый режим)"; // UI статус
        }

        SaveDailyData(); // Сохранение обновленной серии и времени
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
        string lastClaimStr = PlayerPrefs.GetString("LastDailyClaim", ""); // Загрузка строки времени
        if (!string.IsNullOrEmpty(lastClaimStr)) // Если дата сохранена
        {
            lastClaimTime = DateTime.Parse(lastClaimStr); // Парсинг даты последнего сбора
        }
        else
        {
            // По умолчанию даем забрать сразу
            lastClaimTime = DateTime.Now.AddDays(-2); // Инициализация 2 дня назад для мгновенной доступности
        }
    }

    private void SaveDailyData() // Запись прогресса ежедневного входа
    {
        PlayerPrefs.SetInt("DailyStreak", currentStreak); // Сохранение дня серии
        PlayerPrefs.SetString("LastDailyClaim", lastClaimTime.ToString()); // Сохранение времени сбора
        PlayerPrefs.Save(); // Запись на постоянный диск
    }
}
