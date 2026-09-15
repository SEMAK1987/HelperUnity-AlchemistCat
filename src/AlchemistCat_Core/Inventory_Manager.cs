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

    [Header("Звуковые эффекты предметов")]
    public AudioClip potionDrinkSound; // Звук выпивания зелья опыта
    public AudioClip itemUseSound; // Звук использования предмета / тины / бутылки

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
    /// Отрисовка UI единого сундука со стаками предметов и интерактивным кликом для использования
    /// </summary>
    public void UpdateChestUI() // Отрисовка ячеек сундука с бейджами количества, рамками редкости и кнопками использования
    {
        // Автоматический поиск контейнера слотов, если он не назначен в инспекторе
        if (chestSlotsContainer == null)
        {
            if (RecipeCrafting_Manager.Instance != null && RecipeCrafting_Manager.Instance.inventorySlotsContent != null)
            {
                chestSlotsContainer = RecipeCrafting_Manager.Instance.inventorySlotsContent; // Подключение контейнера слотов из RecipeCrafting_Manager
            }
            else
            {
                GameObject foundObj = GameObject.Find("Inventory_Slots_Content"); // Поиск по имени в сцене
                if (foundObj == null) foundObj = GameObject.Find("Content"); // Запасной поиск
                if (foundObj != null) chestSlotsContainer = foundObj.transform;
            }
        }

        if (chestSlotsContainer == null) return; // Если контейнер не найден, выходим

        int totalCount = 0; // Накопитель общего числа предметов
        int totalXp = 0; // Накопитель суммарного опыта

        int slotIndex = 0;
        int childCount = chestSlotsContainer.childCount;

        // Если в 1-м слоте находится еще не выпитая Колба Мастерства туториала, смещаем заполнение улова
        bool isMasteryFlaskPending = PlayerPrefs.GetInt("Mastery_Flask_Consumed", 0) == 0;
        if (isMasteryFlaskPending && childCount > 0)
        {
            Transform firstSlot = chestSlotsContainer.GetChild(0);
            if (firstSlot != null && firstSlot.Find("Mastery_Potion_Item") != null)
            {
                slotIndex = 1; // Пропускаем 1-ю ячейку с колбой мастерства
            }
        }

        foreach (var stack in inventorySlots) // Перебор каждого стека предметов
        {
            if (stack == null || stack.count <= 0) continue; // Пропуск пустых

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
                // Включаем ячейку
                slotTransform.gameObject.SetActive(true);

                // Настраиваем визуал предмета внутри слота
                Image iconImg = slotTransform.Find("Item_Icon")?.GetComponent<Image>(); // Поиск иконки
                if (iconImg == null) iconImg = slotTransform.GetComponentInChildren<Image>();

                if (iconImg != null)
                {
                    if (stack.icon != null)
                    {
                        iconImg.sprite = stack.icon; // Установка спрайта
                        iconImg.color = Color.white; // Белый цвет
                        iconImg.gameObject.SetActive(true);
                    }
                    else
                    {
                        iconImg.gameObject.SetActive(true);
                        iconImg.color = stack.rarityColor; // Запасная цветовая подсветка
                    }
                }

                // Настройка бейджа количества "x{count}"
                Transform badgeTrans = slotTransform.Find("Count_Badge");
                TextMeshProUGUI countBadge = badgeTrans != null 
                    ? badgeTrans.GetComponentInChildren<TextMeshProUGUI>() 
                    : slotTransform.Find("Count_Text")?.GetComponent<TextMeshProUGUI>();

                if (countBadge != null)
                {
                    countBadge.text = $"x{stack.count}"; // Запись количества
                    if (badgeTrans != null) badgeTrans.gameObject.SetActive(stack.count > 1);
                    else countBadge.gameObject.SetActive(stack.count > 1);
                }

                // Настройка подписи названия
                TextMeshProUGUI titleText = slotTransform.Find("Item_Title")?.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = stack.itemName; // Установка имени
                    titleText.color = stack.rarityColor; // Окрашивание
                }

                // Навешивание кнопки клика по ячейке для использования предмета
                Button slotBtn = slotTransform.GetComponent<Button>();
                if (slotBtn == null) slotBtn = slotTransform.gameObject.AddComponent<Button>();

                string currentId = stack.itemId; // Фиксация строкового ID для замыкания
                slotBtn.onClick.RemoveAllListeners(); // Очистка старых событий
                slotBtn.onClick.AddListener(() => UseItem(currentId)); // Привязка использования предмета по клику
                slotBtn.interactable = true; // Активация интерактивности
            }

            slotIndex++;
        }

        // Очистка / скрытие остальных незанятых ячеек инвентаря
        for (int i = slotIndex; i < childCount; i++)
        {
            Transform extraSlot = chestSlotsContainer.GetChild(i);
            if (extraSlot != null)
            {
                // Не трогаем колбу мастерства в первом слоте
                if (i == 0 && isMasteryFlaskPending) continue;

                Image extraIcon = extraSlot.Find("Item_Icon")?.GetComponent<Image>();
                if (extraIcon != null) extraIcon.gameObject.SetActive(false);

                Transform extraBadge = extraSlot.Find("Count_Badge");
                if (extraBadge != null) extraBadge.gameObject.SetActive(false);

                TextMeshProUGUI extraTitle = extraSlot.Find("Item_Title")?.GetComponent<TextMeshProUGUI>();
                if (extraTitle != null) extraTitle.text = "";

                Button extraBtn = extraSlot.GetComponent<Button>();
                if (extraBtn != null)
                {
                    extraBtn.onClick.RemoveAllListeners();
                    extraBtn.interactable = false;
                }
            }
        }

        if (totalItemsCountText != null) // Текстовый счетчик предметов
            totalItemsCountText.text = $"Предметов в сундуке: {totalCount} шт ({inventorySlots.Count} слотов)";

        if (totalChestXpText != null) // Счетчик опыта
            totalChestXpText.text = $"Всего опыта в зельях: +{totalXp} XP";
    }

    /// <summary>
    /// Использование предмета из сундука по нажатию мышкой:
    /// - Пустая бутылка: дает +5 XP игрока
    /// - Болотная тина: дает +10 XP игрока
    /// - Зелья опыта: дают +10 .. +3000 XP игрока
    /// </summary>
    public void UseItem(string itemId) // Метод использования предмета по клику игрока
    {
        ItemStack stack = inventorySlots.Find(s => s.itemId == itemId);
        if (stack == null || stack.count <= 0) return;

        int xpToGive = stack.xpPerItem;

        // Точное начисление опыта согласно запросу:
        if (itemId == "trash_bottle") // Нажатие на Пустую бутылку дает 5 XP игрока
        {
            xpToGive = 5;
        }
        else if (itemId == "duckweed") // Нажатие на Болотную тину дает 10 XP игрока
        {
            xpToGive = 10;
        }

        // Начисление опыта кота
        if (xpToGive > 0 && Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.GainPlayerExperience(xpToGive); // Начисление в шкалу опыта игрока
        }

        // Воспроизведение звука использования/выпивания
        if (SettingsManager.Instance != null)
        {
            if (potionDrinkSound != null)
            {
                SettingsManager.Instance.PlaySoundEffect(potionDrinkSound); // Воспроизведение назначенного звука выпивания зелья
            }
            else if (RecipeCrafting_Manager.Instance != null && RecipeCrafting_Manager.Instance.potionConsumeSound != null)
            {
                SettingsManager.Instance.PlaySoundEffect(RecipeCrafting_Manager.Instance.potionConsumeSound); // Запасной звук зелья из менеджера крафта
            }
            else if (itemUseSound != null)
            {
                SettingsManager.Instance.PlaySoundEffect(itemUseSound); // Воспроизведение звука использования предмета
            }
            else if (Avatar_Manager.Instance != null && Avatar_Manager.Instance.selectSound != null)
            {
                SettingsManager.Instance.PlaySoundEffect(Avatar_Manager.Instance.selectSound); // Запасной звук клика
            }
        }

        // Уменьшение количества в стеке
        stack.count--;
        if (stack.count <= 0)
        {
            inventorySlots.Remove(stack); // Удаление пустого стека
        }

        SaveInventory(); // Сохранение изменений в PlayerPrefs
        UpdateChestUI(); // Немедленная перерисовка ячеек

        Debug.Log($"[СУНДУК АЛХИМИКА] Использован '{stack.itemName}', получено +{xpToGive} XP игрока! Осталось: {(stack.count > 0 ? stack.count : 0)}");
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
