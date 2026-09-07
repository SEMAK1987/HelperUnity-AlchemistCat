using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core v18.12.15)
/// Менеджер Магического Календаря:
/// - 12 Сезонных рамок месяцев (по 2 в ряд)
/// - Автоматический расчет дней и високосных годов
/// - Подсветка текущего дня
/// - Автоматические бейджи:
///   * Зеленая галочка (Checkmark / Day visited) - день закрыт, награда получена
///   * Красный крестик (Missed_Badge / The day is missed) - день пропущен без входа
/// - Ежедневные, месячные, квартальные и годовые награды
/// </summary>
[ExecuteAlways]
public class Calendar_Manager : MonoBehaviour
{
    public static Calendar_Manager Instance { get; private set; } // Статический синглтон календаря

    [Header("UI Панели и Контейнеры")]
    [SerializeField] private GameObject calendarPanel; // Главная панель календаря
    [SerializeField] private Transform monthsContainer; // Контейнер месяцев (Content у ScrollRect)
    [SerializeField] private GameObject monthPrefab; // Префаб карточки месяца
    [SerializeField] private GameObject dayCellPrefab; // Префаб одной ячейки дня
    [SerializeField] private Button closeButton; // Кнопка закрытия окна календаря

    [Header("12 Спрайтов Месяцев (Янв..Дек)")]
    [SerializeField] private Sprite[] monthSprites = new Sprite[12]; // Массив из 12 сезонных рамок месяцев

    [Header("Иконка Пропущенного Дня (Разбитая Колба)")]
    [SerializeField] private Sprite missedFlaskSprite; // Спрайт разбитой колбы для пропущенных дней

    [Header("Иконки Наград")]
    [SerializeField] private Sprite goldIcon; // Иконка золотой монеты
    [SerializeField] private Sprite stoneIcon; // Иконка алхимического камня
    [SerializeField] private Sprite scrollIcon; // Иконка свитка знаний
    [SerializeField] private Sprite crystalIcon; // Иконка кристалла

    [System.Serializable]
    public class MonthLayoutConfig
    {
        public string monthName = "Month"; // Название месяца
        public Vector2 cardSize = new Vector2(400f, 540f); // Размер карточки месяца
        public Vector2 cellSize = new Vector2(34f, 34f); // Размер ячейки дня
        public Vector2 spacing = new Vector2(5f, 5f); // Отступы между днями
        public int padLeft = 35; // Отступ слева
        public int padRight = 35; // Отступ справа
        public int padTop = 95; // Отступ сверху
        public int padBottom = 30; // Отступ снизу
    }

    [Header("Индивидуальная калибровка сеток для каждого месяца")]
    public MonthLayoutConfig[] customMonthLayouts = new MonthLayoutConfig[12]; // Настройки верстки для 12 месяцев

    [Header("Всплывающее окно награды")]
    [SerializeField] private GameObject rewardPopup; // Всплывающее окно полученной награды
    [SerializeField] private TextMeshProUGUI rewardPopupText; // Текст описания полученной награды
    [SerializeField] private Button rewardPopupCloseBtn; // Кнопка закрытия всплывающего окна

    // Текущая системная дата
    private int currentYear; // Текущий год устройства
    private int currentMonth; // Текущий месяц устройства
    private int currentDay; // Текущий день устройства

    private readonly string[] monthNamesRu = {
        "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
        "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
    };

    private void Reset() // Сброс компонента в редакторе Unity
    {
        ResetLayoutsToDefaults(); // Сброс верстки на заводские параметры
    }

    private void Awake() // Инициализация синглтона и загрузка настроек
    {
        Instance = this; // Установка статического синглтона

        // Если в инспекторе уже были заданы валидные калибровки пользователем, не перезаписываем их дефолтами
        bool hasValidInspectorLayouts = (customMonthLayouts != null && customMonthLayouts.Length == 12 && customMonthLayouts[0] != null && customMonthLayouts[0].cellSize.x > 0); // Проверка валидности настроек

        if (!hasValidInspectorLayouts) // Если настроек в инспекторе нет
        {
            // Если в PlayerPrefs есть сохраненные настройки верстки, подгружаем их
            string savedJson = PlayerPrefs.GetString("FateContinent_Calendar_Layouts", ""); // Чтение JSON из памяти
            if (!string.IsNullOrEmpty(savedJson)) // Если строка не пустая
            {
                try // Попытка десериализации
                {
                    MonthLayoutDataWrapper wrapper = JsonUtility.FromJson<MonthLayoutDataWrapper>(savedJson); // Парсинг JSON
                    if (wrapper != null && wrapper.layouts != null && wrapper.layouts.Length == 12) // Проверка целостности
                    {
                        customMonthLayouts = wrapper.layouts; // Применение сохраненных калибровок
                        hasValidInspectorLayouts = true; // Флаг успешной загрузки
                    }
                }
                catch {} // Игнорирование ошибок парсинга
            }
        }

        if (!hasValidInspectorLayouts) // Если ничего не найдено
        {
            ResetLayoutsToDefaults(); // Установка стандартных калибровок
        }

        if (calendarPanel == null) // Если панель не назначена
            calendarPanel = this.gameObject; // Назначение текущего объекта

        if (closeButton != null) // Если кнопка закрытия назначена
        {
            closeButton.onClick.RemoveAllListeners(); // Очистка старых подписчиков
            closeButton.onClick.AddListener(CloseCalendar); // Подписка на закрытие
        }

        UpdateCurrentDate(); // Определение текущей даты
        string todayKey = $"Cal_Claimed_{currentYear}_{currentMonth}_{currentDay}"; // Ключ сбора за сегодня
        bool isTodayClaimed = PlayerPrefs.GetInt(todayKey, 0) == 1; // Проверка сбора награды

        if (closeButton != null) // Настройка кнопки закрытия
        {
            closeButton.interactable = isTodayClaimed; // Активна только после сбора награды
            closeButton.gameObject.SetActive(isTodayClaimed); // Видима только после сбора
        }

        if (rewardPopupCloseBtn != null) // Кнопка закрытия всплывающего окна
        {
            rewardPopupCloseBtn.onClick.RemoveAllListeners(); // Очистка слушателей
            rewardPopupCloseBtn.onClick.AddListener(() => { // Анонимный обработчик клика
                if (rewardPopup != null) rewardPopup.SetActive(false); // Закрытие всплывающего окна
            });
        }
    }

    [ContextMenu("Сбросить Прогресс Календаря (Reset Calendar Progress)")]
    public void ResetCalendarProgress() // Полный сброс прогресса посещений календаря
    {
        UpdateCurrentDate(); // Обновление даты
        PlayerPrefs.DeleteKey($"Cal_Claimed_{currentYear}_{currentMonth}_{currentDay}"); // Удаление ключа за сегодня
        PlayerPrefs.DeleteKey("Tutorial_Calendar_Claim_Done"); // Удаление ключа туториала
        for (int m = 1; m <= 12; m++) // Цикл по месяцам
        {
            for (int d = 1; d <= 31; d++) // Цикл по дням
            {
                PlayerPrefs.DeleteKey($"Cal_Claimed_{currentYear}_{m}_{d}"); // Удаление ключа дня
            }
        }
        PlayerPrefs.Save(); // Запись на диск
        RefreshAllDaysUI(); // Перерисовка визуала ячеек
        if (closeButton != null) // Блокировка кнопки закрытия
        {
            closeButton.interactable = false; // Деактивация
            closeButton.gameObject.SetActive(false); // Скрытие
        }
        Debug.Log("[Calendar] Прогресс календаря успешно сброшен!"); // Лог в консоль
    }

    private void Start() // Стартовая генерация сетки календаря
    {
        UpdateCurrentDate(); // Получение текущей даты
        GenerateFullCalendar(); // Построение 12 месяцев
    }

    public void OpenCalendar() // Открытие окна календаря
    {
        if (calendarPanel != null) // Если панель задана
        {
            calendarPanel.SetActive(true); // Включение панели
        }
        else // Если нет
        {
            gameObject.SetActive(true); // Включение объекта
        }

        UpdateCurrentDate(); // Обновление даты
        string todayKey = $"Cal_Claimed_{currentYear}_{currentMonth}_{currentDay}"; // Ключ за сегодня
        bool isTodayClaimed = PlayerPrefs.GetInt(todayKey, 0) == 1; // Проверка статуса сбора
        if (closeButton != null) // Настройка кнопки
        {
            closeButton.interactable = isTodayClaimed; // Активность
            closeButton.gameObject.SetActive(isTodayClaimed); // Видимость
        }

        // Всегда синхронизируем и применяем свежие координаты ячеек при открытии
        if (monthsContainer != null && monthsContainer.childCount == 0) // Если месяцы еще не созданы
        {
            GenerateFullCalendar(); // Построить календарь
        }
        else // Если уже созданы
        {
            ApplyAllLayoutsInRealtime(); // Применить калибровку верстки
            RefreshAllDaysUI(); // Обновить статус ячеек
        }
    }

    private Coroutine popupAutoHideCoroutine; // Корутина таймера автоскрытия попапа

    public void CloseCalendar() // Закрытие календаря с проверкой обязательной отметки дня
    {
        UpdateCurrentDate(); // Обновление даты
        string todayKey = $"Cal_Claimed_{currentYear}_{currentMonth}_{currentDay}"; // Ключ за сегодня
        bool isTodayClaimed = PlayerPrefs.GetInt(todayKey, 0) == 1; // Забрана ли награда за сегодня

        // Если сегодняшний день еще не отмечен — строго блокируем выход и показываем подсказку
        if (!isTodayClaimed) // Если не забрал
        {
            string curLang = PlayerPrefs.GetString("Selected_Language", "RU"); // Текущий язык
            string title = curLang == "EN" ? "Attendance Required" : (curLang == "TR" ? "Giriş Damgası Gerekli" : "Отметьте день"); // Заголовок
            string msg = curLang == "EN" ? "Meow! Please stamp today's date in the calendar first to claim your reward!" : // Текст EN
                         (curLang == "TR" ? "Miyav! Ödülünüzü almak için lütfen önce takvimde bugünkü tarihi işaretleyin!" : // Текст TR
                         "Мяу! Сначала отметьте сегодняшний день в календаре (нажав на сияющее число), чтобы получить награду!"); // Текст RU

            ShowPopup(title, msg, 2.5f); // Показ предупреждения на 2.5 секунды
            return; // Запрет закрытия
        }

        // Если день отмечен или туториал уже пройден — закрываем календарь
        PlayerPrefs.SetInt("Tutorial_Calendar_Claim_Done", 1); // Пометка прохождения туториала
        PlayerPrefs.Save(); // Сохранение в PlayerPrefs

        if (calendarPanel != null) // Если панель есть
            calendarPanel.SetActive(false); // Закрываем панель
        else // Если нет
            gameObject.SetActive(false); // Выключаем объект

        // Уведомляем диалоговую систему о закрытии календаря (старт фазы Опыта, Уровня и Аватарок)
        if (DialogueSystem_Manager.Instance != null) // Если диалоговый менеджер доступен
        {
            DialogueSystem_Manager.Instance.OnCalendarClosed(); // Запуск следующего диалога
        }
    }

    private void UpdateCurrentDate() // Считывание текущего системного времени
    {
        DateTime now = DateTime.Now; // Системная дата
        currentYear = now.Year; // Год
        currentMonth = now.Month; // Месяц (1..12)
        currentDay = now.Day;     // День (1..31)
    }

    [Header("Принудительно использовать калибровочные настройки из кода")]
    [Tooltip("Если выключено (false), календарь использует ваши настройки из инспектора в реальном времени!")]
    public bool useCodeDefaultPaddings = false;

    // Таблица параметров калибровки для 12 сезонных рамок:
    // [padLeft, padRight, padTop, padBottom, cardWidth, cardHeight, cellWidth, cellHeight, spacingX, spacingY]
    private static readonly int[,] DefaultPaddings = new int[,]
    {
        { 110, 110, 44, 75, 600, 680, 27, 44, 4, 13 },  // 1. Январь (Цифры подняты выше: padTop 44 - не касаются нижней планки рамки 29..31)
        { 40, 40, 44, 30, 455, 540, 27, 44, 4, 15 },   // 2. Февраль (Цифры на высоте Января: padTop 44, рамка 455x540)
        { 75, 75, 38, 70, 525, 675, 27, 44, 4, 15 },   // 3. Март (Цифры чуть подняты вверх: padTop 38 - не касаются низа рамки 29..31)
        { 38, 38, 35, 28, 460, 540, 27, 44, 4, 15 },   // 4. Апрель (Эталон)
        { 60, 60, 35, 35, 460, 540, 27, 44, 4, 15 },   // 5. Май (Эталон)
        { 40, 40, 35, 30, 460, 540, 27, 44, 4, 15 },   // 6. Июнь (Эталон)
        { 40, 40, 35, 30, 460, 540, 27, 44, 4, 15 },   // 7. Июль (Эталон: padTop 35)
        { 85, 85, 35, 75, 575, 685, 27, 44, 4, 15 },   // 8. Август (Цифры подняты на точный уровень Июля: padTop 35, рамка не тронута)
        { 40, 40, 30, 30, 460, 540, 27, 44, 4, 15 },   // 9. Сентябрь (Эталон: padTop 30)
        { 90, 90, 30, 75, 585, 685, 27, 44, 4, 15 },   // 10. Октябрь (Цифры подняты до уровня Сентября: padTop 30, рамка не тронута)
        { 60, 60, 35, 35, 460, 540, 27, 44, 4, 15 },   // 11. Ноябрь (Эталон)
        { 60, 60, 35, 35, 460, 540, 27, 44, 4, 15 }    // 12. Декабрь (Высота Ноября: padTop 35, ячейки 27x44)
    };

    [Header("Кнопки управления калибровкой (Включайте галочку для действия в Инспекторе)")]
    [Tooltip("Поставьте галочку, чтобы скопировать все координаты месяцев в буфер обмена")]
    public bool btn_CopyAllLayouts = false;
    [Tooltip("Поставьте галочку, чтобы вставить координаты месяцев из буфера обмена")]
    public bool btn_PasteAllLayouts = false;
    [Tooltip("Поставьте галочку, чтобы сохранить текущие координаты в PlayerPrefs на устройстве")]
    public bool btn_SaveLayoutsToPrefs = false;
    [Tooltip("Поставьте галочку, чтобы загрузить сохраненные координаты из PlayerPrefs")]
    public bool btn_LoadLayoutsFromPrefs = false;
    [Tooltip("Поставьте галочку, чтобы сбросить все месяцы на стандартные значения")]
    public bool btn_ResetToDefaults = false;

    [ContextMenu("Сбросить калибровку на значения по умолчанию")]
    public void ResetLayoutsToDefaults() // Сброс верстки всех 12 месяцев на стандартные значения
    {
        customMonthLayouts = new MonthLayoutConfig[12]; // Создание массива на 12 конфигураций
        for (int i = 0; i < 12; i++) // Проход по всем 12 месяцам
        {
            customMonthLayouts[i] = new MonthLayoutConfig // Создание конфига для месяца
            {
                monthName = (monthNamesRu != null && i < monthNamesRu.Length) ? monthNamesRu[i] : $"Месяц {i + 1}", // Имя месяца
                padLeft = DefaultPaddings[i, 0], // Отступ слева
                padRight = DefaultPaddings[i, 1], // Отступ справа
                padTop = DefaultPaddings[i, 2], // Отступ сверху
                padBottom = DefaultPaddings[i, 3], // Отступ снизу
                cardSize = new Vector2(DefaultPaddings[i, 4], DefaultPaddings[i, 5]), // Размеры рамки месяца
                cellSize = new Vector2(DefaultPaddings[i, 6], DefaultPaddings[i, 7]), // Размеры ячейки дня
                spacing = new Vector2(DefaultPaddings[i, 8], DefaultPaddings[i, 9]) // Отступы между ячейками
            };
        }
        ApplyAllLayoutsInRealtime(); // Немедленное применение изменений
    }

    [ContextMenu("Скопировать все координаты месяцев (В Буфер JSON)")]
    public void CopyAllLayoutsToClipboard() // Копирование параметров верстки в буфер обмена в JSON
    {
        if (customMonthLayouts == null || customMonthLayouts.Length == 0) // Если массив пуст
        {
            ResetLayoutsToDefaults(); // Восстанавливаем дефолты
        }

        MonthLayoutDataWrapper wrapper = new MonthLayoutDataWrapper { layouts = customMonthLayouts }; // Обертка данных
        string json = JsonUtility.ToJson(wrapper, true); // Сериализация в читаемый JSON
        GUIUtility.systemCopyBuffer = json; // Запись в буфер обмена
        Debug.Log("<color=#80FFDB><b>[CALENDAR] Все координаты 12 месяцев скопированы в буфер обмена!</b></color>"); // Лог успеха
    }

    [ContextMenu("Вставить координаты месяцев (Из Буфера JSON)")]
    public void PasteAllLayoutsFromClipboard() // Вставка параметров верстки из буфера обмена
    {
        string json = GUIUtility.systemCopyBuffer; // Чтение из буфера
        if (string.IsNullOrEmpty(json)) // Если буфер пуст
        {
            Debug.LogWarning("[CALENDAR] Буфер обмена пуст!"); // Предупреждение
            return; // Выход
        }

        try // Попытка парсинга
        {
            MonthLayoutDataWrapper wrapper = JsonUtility.FromJson<MonthLayoutDataWrapper>(json); // Десериализация
            if (wrapper != null && wrapper.layouts != null && wrapper.layouts.Length == 12) // Проверка массива
            {
                customMonthLayouts = wrapper.layouts; // Применение конфигов
                ApplyAllLayoutsInRealtime(); // Немедленное обновление
                Debug.Log("<color=#FFE57F><b>[CALENDAR] Координаты месяцев успешно вставлены и применены!</b></color>"); // Лог успеха
            }
        }
        catch (Exception ex) // Обработка исключения
        {
            Debug.LogError($"[CALENDAR] Ошибка разбора JSON из буфера: {ex.Message}"); // Вывод ошибки
        }
    }

    [ContextMenu("Сохранить координаты в PlayerPrefs")]
    public void SaveLayoutsToPrefs() // Сохранение калибровок верстки в память устройства
    {
        if (customMonthLayouts == null || customMonthLayouts.Length == 0) return; // Проверка наличия данных
        MonthLayoutDataWrapper wrapper = new MonthLayoutDataWrapper { layouts = customMonthLayouts }; // Формирование обертки
        string json = JsonUtility.ToJson(wrapper); // Сериализация
        PlayerPrefs.SetString("FateContinent_Calendar_Layouts", json); // Сохранение по ключу
        PlayerPrefs.Save(); // Запись
        Debug.Log("<color=#80FFDB><b>[CALENDAR] Координаты успешно сохранены в постоянную память игры!</b></color>"); // Лог в консоль
    }

    [ContextMenu("Загрузить координаты из PlayerPrefs")]
    public void LoadLayoutsFromPrefs() // Загрузка сохраненных калибровок из памяти устройства
    {
        string json = PlayerPrefs.GetString("FateContinent_Calendar_Layouts", ""); // Чтение строки JSON
        if (string.IsNullOrEmpty(json)) // Если данных нет
        {
            Debug.LogWarning("[CALENDAR] Нет сохраненных координат в PlayerPrefs."); // Предупреждение
            return; // Выход
        }

        try // Попытка десериализации
        {
            MonthLayoutDataWrapper wrapper = JsonUtility.FromJson<MonthLayoutDataWrapper>(json); // Разбор JSON
            if (wrapper != null && wrapper.layouts != null && wrapper.layouts.Length == 12) // Проверка валидности
            {
                customMonthLayouts = wrapper.layouts; // Загрузка
                ApplyAllLayoutsInRealtime(); // Применение
                Debug.Log("<color=#FFE57F><b>[CALENDAR] Координаты успешно загружены из PlayerPrefs и применены!</b></color>"); // Лог
            }
        }
        catch (Exception ex) // Ошибка при разборе
        {
            Debug.LogError($"[CALENDAR] Ошибка загрузки координат: {ex.Message}"); // Лог ошибки
        }
    }

    [System.Serializable]
    private class MonthLayoutDataWrapper // Обертка массива для поддержки JsonUtility
    {
        public MonthLayoutConfig[] layouts; // Массив из 12 конфигураций
    }

    private bool _needsLayoutUpdate = false; // Флаг необходимости обновления верстки

    private void OnValidate() // Вызывается редактором Unity при изменении свойств в Инспекторе
    {
        // Кнопки-триггеры в Инспекторе
        if (btn_CopyAllLayouts) // Нажатие копирования
        {
            btn_CopyAllLayouts = false; // Сброс галочки
            CopyAllLayoutsToClipboard(); // Вызов копирования
        }
        if (btn_PasteAllLayouts) // Нажатие вставки
        {
            btn_PasteAllLayouts = false; // Сброс галочки
            PasteAllLayoutsFromClipboard(); // Вызов вставки
        }
        if (btn_SaveLayoutsToPrefs) // Нажатие сохранения
        {
            btn_SaveLayoutsToPrefs = false; // Сброс галочки
            SaveLayoutsToPrefs(); // Вызов сохранения
        }
        if (btn_LoadLayoutsFromPrefs) // Нажатие загрузки
        {
            btn_LoadLayoutsFromPrefs = false; // Сброс галочки
            LoadLayoutsFromPrefs(); // Вызов загрузки
        }
        if (btn_ResetToDefaults) // Нажатие сброса
        {
            btn_ResetToDefaults = false; // Сброс галочки
            ResetLayoutsToDefaults(); // Вызов сброса
        }

        // Помечаем флаг для безопасного обновления верстки в кадре LateUpdate без исключений SendMessage!
        _needsLayoutUpdate = true; // Установка флага
    }

    private void Update() // Покадровое обновление для мгновенной калибровки в Play Mode
    {
        // Если в Play Mode вы меняете координаты в Инспекторе — они мгновенно применяются к сетке!
        if (Application.isPlaying && calendarPanel != null && calendarPanel.activeInHierarchy) // Если игра запущена и календарь активен
        {
            ApplyAllLayoutsInRealtime(); // Применяем калибровку
        }
    }

    private void LateUpdate() // Финальное обновление кадра для безопасного пересчета верстки
    {
        if (_needsLayoutUpdate) // Если флаг установлен
        {
            _needsLayoutUpdate = false; // Сброс флага
            ApplyAllLayoutsInRealtime(); // Применение верстки
        }
    }

    /// <summary>
    /// Применяет измененные размеры карточек, отступы и сетки ячеек ко всем 12 месяцам в реальном времени
    /// </summary>
    public void ApplyAllLayoutsInRealtime() // Синхронизация верстки карточек и сеток всех месяцев
    {
        if (monthsContainer == null || monthsContainer.childCount == 0) return; // Проверка наличия контейнера

        // 1. Проверяем все дочерние объекты в контейнере
        int childCount = monthsContainer.childCount; // Число карточек месяцев
        for (int i = 0; i < childCount; i++) // Проход по всем карточкам
        {
            Transform monthTransform = monthsContainer.GetChild(i); // Получение карточки месяца
            if (monthTransform == null) continue; // Пропуск пустых

            // Определяем порядковый номер месяца (1..12) из имени объекта или по индексу
            int monthNum = i + 1; // Номер месяца по умолчанию
            string objName = monthTransform.name; // Имя объекта
            if (objName.StartsWith("Month_") && objName.Length >= 8) // Проверка шаблона имени
            {
                int parsed; // Переменная парсинга
                if (int.TryParse(objName.Substring(6, 2), out parsed)) // Парсинг номера
                {
                    monthNum = parsed; // Присвоение спарсенного номера
                }
            }

            MonthLayoutConfig cfg = GetLayoutConfigForMonth(monthNum); // Получение конфигурации для месяца
            if (cfg == null) continue; // Пропуск если нет конфигурации

            // Настройка размера карточки месяца
            RectTransform cardRect = monthTransform.GetComponent<RectTransform>(); // Компонент RectTransform карточки
            if (cardRect != null) // Если найден
            {
                cardRect.sizeDelta = cfg.cardSize; // Установка размера карточки
            }

            // Находим сетку чисел внутри месяца
            Transform daysGrid = monthTransform.Find("Days_Grid"); // Поиск сетки чисел
            if (daysGrid == null) daysGrid = monthTransform; // Фолбэк на сам объект

            GridLayoutGroup gridGroup = daysGrid.GetComponent<GridLayoutGroup>(); // Получение компонента сетки
            if (gridGroup != null) // Если сетка существует
            {
                gridGroup.cellSize = cfg.cellSize; // Размер ячейки дня
                gridGroup.spacing = cfg.spacing; // Отступы между ячейками
                gridGroup.padding = new RectOffset(cfg.padLeft, cfg.padRight, cfg.padTop, cfg.padBottom); // Отступы от краев рамки
                gridGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // Фиксация числа колонок
                gridGroup.constraintCount = 7; // 7 дней недели
                gridGroup.childAlignment = TextAnchor.UpperCenter; // Выравнивание по центру сверху

                // Немедленно обновляем внутреннюю сетку чисел
                RectTransform gridRect = daysGrid.GetComponent<RectTransform>(); // RectTransform сетки
                if (gridRect != null) // Если найден
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect); // Принудительный пересчет верстки
                }
            }

            if (cardRect != null) // Если карточка найдена
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect); // Принудительный пересчет карточки
            }
        }

        // Обновляем родительский контейнер Scroll Content
        RectTransform containerRect = monthsContainer.GetComponent<RectTransform>(); // RectTransform контейнера
        if (containerRect != null) // Если найден
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect); // Принудительный пересчет контейнера
        }
    }

    public MonthLayoutConfig GetLayoutConfigForMonth(int monthIndex1Based) // Получение актуальной конфигурации месяца
    {
        int idx = Mathf.Clamp(monthIndex1Based - 1, 0, 11); // Индекс от 0 до 11
        if (!useCodeDefaultPaddings && customMonthLayouts != null && idx < customMonthLayouts.Length && customMonthLayouts[idx] != null && customMonthLayouts[idx].cellSize.x > 0) // Если заданы пользовательские настройки
        {
            return customMonthLayouts[idx]; // Возврат пользовательской калибровки
        }

        MonthLayoutConfig cfg = new MonthLayoutConfig // Создание стандартного конфига
        {
            monthName = monthNamesRu[idx], // Название месяца
            padLeft = DefaultPaddings[idx, 0], // Отступ слева
            padRight = DefaultPaddings[idx, 1], // Отступ справа
            padTop = DefaultPaddings[idx, 2], // Отступ сверху
            padBottom = DefaultPaddings[idx, 3], // Отступ снизу
            cardSize = new Vector2(DefaultPaddings[idx, 4], DefaultPaddings[idx, 5]), // Размер карточки
            cellSize = new Vector2(DefaultPaddings[idx, 6], DefaultPaddings[idx, 7]), // Размер ячейки
            spacing = new Vector2(DefaultPaddings[idx, 8], DefaultPaddings[idx, 9]) // Отступы
        };

        return cfg; // Возврат конфига
    }

    // Генерация 12 месяцев
    public void GenerateFullCalendar() // Создание всех 12 карточек месяцев и ячеек дней
    {
        if (monthsContainer == null || monthPrefab == null) return; // Проверка префаба и контейнера

        // Очищаем старые объекты
        foreach (Transform child in monthsContainer) // Проход по старым дочерним элементам
        {
            Destroy(child.gameObject); // Удаление старого объекта
        }

        for (int m = 1; m <= 12; m++) // Цикл по 12 месяцам
        {
            GameObject monthObj = Instantiate(monthPrefab, monthsContainer); // Создание карточки месяца
            monthObj.name = $"Month_{m:00}_{monthNamesRu[m - 1]}"; // Формирование имени объекта

            MonthLayoutConfig cfg = GetLayoutConfigForMonth(m); // Получение параметров верстки

            // Настройка размера карточки месяца
            RectTransform cardRect = monthObj.GetComponent<RectTransform>(); // Получение RectTransform карточки
            if (cardRect != null) // Если найден
            {
                cardRect.sizeDelta = cfg.cardSize; // Применение размеров карточки
            }

            // Установка спрайта рамки месяца
            Image frameImg = monthObj.GetComponent<Image>(); // Компонент фоновой рамки
            if (frameImg != null && monthSprites != null && (m - 1) < monthSprites.Length) // Проверка наличия спрайта
            {
                frameImg.sprite = monthSprites[m - 1]; // Назначение сезонного спрайта рамки
            }

            // Контейнер для ячеек дней внутри месяца
            Transform daysGrid = monthObj.transform.Find("Days_Grid"); // Поиск сетки дней
            if (daysGrid == null) daysGrid = monthObj.transform; // Фолбэк на сам объект

            // Настройка сетки GridLayoutGroup под пропорции конкретной рамки
            GridLayoutGroup gridGroup = daysGrid.GetComponent<GridLayoutGroup>(); // Компонент GridLayoutGroup
            if (gridGroup != null) // Если компонент есть
            {
                gridGroup.cellSize = cfg.cellSize; // Размер ячейки дня
                gridGroup.spacing = cfg.spacing; // Отступы между ячейками
                gridGroup.padding = new RectOffset(cfg.padLeft, cfg.padRight, cfg.padTop, cfg.padBottom); // Внутренние отступы
                gridGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // Фиксация 7 колонок
                gridGroup.constraintCount = 7; // Число дней в неделе
                gridGroup.childAlignment = TextAnchor.UpperCenter; // Выравнивание
            }

            int daysInMonth = DateTime.DaysInMonth(currentYear, m); // Расчет числа дней в месяце (с учетом високосного года)

            for (int d = 1; d <= daysInMonth; d++) // Цикл по дням месяца
            {
                CreateDayCell(daysGrid, m, d); // Создание ячейки конкретного дня
            }
        }
    }

    private void CreateDayCell(Transform parent, int month, int day) // Создание отдельной ячейки дня
    {
        if (dayCellPrefab == null) return; // Проверка префаба ячейки

        GameObject cellObj = Instantiate(dayCellPrefab, parent); // Создание объекта дня
        cellObj.name = $"Day_{day}"; // Присвоение имени

        TextMeshProUGUI dayText = cellObj.GetComponentInChildren<TextMeshProUGUI>(); // Поиск текста числа
        if (dayText != null) // Если найден
        {
            dayText.text = day.ToString(); // Установка номера дня
        }

        Image rewardImg = cellObj.transform.Find("Reward_Icon")?.GetComponent<Image>(); // Поиск иконки награды
        if (rewardImg != null) // Если найден
        {
            rewardImg.sprite = GetRewardSpriteForDay(month, day); // Установка спрайта награды
        }

        Button cellBtn = cellObj.GetComponent<Button>(); // Получение кнопки ячейки
        if (cellBtn != null) // Если кнопка есть
        {
            cellBtn.onClick.AddListener(() => OnDayClicked(month, day, cellObj)); // Подписка на клик по дню
        }

        UpdateDayCellVisual(cellObj, month, day); // Обновление визуала статуса дня
    }

    private Sprite GetRewardSpriteForDay(int month, int day) // Получение спрайта награды для дня
    {
        // В ежедневном календаре даются ТОЛЬКО ресурсы: Золото, Камни, Свитки (Кристаллы выдаются за полный месяц и год)
        // 5, 10, 15, 20, 25, 30-й день -> Древние Свитки
        if (day % 5 == 0) return scrollIcon != null ? scrollIcon : goldIcon; // Свитки знаний
        // 3, 6, 9, 12, 18, 21, 24, 27-й день -> Алхимические Камни
        if (day % 3 == 0) return stoneIcon != null ? stoneIcon : goldIcon; // Камни улучшения
        // Все остальные дни -> Золотые Монеты
        return goldIcon; // Золотые монеты
    }

    private void OnDayClicked(int month, int day, GameObject cellObj) // Обработка клика по ячейке дня
    {
        string saveKey = $"Cal_Claimed_{currentYear}_{month}_{day}"; // Ключ сбора награды
        UpdateCurrentDate(); // Актуализация системной даты

        // Сегодняшний активный день
        if (month == currentMonth && day == currentDay) // Если кликнули на сегодняшний день
        {
            if (PlayerPrefs.GetInt(saveKey, 0) == 1) // Если уже забрали ранее
            {
                ShowPopup("Награда уже получена", "Вы уже забрали награду за сегодняшний день. Возвращайтесь завтра за новым подарком!"); // Показ подсказки
                return; // Выход
            }

            // Забираем награду
            PlayerPrefs.SetInt(saveKey, 1); // Запись факта сбора
            PlayerPrefs.SetInt("Tutorial_Calendar_Claim_Done", 1); // Пометка выполнения туториала
            PlayerPrefs.Save(); // Сохранение изменений

            if (closeButton != null) // Разблокировка кнопки выхода
            {
                closeButton.interactable = true; // Делаем активной
                closeButton.gameObject.SetActive(true); // Делаем видимой
            }

            string rewardDesc = ClaimReward(month, day); // Начисление ресурсов и получение описания
            ShowPopup("Награда получена!", $"Поздравляем! Вы получили награду за {day} {monthNamesRu[month - 1]}:\n\n<b>{rewardDesc}</b>"); // Показ поздравления

            UpdateDayCellVisual(cellObj, month, day); // Обновление визуала ячейки

            // Проверка закрытия полного месяца и полного года для начисления Кристаллов
            CheckAndClaimMonthCompletionReward(month); // Проверка ежемесячной награды
            CheckAndClaimYearCompletionReward(); // Проверка годовой награды
        }
        else if (month < currentMonth || (month == currentMonth && day < currentDay)) // Если кликнули по прошедшему дню
        {
            // Прошедшие дни
            if (PlayerPrefs.GetInt(saveKey, 0) == 1) // Если день был закрыт
            {
                ShowPopup("День закрыт", $"Награда за {day} {monthNamesRu[month - 1]} уже была успешно получена."); // Показ информации
            }
            else // Если день был пропущен
            {
                ShowPopup("День пропущен", $"Этот день ({day} {monthNamesRu[month - 1]}) был пропущен. Заходите в игру каждый день, чтобы не терять награды!"); // Показ предупреждения
            }
        }
        else // Если будущий день
        {
            // Будущие дни
            ShowPopup("Будущий день", $"Этот день еще не наступил. Приходите {day} {monthNamesRu[month - 1]}, чтобы открыть подарок!"); // Показ подсказки
        }
    }

    private string ClaimReward(int month, int day) // Логика начисления наград за посещение дня
    {
        // В ежедневном календаре начисляются ТОЛЬКО ресурсы: Золото, Камни и Свитки
        int gold = 1000 + (day * 100); // Базовое золото за день
        int stones = 0; // Инициализация камней
        int scrolls = 0; // Инициализация свитков

        if (day % 5 == 0) // Каждый 5-й день
        {
            scrolls = 2 + (day >= 25 ? 1 : 0); // +2..3 Свитка
        }
        else if (day % 3 == 0) // Каждый 3-й день
        {
            stones = 3 + (day >= 21 ? 2 : 0); // +3..5 Камней
        }
        else if (day % 7 == 0) // Каждое воскресенье
        {
            gold += 2000; // Бонусное золото
            stones += 2; // Бонусные камни
        }

        int currentGold = PlayerPrefs.GetInt("Player_Gold", 5000); // Текущее золото
        int currentStones = PlayerPrefs.GetInt("Player_Stones", 10); // Текущие камни
        int currentScrolls = PlayerPrefs.GetInt("Player_Scrolls", 3); // Текущие свитки

        PlayerPrefs.SetInt("Player_Gold", currentGold + gold); // Сохранение золота
        PlayerPrefs.SetInt("Player_Stones", currentStones + stones); // Сохранение камней
        PlayerPrefs.SetInt("Player_Scrolls", currentScrolls + scrolls); // Сохранение свитков
        PlayerPrefs.Save(); // Фиксация на диске

        // Мгновенная синхронизация цифр в верхней панели (TopPanel)
        if (DialogueSystem_Manager.Instance != null) // Если диалоговый менеджер доступен
        {
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources(); // Синхронизация UI
        }

        if (GameManager.Instance != null) // Если GameManager доступен
        {
            GameManager.Instance.AddResources(gold, stones, scrolls, 0); // Начисление в GameManager
        }

        string res = $"+{gold} Золота"; // Формирование строки золота
        if (stones > 0) res += $", +{stones} Камней"; // Добавление камней
        if (scrolls > 0) res += $", +{scrolls} Свитков"; // Добавление свитков
        return res; // Возврат описания
    }

    /// <summary>
    /// Проверка и выдача супер-награды за полное закрытие всех дней месяца (включает Кристаллы)
    /// </summary>
    public void CheckAndClaimMonthCompletionReward(int month) // Награда за полностью закрытый месяц
    {
        string monthRewardKey = $"Cal_MonthCompleted_{currentYear}_{month}"; // Ключ ежемесячной награды
        if (PlayerPrefs.GetInt(monthRewardKey, 0) == 1) return; // Уже получена ранее

        int daysInMonth = DateTime.DaysInMonth(currentYear, month); // Число дней в месяце
        for (int d = 1; d <= daysInMonth; d++) // Проверка каждого дня
        {
            string dayKey = $"Cal_Claimed_{currentYear}_{month}_{d}"; // Ключ дня
            if (PlayerPrefs.GetInt(dayKey, 0) != 1) return; // Если хоть один день не закрыт - выходим
        }

        // Все дни месяца закрыты! Начисляем Ежемесячную Награду (Кристаллы, Золото, Камни, Свитки)
        int bonusCrystals = 50; // +50 Кристаллов за месяц
        int bonusGold = 50000; // +50 000 Золота
        int bonusStones = 25; // +25 Камней
        int bonusScrolls = 15; // +15 Свитков

        PlayerPrefs.SetInt(monthRewardKey, 1); // Фиксация получения награды месяца

        int curC = PlayerPrefs.GetInt("Player_Crystals", 0); // Чтение кристаллов
        int curG = PlayerPrefs.GetInt("Player_Gold", 5000); // Чтение золота
        int curS = PlayerPrefs.GetInt("Player_Stones", 10); // Чтение камней
        int curSc = PlayerPrefs.GetInt("Player_Scrolls", 3); // Чтение свитков

        PlayerPrefs.SetInt("Player_Crystals", curC + bonusCrystals); // Сохранение кристаллов
        PlayerPrefs.SetInt("Player_Gold", curG + bonusGold); // Сохранение золота
        PlayerPrefs.SetInt("Player_Stones", curS + bonusStones); // Сохранение камней
        PlayerPrefs.SetInt("Player_Scrolls", curSc + bonusScrolls); // Сохранение свитков
        PlayerPrefs.Save(); // Запись на диск

        if (DialogueSystem_Manager.Instance != null) // Синхронизация UI
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources(); // Обновление счетчиков

        if (GameManager.Instance != null) // Начисление в GameManager
            GameManager.Instance.AddResources(bonusGold, bonusStones, bonusScrolls, bonusCrystals); // Выдача всех бонусов

        ShowPopup("МЕСЯЦ ПОЛНОСТЬЮ ЗАКРЫТ!", $"Грандиозно! Вы собрали все дни за {monthNamesRu[month - 1]}!\n\nПолучена Ежемесячная Супер-Награда:\n<b>+{bonusCrystals} Кристаллов\n+{bonusGold} Золота\n+{bonusStones} Камней\n+{bonusScrolls} Свитков</b>", 5f); // Показ попапа
    }

    /// <summary>
    /// Проверка и выдача мега-награды за полное закрытие всех 12 месяцев года (включает Кристаллы)
    /// </summary>
    public void CheckAndClaimYearCompletionReward() // Награда за полностью закрытый год
    {
        string yearRewardKey = $"Cal_YearCompleted_{currentYear}"; // Ключ годовой награды
        if (PlayerPrefs.GetInt(yearRewardKey, 0) == 1) return; // Уже получена ранее

        for (int m = 1; m <= 12; m++) // Проверка каждого из 12 месяцев
        {
            int daysInMonth = DateTime.DaysInMonth(currentYear, m); // Дней в месяце
            for (int d = 1; d <= daysInMonth; d++) // Проверка дней
            {
                if (PlayerPrefs.GetInt($"Cal_Claimed_{currentYear}_{m}_{d}", 0) != 1) return; // Если не закрыт - выход
            }
        }

        // Весь год закрыт! Начисляем Годовую Мега-Награду
        int yearCrystals = 500; // +500 Кристаллов
        int yearGold = 500000; // +500 000 Золота
        int yearStones = 100; // +100 Камней
        int yearScrolls = 100; // +100 Свитков

        PlayerPrefs.SetInt(yearRewardKey, 1); // Фиксация награды года

        int curC = PlayerPrefs.GetInt("Player_Crystals", 0); // Чтение кристаллов
        int curG = PlayerPrefs.GetInt("Player_Gold", 5000); // Чтение золота
        int curS = PlayerPrefs.GetInt("Player_Stones", 10); // Чтение камней
        int curSc = PlayerPrefs.GetInt("Player_Scrolls", 3); // Чтение свитков

        PlayerPrefs.SetInt("Player_Crystals", curC + yearCrystals); // Сохранение кристаллов
        PlayerPrefs.SetInt("Player_Gold", curG + yearGold); // Сохранение золота
        PlayerPrefs.SetInt("Player_Stones", curS + yearStones); // Сохранение камней
        PlayerPrefs.SetInt("Player_Scrolls", curSc + yearScrolls); // Сохранение свитков
        PlayerPrefs.Save(); // Запись на диск

        if (DialogueSystem_Manager.Instance != null) // Синхронизация UI
            DialogueSystem_Manager.Instance.SyncPlayerPrefsResources(); // Обновление счетчиков

        if (GameManager.Instance != null) // Начисление в GameManager
            GameManager.Instance.AddResources(yearGold, yearStones, yearScrolls, yearCrystals); // Выдача всех бонусов

        ShowPopup("ГОД ПОЛНОСТЬЮ ЗАКРЫТ!", $"Невероятно! Вы закрыли все 12 месяцев года!\n\nПолучена Годовая Мега-Награда:\n<b>+{yearCrystals} Кристаллов\n+{yearGold} Золота\n+{yearStones} Камней\n+{yearScrolls} Свитков</b>", 7f); // Показ попапа
    }

    private void UpdateDayCellVisual(GameObject cellObj, int month, int day) // Обновление внешнего вида ячейки
    {
        string saveKey = $"Cal_Claimed_{currentYear}_{month}_{day}"; // Ключ сбора
        bool isClaimed = PlayerPrefs.GetInt(saveKey, 0) == 1; // Статус сбора

        Image bgImage = cellObj.GetComponent<Image>(); // Фон ячейки
        GameObject checkmark = cellObj.transform.Find("Checkmark")?.gameObject; // Зеленая галочка
        GameObject missedBadge = cellObj.transform.Find("Missed_Badge")?.gameObject; // Бейдж пропуска

        // 1. Сегодняшний день
        if (month == currentMonth && day == currentDay) // Если день совпадает с текущим
        {
            if (checkmark != null) checkmark.SetActive(isClaimed); // Галочка если забрал
            if (missedBadge != null) missedBadge.SetActive(false); // Пропуск выключен

            if (bgImage != null) // Настройка цвета фона
            {
                // Если забрали — мягкий зеленый, если доступно к сбору — сияющее золото!
                bgImage.color = isClaimed ? new Color(0.2f, 0.6f, 0.25f, 0.85f) : new Color(1f, 0.85f, 0.2f, 0.95f); // Цвет
            }
        }
        // 2. Прошедшие дни
        else if (month < currentMonth || (month == currentMonth && day < currentDay)) // Если день прошел
        {
            if (checkmark != null) checkmark.SetActive(isClaimed); // Галочка если забран
            if (missedBadge != null) // Если не забран
            {
                missedBadge.SetActive(!isClaimed); // Если не забрали — показываем разбитую колбу
                if (missedFlaskSprite != null && !isClaimed) // Назначение спрайта колбы
                {
                    Image missedImg = missedBadge.GetComponent<Image>(); // Компонент Image колбы
                    if (missedImg != null) missedImg.sprite = missedFlaskSprite; // Установка спрайта разбитой колбы
                }
            }

            if (bgImage != null) // Цвет фона прошедшего дня
            {
                // Забранные — приглушенный зеленый, пропущенные — полупрозрачный темный
                bgImage.color = isClaimed ? new Color(0.15f, 0.45f, 0.2f, 0.6f) : new Color(0.25f, 0.15f, 0.15f, 0.5f); // Цвет
            }
        }
        // 3. Будущие дни
        else // Если будущий день
        {
            if (checkmark != null) checkmark.SetActive(false); // Галочка скрыта
            if (missedBadge != null) missedBadge.SetActive(false); // Пропуск скрыт

            if (bgImage != null) // Фон будущего дня
            {
                bgImage.color = new Color(0.12f, 0.1f, 0.2f, 0.45f); // Полупрозрачный темный фон
            }
        }
    }

    public void RefreshAllDaysUI() // Перерисовка визуала всех ячеек во всех месяцах
    {
        if (monthsContainer == null) return; // Проверка контейнера
        int m = 1; // Счетчик месяцев
        foreach (Transform monthTransform in monthsContainer) // Цикл по карточкам месяцев
        {
            Transform daysGrid = monthTransform.Find("Days_Grid"); // Поиск сетки
            if (daysGrid == null) daysGrid = monthTransform; // Фолбэк

            int d = 1; // Счетчик дней
            foreach (Transform dayTransform in daysGrid) // Цикл по ячейкам
            {
                UpdateDayCellVisual(dayTransform.gameObject, m, d); // Обновление визуала дня
                d++; // Инкремент дня
            }
            m++; // Инкремент месяца
        }
    }

    private void ShowPopup(string title, string message, float autoHideSeconds = 0f) // Показ всплывающего окна
    {
        if (rewardPopup != null && rewardPopupText != null) // Проверка окна и текста
        {
            rewardPopupText.text = $"<size=120%><b>{title}</b></size>\n\n{message}"; // Форматирование текста
            rewardPopup.SetActive(true); // Открытие окна

            if (popupAutoHideCoroutine != null) // Если уже был таймер автоскрытия
            {
                StopCoroutine(popupAutoHideCoroutine); // Остановка старого таймера
                popupAutoHideCoroutine = null; // Обнуление ссылки
            }

            if (autoHideSeconds > 0f) // Если задано время скрытия
            {
                popupAutoHideCoroutine = StartCoroutine(AutoHidePopupRoutine(autoHideSeconds)); // Запуск корутины скрытия
            }
        }
    }

    private System.Collections.IEnumerator AutoHidePopupRoutine(float delay) // Корутина таймера автоскрытия окна
    {
        yield return new WaitForSeconds(delay); // Ожидание заданного времени
        if (rewardPopup != null) // Если окно активно
        {
            rewardPopup.SetActive(false); // Закрытие окна
        }
        popupAutoHideCoroutine = null; // Обнуление ссылки
    }
}
