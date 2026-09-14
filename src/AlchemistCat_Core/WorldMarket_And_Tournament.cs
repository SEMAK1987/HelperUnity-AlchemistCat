using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Мировой Рынок, Покупки и Еженедельный Турнир Лидеров (Ранг Адепт):
/// - Все купленные на рынке товары, выигранные трофеи и награды турнира идут прямо в единый Сундук Алхимика
/// - Комиссия гильдии 1% (снижается до 0% при использовании Пыли Продаж)
/// </summary>
public class WorldMarket_And_Tournament : MonoBehaviour
{
    public static WorldMarket_And_Tournament Instance; // Статический синглтон мирового рынка и турниров

    [System.Serializable]
    public class TournamentReward
    {
        public string rankRange; // Диапазон мест в турнирной таблице ("1 место", "2 место")
        public int gold; // Награда золотом
        public int stones; // Награда камнями
        public int scrolls; // Награда свитками
        public int crystals; // Награда кристаллами
    }

    [Header("=== Призы Турнира ===")]
    public List<TournamentReward> tournamentRewards = new List<TournamentReward>() // Таблица еженедельных турнирных призов
    {
        new TournamentReward { rankRange = "1 место", gold = 500000, stones = 500, scrolls = 100, crystals = 50 },
        new TournamentReward { rankRange = "2 место", gold = 400000, stones = 400, scrolls = 70, crystals = 30 },
        new TournamentReward { rankRange = "3 место", gold = 300000, stones = 300, scrolls = 50, crystals = 20 },
        new TournamentReward { rankRange = "4 место", gold = 200000, stones = 100, scrolls = 30, crystals = 10 },
        new TournamentReward { rankRange = "5 место", gold = 100000, stones = 50, scrolls = 20, crystals = 5 },
        new TournamentReward { rankRange = "6-10 место", gold = 50000, stones = 30, scrolls = 10, crystals = 1 },
        new TournamentReward { rankRange = "11-20 место", gold = 30000, stones = 10, scrolls = 5, crystals = 0 },
        new TournamentReward { rankRange = "21-100 место", gold = 10000, stones = 5, scrolls = 3, crystals = 0 },
        new TournamentReward { rankRange = "101+ место", gold = 5000, stones = 3, scrolls = 1, crystals = 0 }
    };

    private void Awake()
    {
        Instance = this; // Инициализация синглтона
    }

    // Выставление лота на рынок с комиссией 1%
    public bool ListMarketItem(string itemName, int priceGold, int playerRankLevel, int itemRequiredRank) // Публичный метод размещения товара на бирже
    {
        if (playerRankLevel < itemRequiredRank) // Проверка соответствия ранга алхимика требованиям предмета
        {
            Debug.LogWarning("Ваш ранг мастерства слишком мал для продажи этого предмета!"); // Предупреждение о недостатке ранга
            return false; // Отказ в выставлении лота
        }

        int commission = Mathf.Max(1, (int)(priceGold * 0.01f)); // Расчет 1% комиссии гильдии торговцев (минимум 1G)
        Debug.Log($"Предмет {itemName} выставлен на Мировой Рынок за {priceGold}G. Комиссия: {commission}G."); // Логирование успешного размещения
        return true; // Успешное размещение лота на рынке
    }

    /// <summary>
    /// Покупка товара на рынке или во внутриигровом магазине: предмет направляется сразу в единый Сундук Алхимика
    /// </summary>
    public bool BuyMarketItem(string itemId, string itemName, int priceGold, int count = 1, int xp = 0, Sprite icon = null)
    {
        int currentGold = PlayerPrefs.GetInt("Player_Gold", 0);
        if (currentGold < priceGold)
        {
            Debug.LogWarning($"Недостаточно золота для покупки {itemName}! Требуется: {priceGold}G, есть: {currentGold}G.");
            return false;
        }

        // Списание золота
        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.SpendGold(priceGold);
        }
        else
        {
            PlayerPrefs.SetInt("Player_Gold", currentGold - priceGold);
            PlayerPrefs.Save();
        }

        // Отправка купленного предмета прямо в единый Сундук Алхимика
        if (Inventory_Manager.Instance != null)
        {
            Inventory_Manager.Instance.AddItem(itemId, itemName, count, xp, icon, new Color(1f, 0.84f, 0f)); // Золотая рамка магазина
            Debug.Log($"[РЫНОК/МАГАЗИН] Успешная покупка: {itemName} (x{count}) за {priceGold}G! Предмет отправлен в Сундук Алхимика.");
        }

        return true;
    }

    /// <summary>
    /// Зачисление наград турнира лидеров в ресурсы и единый Сундук Алхимика
    /// </summary>
    public void ClaimTournamentReward(int rankIndex)
    {
        if (rankIndex < 0 || rankIndex >= tournamentRewards.Count) return;
        var reward = tournamentRewards[rankIndex];

        if (Avatar_Manager.Instance != null)
        {
            Avatar_Manager.Instance.AddGold(reward.gold);
            Avatar_Manager.Instance.AddStones(reward.stones);
            Avatar_Manager.Instance.AddScrolls(reward.scrolls);
            if (reward.crystals > 0) Avatar_Manager.Instance.AddCrystals(reward.crystals);
        }

        // Выдача кубка или медали турнира в единый сундук
        if (Inventory_Manager.Instance != null)
        {
            Inventory_Manager.Instance.AddItem($"trophy_tournament_rank_{rankIndex + 1}", $"Кубок Турнира ({reward.rankRange})", 1, 500, null, new Color(0.9f, 0.3f, 1f));
        }

        Debug.Log($"[ТУРНИР] Награда за {reward.rankRange} получена и отправлена в Сундук Алхимика!");
    }
}
