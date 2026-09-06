using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Мини-игра Лаборатория «Химия Моё Всё, Физика Не Помеха»:
/// - 5 пустых колб
/// - 10 дозаторов эссенций
/// - 40 зашитых рецептов (10 Простых, 10 Средних, 10 Сложных, 10 Невероятных)
/// - Авто-инициализация базы, статус "Открыт/Неизвестен", смешивание и продажа открытых рецептов
/// </summary>
public class Laboratory_Mixer : MonoBehaviour
{
    public static Laboratory_Mixer Instance; // Статический синглтон лаборатории смешивания

    [System.Serializable]
    public class LabRecipe
    {
        public int id; // Уникальный идентификатор рецепта
        public string recipeName; // Название рецепта
        public string tier; // Тип сложности: Простой, Средний, Сложный, Невероятный
        public int[] requiredPercentages = new int[10]; // 10 дозаторов (сумма пропорций = 100%)
        public int rewardGold; // Награда золотом
        public int rewardStones; // Награда камнями
        public int rewardScrolls; // Награда свитками
        public int rewardCrystals; // Награда кристаллами
        public bool isDiscovered; // Открыт ли рецепт игроком
    }

    [System.Serializable]
    public class WorkingFlask
    {
        public int totalFillPercentage; // Процент наполненности колбы (0 - 100%)
        public int[] currentEssencePercentages = new int[10]; // Пропорции залитых эссенций (10 дозаторов)
        public Color currentColor = Color.clear; // Текущий смешанный цвет жидкости
    }

    [Header("=== Текущая выбранная колба (0 - 4) ===")]
    public int currentSelectedFlaskIndex = 0; // Индекс выбранной для заливки колбы (от 0 до 4)

    [Header("=== 5 Рабочих Колб ===")]
    public WorkingFlask[] workingFlasks = new WorkingFlask[5]; // Массив 5 рабочих колб лаборатории

    [Header("=== База 40 Рецептов (Генерируется автоматически) ===")]
    public List<LabRecipe> allRecipes = new List<LabRecipe>(); // Список всех 40 рецептов лаборатории

    private void Awake() // Инициализация синглтона, колб, базы 40 рецептов и сохраненных открытий
    {
        Instance = this; // Инициализация синглтона
        InitializeFlasks(); // Инициализация 5 колб пустыми значениями
        InitializeAll40Recipes(); // Загрузка базы 40 алхимических рецептов
        LoadDiscoveredRecipes(); // Загрузка статуса открытых рецептов из памяти
    }

    // Метод для выбора активной колбы (0 - 4) по клику на саму колбу
    public void SelectFlask(int flaskIndex) // Выбор активной рабочей колбы по ее порядковому номеру (0-4)
    {
        if (flaskIndex >= 0 && flaskIndex < 5) // Проверка диапазона от 0 до 4
        {
            currentSelectedFlaskIndex = flaskIndex; // Запоминание выбранной колбы
            Debug.Log($"Выбрана колба #{flaskIndex + 1} для наполнения."); // Логирование выбора колбы
        }
    }

    // Удобный метод для кнопок дозаторов в Unity OnClick(): передаем только номер дозатора (1-10), наливает 10% в текущую выбранную колбу
    public void AddDispenserToCurrentFlask(int dispenser1to10) // Налив 10% эссенции из выбранного дозатора (1-10) в активную колбу
    {
        AddLiquidToFlask(currentSelectedFlaskIndex, dispenser1to10 - 1, 10); // Вызов добавления жидкости
    }

    // 10 отдельных готовых функций для быстрого выбора в выпадающем списке Unity OnClick:
    public void Dispenser_1_Ruby_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 0, 10); // Дозатор 1 (Рубин): налив 10%
    public void Dispenser_2_Sapphire_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 1, 10); // Дозатор 2 (Сапфир): налив 10%
    public void Dispenser_3_Emerald_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 2, 10); // Дозатор 3 (Изумруд): налив 10%
    public void Dispenser_4_Amethyst_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 3, 10); // Дозатор 4 (Аметист): налив 10%
    public void Dispenser_5_Sun_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 4, 10); // Дозатор 5 (Солнце): налив 10%
    public void Dispenser_6_Moon_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 5, 10); // Дозатор 6 (Луна): налив 10%
    public void Dispenser_7_Aquamarine_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 6, 10); // Дозатор 7 (Аквамарин): налив 10%
    public void Dispenser_8_Amber_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 7, 10); // Дозатор 8 (Янтарь): налив 10%
    public void Dispenser_9_Obsidian_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 8, 10); // Дозатор 9 (Обсидиан): налив 10%
    public void Dispenser_10_Prism_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 9, 10); // Дозатор 10 (Призма): налив 10%

    private void InitializeFlasks() // Инициализация и обнуление массива 5 рабочих колб
    {
        if (workingFlasks == null || workingFlasks.Length != 5) // Если массив не создан или имеет неверную длину
        {
            workingFlasks = new WorkingFlask[5]; // Создание массива из 5 колб
        }
        for (int i = 0; i < 5; i++) // Перебор 5 слотов
        {
            if (workingFlasks[i] == null) // Если слот пуст
                workingFlasks[i] = new WorkingFlask(); // Создание чистого объекта колбы
        }
    }

    // Автоматическая генерация всех 40 рецептов в коде
    public void InitializeAll40Recipes() // Автоматическая генерация базы 40 рецептов по 4 категориям сложности
    {
        if (allRecipes != null && allRecipes.Count >= 40) return; // Пропуск если рецепты уже заполнены

        allRecipes = new List<LabRecipe>(); // Создание чистого списка рецептов

        // --- КАТЕГОРИЯ 1: 10 ПРОСТЫХ РЕЦЕПТОВ (2 компонента) ---
        AddRecipe(1, "Фиолетовый эликсир бодрости", "Простой", new int[] { 50, 50, 0, 0, 0, 0, 0, 0, 0, 0 }, 5000, 5, 0, 0); // Простой 1
        AddRecipe(2, "Эликсир солнечного листа", "Простой", new int[] { 0, 0, 60, 0, 40, 0, 0, 0, 0, 0 }, 6000, 6, 0, 0); // Простой 2
        AddRecipe(3, "Эссенция ледяного пламени", "Простой", new int[] { 70, 0, 0, 0, 0, 0, 30, 0, 0, 0 }, 7000, 7, 0, 0); // Простой 3
        AddRecipe(4, "Сумеречный настой", "Простой", new int[] { 0, 0, 0, 50, 0, 50, 0, 0, 0, 0 }, 8000, 8, 0, 0); // Простой 4
        AddRecipe(5, "Зелье янтарной смолы", "Простой", new int[] { 0, 0, 40, 0, 0, 0, 0, 60, 0, 0 }, 9000, 9, 0, 0); // Простой 5
        AddRecipe(6, "Грозовой концентрат", "Простой", new int[] { 0, 50, 0, 0, 50, 0, 0, 0, 0, 0 }, 10000, 10, 0, 0); // Простой 6
        AddRecipe(7, "Теневая вуаль", "Простой", new int[] { 0, 0, 0, 30, 0, 0, 0, 0, 70, 0 }, 11000, 11, 0, 0); // Простой 7
        AddRecipe(8, "Морской бриз", "Простой", new int[] { 0, 80, 0, 0, 0, 0, 20, 0, 0, 0 }, 12000, 12, 0, 0); // Простой 8
        AddRecipe(9, "Лунная роса", "Простой", new int[] { 0, 0, 50, 0, 0, 50, 0, 0, 0, 0 }, 13000, 13, 0, 0); // Простой 9
        AddRecipe(10, "Огненный шторм", "Простой", new int[] { 60, 0, 0, 0, 0, 0, 0, 40, 0, 0 }, 15000, 15, 0, 0); // Простой 10

        // --- КАТЕГОРИЯ 2: 10 СРЕДНИХ РЕЦЕПТОВ (3 компонента) ---
        AddRecipe(11, "Зелье Нефритового Дракона", "Средний", new int[] { 0, 0, 30, 0, 0, 30, 0, 40, 0, 0 }, 20000, 20, 5, 0); // Средний 11
        AddRecipe(12, "Эссенция Северного Сияния", "Средний", new int[] { 0, 40, 0, 30, 0, 0, 30, 0, 0, 0 }, 25000, 25, 7, 0); // Средний 12
        AddRecipe(13, "Солнечный феникс", "Средний", new int[] { 50, 0, 0, 0, 30, 0, 0, 20, 0, 0 }, 30000, 30, 8, 0); // Средний 13
        AddRecipe(14, "Глубинный кристалл", "Средний", new int[] { 0, 40, 0, 0, 0, 0, 20, 0, 40, 0 }, 35000, 35, 10, 0); // Средний 14
        AddRecipe(15, "Древесный дух", "Средний", new int[] { 0, 0, 50, 0, 20, 30, 0, 0, 0, 0 }, 40000, 40, 12, 0); // Средний 15
        AddRecipe(16, "Астральный мираж", "Средний", new int[] { 0, 0, 0, 40, 0, 30, 30, 0, 0, 0 }, 45000, 45, 14, 0); // Средний 16
        AddRecipe(17, "Магматический щит", "Средний", new int[] { 35, 0, 0, 0, 0, 0, 0, 45, 20, 0 }, 50000, 50, 15, 0); // Средний 17
        AddRecipe(18, "Благословение зари", "Средний", new int[] { 20, 0, 0, 0, 40, 40, 0, 0, 0, 0 }, 55000, 55, 16, 0); // Средний 18
        AddRecipe(19, "Песнь сирены", "Средний", new int[] { 0, 30, 0, 20, 0, 0, 50, 0, 0, 0 }, 60000, 60, 18, 0); // Средний 19
        AddRecipe(20, "Ночной кошмар", "Средний", new int[] { 20, 0, 0, 30, 0, 0, 0, 0, 50, 0 }, 70000, 70, 20, 0); // Средний 20

        // --- КАТЕГОРИЯ 3: 10 СЛОЖНЫХ РЕЦЕПТОВ (4 компонента) ---
        AddRecipe(21, "Эликсир Четырех Стихий", "Сложный", new int[] { 25, 25, 25, 0, 0, 0, 0, 25, 0, 0 }, 100000, 100, 20, 5); // Сложный 21
        AddRecipe(22, "Гармония Небес", "Сложный", new int[] { 0, 0, 0, 20, 30, 30, 20, 0, 0, 0 }, 120000, 120, 25, 7); // Сложный 22
        AddRecipe(23, "Вулканический вихрь", "Сложный", new int[] { 35, 0, 0, 0, 15, 0, 0, 25, 25, 0 }, 140000, 140, 30, 10); // Сложный 23
        AddRecipe(24, "Океаническая бездна", "Сложный", new int[] { 0, 40, 0, 0, 0, 15, 25, 0, 20, 0 }, 160000, 160, 35, 12); // Сложный 24
        AddRecipe(25, "Древний Лес Предков", "Сложный", new int[] { 0, 0, 35, 0, 15, 25, 0, 25, 0, 0 }, 180000, 180, 40, 15); // Сложный 25
        AddRecipe(26, "Эфирный Разлом", "Сложный", new int[] { 0, 0, 0, 30, 0, 20, 30, 0, 20, 0 }, 200000, 200, 45, 18); // Сложный 26
        AddRecipe(27, "Пламя Прометея", "Сложный", new int[] { 40, 0, 0, 0, 25, 0, 0, 20, 0, 15 }, 230000, 230, 50, 20); // Сложный 27
        AddRecipe(28, "Дыхание Валькирии", "Сложный", new int[] { 0, 0, 0, 15, 25, 30, 30, 0, 0, 0 }, 260000, 260, 55, 22); // Сложный 28
        AddRecipe(29, "Сердце Титана", "Сложный", new int[] { 20, 0, 0, 0, 20, 0, 0, 35, 25, 0 }, 300000, 300, 60, 25); // Сложный 29
        AddRecipe(30, "Врата Бездны", "Сложный", new int[] { 15, 0, 0, 25, 0, 15, 0, 0, 45, 0 }, 350000, 350, 70, 30); // Сложный 30

        // --- КАТЕГОРИЯ 4: 10 НЕВЕРОЯТНЫХ РЕЦЕПТОВ (5 компонентов) ---
        AddRecipe(31, "Слеза Ткача Реальности", "Невероятный", new int[] { 0, 0, 0, 15, 20, 15, 0, 0, 20, 30 }, 500000, 500, 50, 50); // Невероятный 31
        AddRecipe(32, "Свет Сверхновой", "Невероятный", new int[] { 15, 0, 0, 0, 35, 15, 10, 0, 0, 25 }, 550000, 550, 55, 55); // Невероятный 32
        AddRecipe(33, "Хронос-Эссенция", "Невероятный", new int[] { 0, 15, 0, 10, 0, 25, 20, 0, 0, 30 }, 600000, 600, 60, 60); // Невероятный 33
        AddRecipe(34, "Древо Миров Иггдрасиль", "Невероятный", new int[] { 0, 0, 30, 0, 15, 10, 0, 20, 0, 25 }, 650000, 650, 65, 65); // Невероятный 34
        AddRecipe(35, "Око Хаоса", "Невероятный", new int[] { 15, 10, 0, 15, 0, 0, 0, 0, 35, 25 }, 700000, 700, 70, 70); // Невероятный 35
        AddRecipe(36, "Абсолютный Ноль", "Невероятный", new int[] { 0, 15, 0, 0, 0, 15, 35, 0, 10, 25 }, 750000, 750, 75, 75); // Невероятный 36
        AddRecipe(37, "Первородная Звезда", "Невероятный", new int[] { 20, 0, 0, 0, 25, 10, 0, 15, 0, 30 }, 800000, 800, 80, 80); // Невероятный 37
        AddRecipe(38, "Владыка Эфира", "Невероятный", new int[] { 0, 0, 0, 30, 10, 15, 20, 0, 0, 25 }, 850000, 850, 85, 85); // Невероятный 38
        AddRecipe(39, "Эликсир Бессмертия Древних", "Невероятный", new int[] { 0, 0, 15, 0, 20, 15, 0, 15, 0, 35 }, 900000, 900, 90, 90); // Невероятный 39
        AddRecipe(40, "Квинтэссенция Философского Камня", "Невероятный", new int[] { 10, 10, 0, 0, 20, 0, 0, 0, 20, 40 }, 1000000, 1000, 100, 100); // Невероятный 40
    }

    private void AddRecipe(int id, string name, string tier, int[] percentages, int gold, int stones, int scrolls, int crystals) // Вспомогательный метод создания рецепта лаборатории
    {
        allRecipes.Add(new LabRecipe // Создание структуры рецепта
        {
            id = id, // ID рецепта
            recipeName = name, // Название
            tier = tier, // Сложность
            requiredPercentages = percentages, // Требуемые проценты эссенций
            rewardGold = gold, // Награда золотом
            rewardStones = stones, // Награда камнями
            rewardScrolls = scrolls, // Награда свитками
            rewardCrystals = crystals, // Награда кристаллами
            isDiscovered = false // Изначально не открыт
        });
    }

    // Наливание жидкости в колбу
    public void AddLiquidToFlask(int flaskIndex, int dispenserIndex, int amountPercent) // Наливание определенного процента эссенции в колбу с авто-проверкой готовности при 100%
    {
        if (flaskIndex < 0 || flaskIndex >= 5 || dispenserIndex < 0 || dispenserIndex >= 10) return; // Проверка валидности индексов колбы и дозатора

        WorkingFlask flask = workingFlasks[flaskIndex]; // Получение ссылки на колбу

        if (flask.totalFillPercentage + amountPercent <= 100) // Проверка не превысит ли объем 100%
        {
            flask.totalFillPercentage += amountPercent; // Прибавление общего объема наполнения
            flask.currentEssencePercentages[dispenserIndex] += amountPercent; // Прибавление доли конкретной эссенции
            Debug.Log($"В колбу #{flaskIndex + 1} налито {amountPercent}% эссенции #{dispenserIndex + 1}. Всего: {flask.totalFillPercentage}%"); // Логирование налива

            if (flask.totalFillPercentage == 100) // Если колба наполнена ровно до 100%
            {
                CheckRecipeMatch(flaskIndex); // Запуск сверки состава со списком 40 рецептов
            }
        }
        else // Если колба переполнится
        {
            Debug.LogWarning("Колба переполнена! Максимум 100%."); // Предупреждение о переполнении
        }
    }

    // Проверка соответствия рецепту
    private void CheckRecipeMatch(int flaskIndex) // Проверка пропорций в колбе на точное совпадение с одним из 40 рецептов
    {
        WorkingFlask flask = workingFlasks[flaskIndex]; // Получение содержимого колбы

        foreach (var recipe in allRecipes) // Перебор всех 40 рецептов
        {
            bool match = true; // Флаг полного совпадения
            for (int i = 0; i < 10; i++) // Сверка всех 10 эссенций
            {
                if (flask.currentEssencePercentages[i] != recipe.requiredPercentages[i]) // Если процент хоть одной эссенции не совпал
                {
                    match = false; // Совпадение нарушено
                    break; // Прерывание цикла
                }
            }

            if (match) // Если состав совпал идеально
            {
                recipe.isDiscovered = true; // Открытие рецепта
                SaveDiscoveredRecipe(recipe.id); // Сохранение факта открытия
                Debug.Log($"🎉 УСПЕХ! Открыт рецепт: '{recipe.recipeName}' ({recipe.tier})! Теперь его можно продать!"); // Логирование победы
                return; // Успешный выход
            }
        }

        Debug.Log("Смесь не совпала ни с одним рецептом. Получилась 'Нестабильная алхимическая жижа'."); // Логирование неудачи
    }

    // Продажа открытого рецепта
    public bool SellRecipe(int recipeId) // Продажа открытого рецепта и получение щедрых наград
    {
        LabRecipe recipe = allRecipes.Find(r => r.id == recipeId); // Поиск рецепта по ID
        if (recipe != null && recipe.isDiscovered) // Если рецепт найден и уже открыт
        {
            Debug.Log($"Продан рецепт '{recipe.recipeName}'! Награда: {recipe.rewardGold}G, {recipe.rewardStones} камней, {recipe.rewardScrolls} свитков, {recipe.rewardCrystals} кристаллов."); // Логирование продажи
            return true; // Успешная продажа
        }
        return false; // Рецепт не открыт или не найден
    }

    // Очистка колбы
    public void ClearFlask(int flaskIndex) // Полная очистка и опустошение содержимого колбы
    {
        if (flaskIndex >= 0 && flaskIndex < 5) // Проверка диапазона индекса колбы
        {
            workingFlasks[flaskIndex] = new WorkingFlask(); // Создание новой пустой колбы
            Debug.Log($"Колба #{flaskIndex + 1} очищена."); // Логирование очистки
        }
    }

    private void SaveDiscoveredRecipe(int id) // Сохранение открытого рецепта в PlayerPrefs
    {
        PlayerPrefs.SetInt($"Lab_Recipe_Discovered_{id}", 1); // Установка флага открытия в реестр
        PlayerPrefs.Save(); // Запись на диск
    }

    private void LoadDiscoveredRecipes() // Загрузка статуса открытых рецептов из PlayerPrefs
    {
        foreach (var recipe in allRecipes) // Перебор всех рецептов
        {
            if (PlayerPrefs.GetInt($"Lab_Recipe_Discovered_{recipe.id}", 0) == 1) // Если флаг сохранен
            {
                recipe.isDiscovered = true; // Пометка рецепта открытым
            }
        }
    }
}

