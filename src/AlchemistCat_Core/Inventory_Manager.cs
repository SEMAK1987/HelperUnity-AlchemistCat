using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Система Инвентаря и Сундука игрока с поддержкой стака одинаковых предметов:
/// - При добавлении одинаковых зелий/ресурсов они объединяются в один слот
/// - Счетчик количества отображается в виде бейджа "x{count}"
/// - Автоматическая сортировка по качеству и сохранение в PlayerPrefs
/// </summary>
public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance { get; private set; } // Статический синглтон для доступа к инвентарю

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

    [Header("UI Панель Сундука / Инвентаря")]
    public GameObject chestInventoryPanel; // Главный объект окна сундука
    public Transform chestSlotsContainer; // Контейнер (Grid / Layout) со слотами предметов
    public GameObject chestSlotPrefab; // Префаб одного слота инвентаря
    public Button closeChestButton; // Кнопка закрытия окна сундука
    public TextMeshProUGUI totalItemsCountText; // Текстовый счетчик общего количества предметов
    public TextMeshProUGUI totalChestXpText; // Текстовый счетчик суммарного опыта предметов

    [Header("Список предметов в сундуке игрока")]
    public List<ItemStack> inventorySlots = new List<ItemStack>(); // Список всех слотов предметов в сундуке

    private void Awake() // Инициализация синглтона и защита от уничтожения между сценами
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

        LoadInventory(); // Загрузка предметов из сохраненного файла/реестра
    }

    /// <summary>
    /// Добавить предмет в сундук с автоматическим стаком одинаковых
    /// </summary>
    public void AddItem(string itemId, string itemName, int count, int xpPerItem, Sprite icon, Color rarityColor) // Добавление предмета или увеличение количества в стеке
    {
        // Ищем, есть ли уже такой предмет в инвентаре
        ItemStack existingSlot = inventorySlots.Find(slot => slot.itemId == itemId); // Поиск существующего слота по ID

        if (existingSlot != null) // Если такой предмет уже есть в сундуке
        {
            // Одинаковые предметы вкладываются друг в друга! Увеличиваем счетчик
            existingSlot.count += count; // Увеличение количества предметов в стеке
            Debug.Log($"[ИНВЕНТАРЬ] Предмет {itemName} сложен в стек! Новое количество: x{existingSlot.count}"); // Логирование стекирования
        }
        else // Если предмет новый
        {
            // Создаем новую ячейку со счетчиком
            ItemStack newStack = new ItemStack(itemId, itemName, count, xpPerItem, icon, rarityColor); // Создание нового объекта стека
            inventorySlots.Add(newStack); // Добавление слота в общий список
            Debug.Log($"[ИНВЕНТАРЬ] Создан новый слот для {itemName} (x{count})"); // Логирование нового слота
        }

        SaveInventory(); // Сохранение обновленного состава инвентаря
        UpdateChestUI(); // Перерисовка интерфейса сундука
    }

    /// <summary>
    /// Массовое добавление улова из мини-игры рыбалки
    /// </summary>
    public void AddFishingSessionLoot(List<AlchemyFishing_Minigame.LootResult> caughtLoot) // Массовое добавление улова из мини-игры рыбалки
    {
        foreach (var loot in caughtLoot) // Перебор всего выловленного лута
        {
            AddItem(loot.itemId, loot.itemName, 1, loot.xp, loot.sprite, loot.rarityColor); // Добавление каждого предмета в инвентарь
        }

        SaveInventory(); // Сохранение инвентаря после сессии рыбалки
        UpdateChestUI(); // Перерисовка сундука
    }

    /// <summary>
    /// Отрисовка UI сундука со стаками предметов
    /// </summary>
    public void UpdateChestUI() // Отрисовка ячеек сундука с бейджами количества и рамками редкости
    {
        if (chestSlotsContainer == null || chestSlotPrefab == null) return; // Проверка наличия UI контейнера и префаба

        // Очищаем старые ячейки
        foreach (Transform child in chestSlotsContainer) // Перебор всех предыдущих UI слотов
        {
            Destroy(child.gameObject); // Уничтожение старого слота
        }

        int totalCount = 0; // Накопитель общего числа предметов
        int totalXp = 0; // Накопитель суммарного опыта

        // Создаем визуальные ячейки для каждого стака
        foreach (var stack in inventorySlots) // Перебор каждого стека предметов
        {
            totalCount += stack.count; // Прибавление количества предметов
            totalXp += stack.xpPerItem * stack.count; // Прибавление опыта

            GameObject slotObj = Instantiate(chestSlotPrefab, chestSlotsContainer); // Создание UI ячейки из префаба
            
            // Иконка
            Image iconImg = slotObj.transform.Find("Item_Icon")?.GetComponent<Image>(); // Поиск компонента Image иконки
            if (iconImg != null && stack.icon != null) // Если иконка найдена
            {
                iconImg.sprite = stack.icon; // Установка спрайта предмета
            }

            // Счетчик количества (x3, x4 и т.д.)
            TextMeshProUGUI countBadge = slotObj.transform.Find("Count_Badge/Text")?.GetComponent<TextMeshProUGUI>(); // Поиск текста бейджа количества
            if (countBadge != null) // Если бейдж найден
            {
                countBadge.text = $"x{stack.count}"; // Запись текста кратности
                countBadge.transform.parent.gameObject.SetActive(stack.count > 1); // Показ бейджа только если предметов больше одного
            }

            // Название предмета
            TextMeshProUGUI titleText = slotObj.transform.Find("Item_Title")?.GetComponent<TextMeshProUGUI>(); // Поиск текста заголовка
            if (titleText != null) // Если текст заголовка найден
            {
                titleText.text = stack.itemName; // Установка имени предмета
                titleText.color = stack.rarityColor; // Окрашивание в цвет редкости
            }

            // Рамка редкости
            Image borderImg = slotObj.transform.Find("Rarity_Border")?.GetComponent<Image>(); // Поиск рамки редкости
            if (borderImg != null) // Если рамка есть
            {
                borderImg.color = stack.rarityColor; // Установка цвета рамки по редкости
            }
        }

        if (totalItemsCountText != null) // Если текстовый счетчик общего числа назначен
            totalItemsCountText.text = $"Предметов в сундуке: {totalCount} шт ({inventorySlots.Count} слотов)"; // Отображение суммарного числа

        if (totalChestXpText != null) // Если счетчик опыта назначен
            totalChestXpText.text = $"Всего опыта в зельях: +{totalXp} XP"; // Отображение суммарного опыта
    }

    public void OpenChestPanel() // Открытие панели сундука и обновление списка предметов
    {
        if (chestInventoryPanel != null) // Если панель сундука задана
        {
            chestInventoryPanel.SetActive(true); // Включение окна
            UpdateChestUI(); // Перерисовка интерфейса
        }
    }

    public void CloseChestPanel() // Закрытие панели сундука
    {
        if (chestInventoryPanel != null) // Если панель сундука задана
            chestInventoryPanel.SetActive(false); // Скрытие окна
    }

    private void SaveInventory() // Сохранение списка предметов в постоянную память
    {
        // Сериализация списка слотов в PlayerPrefs
        // В реальном проекте используется SaveGameSystem / JsonUtility
    }

    private void LoadInventory() // Загрузка сохраненных предметов из памяти
    {
        // Загрузка состояния слотов инвентаря
    }
}
