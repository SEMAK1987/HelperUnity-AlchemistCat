using UnityEngine;

/// <summary>
/// Продвинутая система Котлов и Навыков Варки:
/// - Котлы +30%, +40%, +50%, 100% x2, 100% x3
/// - Пассивки скорости варки
/// </summary>
public class Advanced_Cauldron_System : MonoBehaviour // Продвинутая система котлов и навыков варки
{
    public enum CauldronType // Типы алхимических котлов
    {
        Basic, // Базовый медный котел (стандартный выход 1x)
        Distiller, // Дистиллятор (+30% шанс сварить второе зелье)
        SeniorPharmacist, // Котел Старшего Фармацевта (+40% шанс на дубль)
        Archmagister, // Котел Архимагистра (+50% шанс на дубль)
        TemperatureLord, // Повелитель Температур (100% удвоение + 30% ускорение)
        RealityWeaver // Ткач Реальности (100% утроение зелий x3)
    }

    public CauldronType currentCauldron = CauldronType.Basic; // Текущий установленный котел на столе
    public bool hasQuickAssistantPassive = false; // Пассивный навык: +20% скорость (Эфирный Экспериментатор)
    public bool hasQuickHandsPassive = false; // Пассивный навык: +30% скорость (Повелитель Температур)

    public int CalculateCraftOutput() // Расчет множителя получаемых предметов при варке
    {
        switch (currentCauldron) // Выбор в зависимости от типа котла
        {
            case CauldronType.Distiller: // Дистиллятор
                return (Random.value <= 0.30f) ? 2 : 1; // Проверка 30% шанса на удвоение

            case CauldronType.SeniorPharmacist: // Старший Фармацевт
                return (Random.value <= 0.40f) ? 2 : 1; // Проверка 40% шанса на удвоение

            case CauldronType.Archmagister: // Архимагистр
                return (Random.value <= 0.50f) ? 2 : 1; // Проверка 50% шанса на удвоение

            case CauldronType.TemperatureLord: // Повелитель Температур
                return 2; // Гарантированные 2 предмета (x2)

            case CauldronType.RealityWeaver: // Ткач Реальности
                return 3; // Гарантированные 3 предмета (x3)

            default: // Базовый котел
                return 1; // Стандартный 1 предмет (x1)
        }
    }

    public float GetCraftingSpeedMultiplier() // Расчет скорости варки с учетом пассивок
    {
        float speed = 1.0f; // Базовая скорость (100%)
        if (hasQuickHandsPassive) speed += 0.30f; // Бонус +30% к скорости варки
        else if (hasQuickAssistantPassive) speed += 0.20f; // Бонус +20% к скорости варки
        return speed; // Итоговый множитель скорости
    }
}
