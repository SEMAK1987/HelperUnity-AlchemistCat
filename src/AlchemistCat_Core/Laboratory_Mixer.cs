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

    private void Awake()
    {
        Instance = this; // Инициализация синглтона
        InitializeFlasks(); // Инициализация 5 колб пустыми значениями
        InitializeAll40Recipes(); // Загрузка базы 40 алхимических рецептов
        LoadDiscoveredRecipes(); // Загрузка статуса открытых рецептов из памяти
    }

    // Метод для выбора активной колбы (0 - 4) по клику на саму колбу
    public void SelectFlask(int flaskIndex)
    {
        if (flaskIndex >= 0 && flaskIndex < 5)
        {
            currentSelectedFlaskIndex = flaskIndex;
            Debug.Log($"Выбрана колба #{flaskIndex + 1} для наполнения.");
        }
    }

    // Удобный метод для кнопок дозаторов в Unity OnClick(): передаем только номер дозатора (1-10), наливает 10% в текущую выбранную колбу
    public void AddDispenserToCurrentFlask(int dispenser1to10)
    {
        AddLiquidToFlask(currentSelectedFlaskIndex, dispenser1to10 - 1, 10);
    }

    // 10 отдельных готовых функций для быстрого выбора в выпадающем списке Unity OnClick:
    public void Dispenser_1_Ruby_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 0, 10);
    public void Dispenser_2_Sapphire_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 1, 10);
    public void Dispenser_3_Emerald_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 2, 10);
    public void Dispenser_4_Amethyst_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 3, 10);
    public void Dispenser_5_Sun_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 4, 10);
    public void Dispenser_6_Moon_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 5, 10);
    public void Dispenser_7_Aquamarine_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 6, 10);
    public void Dispenser_8_Amber_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 7, 10);
    public void Dispenser_9_Obsidian_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 8, 10);
    public void Dispenser_10_Prism_Add10() => AddLiquidToFlask(currentSelectedFlaskIndex, 9, 10);

    private void InitializeFlasks()
    {
        if (workingFlasks == null || workingFlasks.Length != 5)
        {
            workingFlasks = new WorkingFlask[5];
        }
        for (int i = 0; i < 5; i++)
        {
            if (workingFlasks[i] == null)
                workingFlasks[i] = new WorkingFlask();
        }
    }

    // Автоматическая генерация всех 40 рецептов в коде
    public void InitializeAll40Recipes()
    {
        if (allRecipes != null && allRecipes.Count >= 40) return;

        allRecipes = new List<LabRecipe>();

        // --- КАТЕГОРИЯ 1: 10 ПРОСТЫХ РЕЦЕПТОВ (2 компонента) ---
        AddRecipe(1, "Фиолетовый эликсир бодрости", "Простой", new int[] { 50, 50, 0, 0, 0, 0, 0, 0, 0, 0 }, 5000, 5, 0, 0);
        AddRecipe(2, "Эликсир солнечного листа", "Простой", new int[] { 0, 0, 60, 0, 40, 0, 0, 0, 0, 0 }, 6000, 6, 0, 0);
        AddRecipe(3, "Эссенция ледяного пламени", "Простой", new int[] { 70, 0, 0, 0, 0, 0, 30, 0, 0, 0 }, 7000, 7, 0, 0);
        AddRecipe(4, "Сумеречный настой", "Простой", new int[] { 0, 0, 0, 50, 0, 50, 0, 0, 0, 0 }, 8000, 8, 0, 0);
        AddRecipe(5, "Зелье янтарной смолы", "Простой", new int[] { 0, 0, 40, 0, 0, 0, 0, 60, 0, 0 }, 9000, 9, 0, 0);
        AddRecipe(6, "Грозовой концентрат", "Простой", new int[] { 0, 50, 0, 0, 50, 0, 0, 0, 0, 0 }, 10000, 10, 0, 0);
        AddRecipe(7, "Теневая вуаль", "Простой", new int[] { 0, 0, 0, 30, 0, 0, 0, 0, 70, 0 }, 11000, 11, 0, 0);
        AddRecipe(8, "Морской бриз", "Простой", new int[] { 0, 80, 0, 0, 0, 0, 20, 0, 0, 0 }, 12000, 12, 0, 0);
        AddRecipe(9, "Лунная роса", "Простой", new int[] { 0, 0, 50, 0, 0, 50, 0, 0, 0, 0 }, 13000, 13, 0, 0);
        AddRecipe(10, "Огненный шторм", "Простой", new int[] { 60, 0, 0, 0, 0, 0, 0, 40, 0, 0 }, 15000, 15, 0, 0);

        // --- КАТЕГОРИЯ 2: 10 СРЕДНИХ РЕЦЕПТОВ (3 компонента) ---
        AddRecipe(11, "Зелье Нефритового Дракона", "Средний", new int[] { 0, 0, 30, 0, 0, 30, 0, 40, 0, 0 }, 20000, 20, 5, 0);
        AddRecipe(12, "Эссенция Северного Сияния", "Средний", new int[] { 0, 40, 0, 30, 0, 0, 30, 0, 0, 0 }, 25000, 25, 7, 0);
        AddRecipe(13, "Солнечный феникс", "Средний", new int[] { 50, 0, 0, 0, 30, 0, 0, 20, 0, 0 }, 30000, 30, 8, 0);
        AddRecipe(14, "Глубинный кристалл", "Средний", new int[] { 0, 40, 0, 0, 0, 0, 20, 0, 40, 0 }, 35000, 35, 10, 0);
        AddRecipe(15, "Древесный дух", "Средний", new int[] { 0, 0, 50, 0, 20, 30, 0, 0, 0, 0 }, 40000, 40, 12, 0);
        AddRecipe(16, "Астральный мираж", "Средний", new int[] { 0, 0, 0, 40, 0, 30, 30, 0, 0, 0 }, 45000, 45, 14, 0);
        AddRecipe(17, "Магматический щит", "Средний", new int[] { 35, 0, 0, 0, 0, 0, 0, 45, 20, 0 }, 50000, 50, 15, 0);
        AddRecipe(18, "Благословение зари", "Средний", new int[] { 20, 0, 0, 0, 40, 40, 0, 0, 0, 0 }, 55000, 55, 16, 0);
        AddRecipe(19, "Песнь сирены", "Средний", new int[] { 0, 30, 0, 20, 0, 0, 50, 0, 0, 0 }, 60000, 60, 18, 0);
        AddRecipe(20, "Ночной кошмар", "Средний", new int[] { 20, 0, 0, 30, 0, 0, 0, 0, 50, 0 }, 70000, 70, 20, 0);

        // --- КАТЕГОРИЯ 3: 10 СЛОЖНЫХ РЕЦЕПТОВ (4 компонента) ---
        AddRecipe(21, "Эликсир Четырех Стихий", "Сложный", new int[] { 25, 25, 25, 0, 0, 0, 0, 25, 0, 0 }, 100000, 100, 20, 5);
        AddRecipe(22, "Гармония Небес", "Сложный", new int[] { 0, 0, 0, 20, 30, 30, 20, 0, 0, 0 }, 120000, 120, 25, 7);
        AddRecipe(23, "Вулканический вихрь", "Сложный", new int[] { 35, 0, 0, 0, 15, 0, 0, 25, 25, 0 }, 140000, 140, 30, 10);
        AddRecipe(24, "Океаническая бездна", "Сложный", new int[] { 0, 40, 0, 0, 0, 15, 25, 0, 20, 0 }, 160000, 160, 35, 12);
        AddRecipe(25, "Древний Лес Предков", "Сложный", new int[] { 0, 0, 35, 0, 15, 25, 0, 25, 0, 0 }, 180000, 180, 40, 15);
        AddRecipe(26, "Эфирный Разлом", "Сложный", new int[] { 0, 0, 0, 30, 0, 20, 30, 0, 20, 0 }, 200000, 200, 45, 18);
        AddRecipe(27, "Пламя Прометея", "Сложный", new int[] { 40, 0, 0, 0, 25, 0, 0, 20, 0, 15 }, 230000, 230, 50, 20);
        AddRecipe(28, "Дыхание Валькирии", "Сложный", new int[] { 0, 0, 0, 15, 25, 30, 30, 0, 0, 0 }, 260000, 260, 55, 22);
        AddRecipe(29, "Сердце Титана", "Сложный", new int[] { 20, 0, 0, 0, 20, 0, 0, 35, 25, 0 }, 300000, 300, 60, 25);
        AddRecipe(30, "Врата Бездны", "Сложный", new int[] { 15, 0, 0, 25, 0, 15, 0, 0, 45, 0 }, 350000, 350, 70, 30);

        // --- КАТЕГОРИЯ 4: 10 НЕВЕРОЯТНЫХ РЕЦЕПТОВ (5 компонентов) ---
        AddRecipe(31, "Слеза Ткача Реальности", "Невероятный", new int[] { 0, 0, 0, 15, 20, 15, 0, 0, 20, 30 }, 500000, 500, 50, 50);
        AddRecipe(32, "Свет Сверхновой", "Невероятный", new int[] { 15, 0, 0, 0, 35, 15, 10, 0, 0, 25 }, 550000, 550, 55, 55);
        AddRecipe(33, "Хронос-Эссенция", "Невероятный", new int[] { 0, 15, 0, 10, 0, 25, 20, 0, 0, 30 }, 600000, 600, 60, 60);
        AddRecipe(34, "Древо Миров Иггдрасиль", "Невероятный", new int[] { 0, 0, 30, 0, 15, 10, 0, 20, 0, 25 }, 650000, 650, 65, 65);
        AddRecipe(35, "Око Хаоса", "Невероятный", new int[] { 15, 10, 0, 15, 0, 0, 0, 0, 35, 25 }, 700000, 700, 70, 70);
        AddRecipe(36, "Абсолютный Ноль", "Невероятный", new int[] { 0, 15, 0, 0, 0, 15, 35, 0, 10, 25 }, 750000, 750, 75, 75);
        AddRecipe(37, "Первородная Звезда", "Невероятный", new int[] { 20, 0, 0, 0, 25, 10, 0, 15, 0, 30 }, 800000, 800, 80, 80);
        AddRecipe(38, "Владыка Эфира", "Невероятный", new int[] { 0, 0, 0, 30, 10, 15, 20, 0, 0, 25 }, 850000, 850, 85, 85);
        AddRecipe(39, "Эликсир Бессмертия Древних", "Невероятный", new int[] { 0, 0, 15, 0, 20, 15, 0, 15, 0, 35 }, 900000, 900, 90, 90);
        AddRecipe(40, "Квинтэссенция Философского Камня", "Невероятный", new int[] { 10, 10, 0, 0, 20, 0, 0, 0, 20, 40 }, 1000000, 1000, 100, 100);
    }

    private void AddRecipe(int id, string name, string tier, int[] percentages, int gold, int stones, int scrolls, int crystals)
    {
        allRecipes.Add(new LabRecipe
        {
            id = id,
            recipeName = name,
            tier = tier,
            requiredPercentages = percentages,
            rewardGold = gold,
            rewardStones = stones,
            rewardScrolls = scrolls,
            rewardCrystals = crystals,
            isDiscovered = false
        });
    }

    // Наливание жидкости в колбу
    public void AddLiquidToFlask(int flaskIndex, int dispenserIndex, int amountPercent)
    {
        if (flaskIndex < 0 || flaskIndex >= 5 || dispenserIndex < 0 || dispenserIndex >= 10) return;

        WorkingFlask flask = workingFlasks[flaskIndex];

        if (flask.totalFillPercentage + amountPercent <= 100)
        {
            flask.totalFillPercentage += amountPercent;
            flask.currentEssencePercentages[dispenserIndex] += amountPercent;
            Debug.Log($"В колбу #{flaskIndex + 1} налито {amountPercent}% эссенции #{dispenserIndex + 1}. Всего: {flask.totalFillPercentage}%");

            if (flask.totalFillPercentage == 100)
            {
                CheckRecipeMatch(flaskIndex);
            }
        }
        else
        {
            Debug.LogWarning("Колба переполнена! Максимум 100%.");
        }
    }

    // Проверка соответствия рецепту
    private void CheckRecipeMatch(int flaskIndex)
    {
        WorkingFlask flask = workingFlasks[flaskIndex];

        foreach (var recipe in allRecipes)
        {
            bool match = true;
            for (int i = 0; i < 10; i++)
            {
                if (flask.currentEssencePercentages[i] != recipe.requiredPercentages[i])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                recipe.isDiscovered = true;
                SaveDiscoveredRecipe(recipe.id);
                Debug.Log($"🎉 УСПЕХ! Открыт рецепт: '{recipe.recipeName}' ({recipe.tier})! Теперь его можно продать!");
                return;
            }
        }

        Debug.Log("Смесь не совпала ни с одним рецептом. Получилась 'Нестабильная алхимическая жижа'.");
    }

    // Продажа открытого рецепта
    public bool SellRecipe(int recipeId)
    {
        LabRecipe recipe = allRecipes.Find(r => r.id == recipeId);
        if (recipe != null && recipe.isDiscovered)
        {
            Debug.Log($"Продан рецепт '{recipe.recipeName}'! Награда: {recipe.rewardGold}G, {recipe.rewardStones} камней, {recipe.rewardScrolls} свитков, {recipe.rewardCrystals} кристаллов.");
            return true;
        }
        return false;
    }

    // Очистка колбы
    public void ClearFlask(int flaskIndex)
    {
        if (flaskIndex >= 0 && flaskIndex < 5)
        {
            workingFlasks[flaskIndex] = new WorkingFlask();
            Debug.Log($"Колба #{flaskIndex + 1} очищена.");
        }
    }

    private void SaveDiscoveredRecipe(int id)
    {
        PlayerPrefs.SetInt($"Lab_Recipe_Discovered_{id}", 1);
        PlayerPrefs.Save();
    }

    private void LoadDiscoveredRecipes()
    {
        foreach (var recipe in allRecipes)
        {
            if (PlayerPrefs.GetInt($"Lab_Recipe_Discovered_{recipe.id}", 0) == 1)
            {
                recipe.isDiscovered = true;
            }
        }
    }
}

