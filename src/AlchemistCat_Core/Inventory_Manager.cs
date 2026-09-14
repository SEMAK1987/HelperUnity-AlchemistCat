using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Единый Мастер-Сундук Алхимика (The One Central Alchemist Chest):
/// - Единая точка складирования всех предметов за все квесты, мини-игры, крафты и покупки
/// - Автоматический стек одинаковых предметов с бейджем количества "x{count}"
/// - Динамическое размещение в сетке слотов сундука с поддержкой до 100 ячеек
/// - Сохранение и загрузка состояния инвентаря в PlayerPrefs
/// </summary>
public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance { get; private set; } // Статический синглтон для доступа к единому сундуку

    [System.Serializable]
    public class ItemStack
    {
        public string itemId; // Уникальный строковый ID предмета
        public string itemName; // Название предмета для отображения
        public int count; // Количество предметов в одном слоте (стек)
        public int xpPerItem; // Опыт, даваемый за использование предмета
        public Sprite icon; // Иконка предмета для инвентаря
        public Color rarityColor; // Цвет рамки редкости (Обычный, Редкий, Легендарный)

        public ItemStack(string id, string name, int count, int xp, Sprite icon, Color color)
        {
            this.itemId = id; // Запоминаем идентификатор
            this.itemName = name; // Запоминаем название
            this.count = count; // Запоминаем число
            this.xpPerItem = xp; // Запоминаем опыт
            this.icon = icon; // Запоминаем иконку
            this.rarityColor = color; // Запоминаем цвет
        }
    }

    [Header("UI Панель Сундука Алхимика")]
    public GameObject chestInventoryPanel; // Главный объект окна сундука
    public Transform chestSlotsContainer; // Контейнер со слотами предметов (Inventory_Slots_Content)
    public GameObject chestSlotPrefab; // Префаб одного слота инвентаря
    public Button closeChestButton; // Кнопка закрытия окна сундука
    public TextMeshProUGUI totalItemsCountText; // Текстовый счетчик общего количества предметов
    public TextMeshProUGUI totalChestXpText; // Текстовый счетчик суммарного опыта предметов

    [Header("Список предметов в едином сундуке игрока")]
    public List<ItemStack> inventorySlots = new List<ItemStack>(); // Список всех слотов предметов в сундуке

    private void Awake() // Инициализация синглтона и защита от уничтожения
    {
        if (Instance == null) // Проверка на первый созданный экземпляр
        {
            Instance = this; // Инициализация синглтона
            DontDestroyOnLoad(gameObject); // Сохранение при смене сцен
        }
        else // Если синглтон уже существует
        {
            Destroy(gameObject); // Удаление дубликата
        }
    }

    private void Start() // Стартовая подписка на UI и загрузка сохраненного инвентаря
    {
        if (closeChestButton != null) // Если кнопка закрытия сундука назначена
            closeChestButton.onClick.AddListener(CloseChestPanel); // Назначение закрытия окна по кнопке

        LoadInventory(); // Загрузка предметов из сохраненного реестра
    }

    /// <summary>
    /// Добавить любой предмет в единый сундук Алхимика со стаком одинаковых предметов
    /// </summary>
    public void AddItem(string itemId, string itemName, int count, int xpPerItem, Sprite icon = null, Color? rarityColor = null) // Добавление предмета в сундук
    {
        Color chosenColor = rarityColor.HasValue ? rarityColor.Value : new Color(0.95f, 0.77f, 0.05f, 1f); // Дефолтный цвет золота

        // Ищем, есть ли уже такой предмет в сундуке
        ItemStack existingSlot = inventorySlots.Find(slot => slot.itemId == itemId); // Поиск существующего слота по ID

        if (existingSlot != null) // Если такой предмет уже есть в сундуке
        {
            // Одинаковые предметы вкладываются друг в друга! Увеличиваем счетчик
            existingSlot.count += count; // Увеличение количества предметов в стеке
            if (icon != null) existingSlot.icon = icon; // Обновление иконки при наличии
            Debug.Log($"[СУНДУК АЛХИМИКА] Предмет '{itemName}' добавлен в общий стек! Новое количество: x{existingSlot.count}"); // Логирование
        }
        else // Если предмет новый
        {
            // Создаем новую ячейку в сундуке
            ItemStack newStack = new ItemStack(itemId, itemName, count, xpPerItem, icon, chosenColor); // Создание нового объекта стека
            inventorySlots.Add(newStack); // Добавление слота в общий список сундука
            Debug.Log($"[СУНДУК АЛХИМИКА] Создан новый слот для '{itemName}' (x{count})"); // Логирование нового слота
        }

        SaveInventory(); // Сохранение обновленного состава сундука
        UpdateChestUI(); // Перерисовка интерфейса сундука

        // Синхронизация со скриптом RecipeCrafting_Manager при его наличии
        if (RecipeCrafting_Manager.Instance != null)
        {
            RecipeCrafting_Manager.Instance.EnsureInventorySlots(); // Проверка ячеек инвентаря
        }
    }

    /// <summary>
    /// Удобный метод для наград из квестов, мини-игр и покупок
    /// </summary>
    public void AddRewardItem(string itemId, string itemName, int count = 1, int xpPerItem = 0, Sprite icon = null)
    {
        AddItem(itemId, itemName, count, xpPerItem, icon, new Color(0.3f, 0.85f, 0.95f, 1f));
    }

    /// <summary>
    /// Массовое добавление улова из мини-игры рыбалки в единый сундук
    /// </summary>
    public void AddFishingSessionLoot(List<AlchemyFishing_Minigame.LootResult> caughtLoot) // Массовое добавление улова в единый сундук
    {
        if (caughtLoot == null) return;
        foreach (var loot in caughtLoot) // Перебор всего выловленного лута
        {
            AddItem(loot.itemId, loot.itemName, 1, loot.xp, loot.sprite, loot.rarityColor); // Добавление в сундук
        }

        SaveInventory(); // Сохранение сундука
        UpdateChestUI(); // Перерисовка сундука
    }

    /// <summary>
    /// Отрисовка UI единого сундука со стаками предметов
    /// </summary>
    public void UpdateChestUI() // Отрисовка ячеек сундука с бейджами количества и рамками редкости
    {
        if (chestSlotsContainer == null) return; // Проверка наличия контейнера

        int totalCount = 0; // Накопитель общего числа предметов
        int totalXp = 0; // Накопитель суммарного опыта

        // Если в контейнере уже есть ячейки (например, подготовленные 100 слотов инвентаря)
        int slotIndex = 0;
        int childCount = chestSlotsContainer.childCount;

        foreach (var stack in inventorySlots) // Перебор каждого стека предметов
        {
            totalCount += stack.count; // Прибавление количества
            totalXp += stack.xpPerItem * stack.count; // Прибавление опыта

            Transform slotTransform = null;

            if (slotIndex < childCount)
            {
                slotTransform = chestSlotsContainer.GetChild(slotIndex); // Используем существующую ячейку сетки
            }
            else if (chestSlotPrefab != null)
            {
                GameObject newSlotObj = Instantiate(chestSlotPrefab, chestSlotsContainer); // Создание новой UI ячейки
                slotTransform = newSlotObj.transform;
            }

            if (slotTransform != null)
            {
                // Настраиваем визуал предмета внутри слота
                Image iconImg = slotTransform.Find("Item_Icon")?.GetComponent<Image>(); // Поиск иконки
                if (iconImg != null && stack.icon != null)
                {
                    iconImg.sprite = stack.icon; // Установка спрайта
                    iconImg.gameObject.SetActive(true);
                }

                TextMeshProUGUI countBadge = slotTransform.Find("Count_Badge/Text")?.GetComponent<TextMeshProUGUI>(); // Поиск бейджа
                if (countBadge != null)
                {
                    countBadge.text = $"x{stack.count}"; // Запись количества
                    countBadge.transform.parent.gameObject.SetActive(stack.count > 1); // Показ бейджа
                }

                TextMeshProUGUI titleText = slotTransform.Find("Item_Title")?.GetComponent<TextMeshProUGUI>(); // Поиск текста заголовка
                if (titleText != null)
                {
                    titleText.text = stack.itemName; // Установка имени
                    titleText.color = stack.rarityColor; // Окрашивание
                }
            }

            slotIndex++;
        }

        if (totalItemsCountText != null) // Текстовый счетчик предметов
            totalItemsCountText.text = $"Предметов в сундуке: {totalCount} шт ({inventorySlots.Count} слотов)";

        if (totalChestXpText != null) // Счетчик опыта
            totalChestXpText.text = $"Всего опыта в зельях: +{totalXp} XP";
    }

    public void OpenChestPanel() // Открытие панели сундука и обновление списка предметов
    {
        if (chestInventoryPanel != null) // Если панель сундука задана
        {
            chestInventoryPanel.SetActive(true); // Включение окна
            UpdateChestUI(); // Перерисовка интерфейса
        }
        else if (RecipeCrafting_Manager.Instance != null)
        {
            RecipeCrafting_Manager.Instance.OpenInventory(); // Открытие инвентаря через RecipeCrafting_Manager
        }
    }

    public void CloseChestPanel() // Закрытие панели сундука
    {
        if (chestInventoryPanel != null) // Если панель сундука задана
            chestInventoryPanel.SetActive(false); // Скрытие окна
    }

    private void SaveInventory() // Сохранение списка предметов в PlayerPrefs
    {
        PlayerPrefs.SetInt("Chest_Slot_Count", inventorySlots.Count); // Сохраняем число слотов
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var slot = inventorySlots[i];
            PlayerPrefs.SetString($"Chest_Slot_{i}_Id", slot.itemId);
            PlayerPrefs.SetString($"Chest_Slot_{i}_Name", slot.itemName);
            PlayerPrefs.SetInt($"Chest_Slot_{i}_Count", slot.count);
            PlayerPrefs.SetInt($"Chest_Slot_{i}_XP", slot.xpPerItem);
        }
        PlayerPrefs.Save();
    }

    private void LoadInventory() // Загрузка сохраненных предметов из памяти
    {
        inventorySlots.Clear();
        int savedCount = PlayerPrefs.GetInt("Chest_Slot_Count", 0);
        for (int i = 0; i < savedCount; i++)
        {
            string id = PlayerPrefs.GetString($"Chest_Slot_{i}_Id", "");
            string name = PlayerPrefs.GetString($"Chest_Slot_{i}_Name", "");
            int count = PlayerPrefs.GetInt($"Chest_Slot_{i}_Count", 1);
            int xp = PlayerPrefs.GetInt($"Chest_Slot_{i}_XP", 0);

            if (!string.IsNullOrEmpty(id))
            {
                inventorySlots.Add(new ItemStack(id, name, count, xp, null, new Color(0.95f, 0.77f, 0.05f, 1f)));
            }
        }
    }
}
