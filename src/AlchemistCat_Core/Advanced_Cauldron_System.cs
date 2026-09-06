using UnityEngine;

/// <summary>
/// Продвинутая система Котлов и Навыков Варки:
/// - Котлы +30%, +40%, +50%, 100% x2, 100% x3
/// - Пассивки скорости варки
/// </summary>
public class Advanced_Cauldron_System : MonoBehaviour
{
    public enum CauldronType
    {
        Basic, // Базовый медный котел (стандартный выход)
        Distiller, // Дистиллятор (+30% шанс сварить второе зелье)
        SeniorPharmacist, // Котел Старшего Фармацевта (+40% шанс на дубль)
        Archmagister, // Котел Архимагистра (+50% шанс на дубль)
        TemperatureLord, // Повелитель Температур (100% удвоение + 30% ускорение)
        RealityWeaver // Ткач Реальности (100% утроение зелий x3)
    }

    public CauldronType currentCauldron = CauldronType.Basic; // Текущий установленный котел на столе
    public bool hasQuickAssistantPassive = false; // Пассивный навык: +20% скорость (Эфирный Экспериментатор)
    public bool hasQuickHandsPassive = false; // Пассивный навык: +30% скорость (Повелитель Температур)

    // Расчет множителя получаемых предметов
    public int CalculateCraftOutput()
    {
        switch (currentCauldron)
        {
            case CauldronType.Distiller:
                return (Random.value <= 0.30f) ? 2 : 1; // Проверка 30% шанса

            case CauldronType.SeniorPharmacist:
                return (Random.value <= 0.40f) ? 2 : 1; // Проверка 40% шанса

            case CauldronType.Archmagister:
                return (Random.value <= 0.50f) ? 2 : 1; // Проверка 50% шанса

            case CauldronType.TemperatureLord:
                return 2; // Гарантированные 2 предмета

            case CauldronType.RealityWeaver:
                return 3; // Гарантированные 3 предмета

            default:
                return 1; // Стандартный 1 предмет
        }
    }

    // Расчет скорости варки с учетом пассивок
    public float GetCraftingSpeedMultiplier()
    {
        float speed = 1.0f; // Базовая скорость (100%)
        if (hasQuickHandsPassive) speed += 0.30f; // Бонус +30% к скорости
        else if (hasQuickAssistantPassive) speed += 0.20f; // Бонус +20% к скорости
        return speed; // Итоговый множитель скорости
    }
}
