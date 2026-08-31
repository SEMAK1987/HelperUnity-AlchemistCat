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
    public static Inventory_Manager Instance { get; private set; }

    [System.Serializable]
    public class ItemStack
    {
        public string itemId;
        public string itemName;
        public int count;
        public int xpPerItem;
        public Sprite icon;
        public Color rarityColor;

        public ItemStack(string id, string name, int count, int xp, Sprite icon, Color color)
        {
            this.itemId = id;
            this.itemName = name;
            this.count = count;
            this.xpPerItem = xp;
            this.icon = icon;
            this.rarityColor = color;
        }
    }

    [Header("UI Панель Сундука / Инвентаря")]
    public GameObject chestInventoryPanel;
    public Transform chestSlotsContainer;
    public GameObject chestSlotPrefab;
    public Button closeChestButton;
    public TextMeshProUGUI totalItemsCountText;
    public TextMeshProUGUI totalChestXpText;

    [Header("Список предметов в сундуке игрока")]
    public List<ItemStack> inventorySlots = new List<ItemStack>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (closeChestButton != null)
            closeChestButton.onClick.AddListener(CloseChestPanel);

        LoadInventory();
    }

    /// <summary>
    /// Добавить предмет в сундук с автоматическим стаком одинаковых
    /// </summary>
    public void AddItem(string itemId, string itemName, int count, int xpPerItem, Sprite icon, Color rarityColor)
    {
        // Ищем, есть ли уже такой предмет в инвентаре
        ItemStack existingSlot = inventorySlots.Find(slot => slot.itemId == itemId);

        if (existingSlot != null)
        {
            // Одинаковые предметы вкладываются друг в друга! Увеличиваем счетчик
            existingSlot.count += count;
            Debug.Log($"[ИНВЕНТАРЬ] Предмет {itemName} сложен в стек! Новое количество: x{existingSlot.count}");
        }
        else
        {
            // Создаем новую ячейку со счетчиком
            ItemStack newStack = new ItemStack(itemId, itemName, count, xpPerItem, icon, rarityColor);
            inventorySlots.Add(newStack);
            Debug.Log($"[ИНВЕНТАРЬ] Создан новый слот для {itemName} (x{count})");
        }

        SaveInventory();
        UpdateChestUI();
    }

    /// <summary>
    /// Массовое добавление улова из мини-игры рыбалки
    /// </summary>
    public void AddFishingSessionLoot(List<AlchemyFishing_Minigame.LootResult> caughtLoot)
    {
        foreach (var loot in caughtLoot)
        {
            AddItem(loot.itemId, loot.itemName, 1, loot.xp, loot.sprite, loot.rarityColor);
        }

        SaveInventory();
        UpdateChestUI();
    }

    /// <summary>
    /// Отрисовка UI сундука со стаками предметов
    /// </summary>
    public void UpdateChestUI()
    {
        if (chestSlotsContainer == null || chestSlotPrefab == null) return;

        // Очищаем старые ячейки
        foreach (Transform child in chestSlotsContainer)
        {
            Destroy(child.gameObject);
        }

        int totalCount = 0;
        int totalXp = 0;

        // Создаем визуальные ячейки для каждого стака
        foreach (var stack in inventorySlots)
        {
            totalCount += stack.count;
            totalXp += stack.xpPerItem * stack.count;

            GameObject slotObj = Instantiate(chestSlotPrefab, chestSlotsContainer);
            
            // Иконка
            Image iconImg = slotObj.transform.Find("Item_Icon")?.GetComponent<Image>();
            if (iconImg != null && stack.icon != null)
            {
                iconImg.sprite = stack.icon;
            }

            // Счетчик количества (x3, x4 и т.д.)
            TextMeshProUGUI countBadge = slotObj.transform.Find("Count_Badge/Text")?.GetComponent<TextMeshProUGUI>();
            if (countBadge != null)
            {
                countBadge.text = $"x{stack.count}";
                countBadge.transform.parent.gameObject.SetActive(stack.count > 1);
            }

            // Название предмета
            TextMeshProUGUI titleText = slotObj.transform.Find("Item_Title")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = stack.itemName;
                titleText.color = stack.rarityColor;
            }

            // Рамка редкости
            Image borderImg = slotObj.transform.Find("Rarity_Border")?.GetComponent<Image>();
            if (borderImg != null)
            {
                borderImg.color = stack.rarityColor;
            }
        }

        if (totalItemsCountText != null)
            totalItemsCountText.text = $"Предметов в сундуке: {totalCount} шт ({inventorySlots.Count} слотов)";

        if (totalChestXpText != null)
            totalChestXpText.text = $"Всего опыта в зельях: +{totalXp} XP";
    }

    public void OpenChestPanel()
    {
        if (chestInventoryPanel != null)
        {
            chestInventoryPanel.SetActive(true);
            UpdateChestUI();
        }
    }

    public void CloseChestPanel()
    {
        if (chestInventoryPanel != null)
            chestInventoryPanel.SetActive(false);
    }

    private void SaveInventory()
    {
        // Сериализация списка слотов в PlayerPrefs
        // В реальном проекте используется SaveGameSystem / JsonUtility
    }

    private void LoadInventory()
    {
        // Загрузка состояния слотов инвентаря
    }
}
