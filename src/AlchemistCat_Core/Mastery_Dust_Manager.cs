using UnityEngine;

/// <summary>
/// Пыль Мастерства (5 видов мешочков):
/// 1. Пыль Зачарования (+20% ингредиентов)
/// 2. Пыль Расхождения (+20% ускорение варки)
/// 3. Пыль Ресурсов (x2 награды за задание)
/// 4. Пыль Продаж (0% комиссия)
/// 5. Пыль Мастерства (x2 опыт зелий)
/// </summary>
public class Mastery_Dust_Manager : MonoBehaviour
{
    public static Mastery_Dust_Manager Instance; // Статический синглтон менеджера пыли мастерства

    public int dustEnchantmentCount = 0; // Запас Пыли Зачарования (+20% ингредиентов в котлах)
    public int dustDivergenceCount = 0; // Запас Пыли Расхождения (+20% ускорение варки)
    public int dustResourcesCount = 0; // Запас Пыли Ресурсов (x2 награды за задание)
    public int dustSalesCount = 0; // Запас Пыли Продаж (0% комиссия на рынке)
    public int dustMasteryCount = 0; // Запас Пыли Мастерства (x2 опыт зелья 1 раз в месяц)

    private void Awake() => Instance = this; // Инициализация синглтона при старте

    public void UseEnchantmentDust()
    {
        if (dustEnchantmentCount > 0) // Если пыль есть в наличии
        {
            dustEnchantmentCount--; // Списываем 1 мешочек пыли
            Debug.Log("Пыль Зачарования активирована: +20% ингредиентов в котлах на 10 минут!"); // Лог в консоль
        }
    }

    public void UseDivergenceDust()
    {
        if (dustDivergenceCount > 0)
        {
            dustDivergenceCount--;
            Debug.Log("Пыль Расхождения активирована: +20% ускорение варки на 10 минут!");
        }
    }

    public void UseResourcesDust()
    {
        if (dustResourcesCount > 0)
        {
            dustResourcesCount--;
            Debug.Log("Пыль Ресурсов активирована: x2 награды за следующее действие!");
        }
    }

    public void UseSalesDust()
    {
        if (dustSalesCount > 0)
        {
            dustSalesCount--;
            Debug.Log("Пыль Продаж активирована: 0% комиссия рынка на 10 минут!");
        }
    }

    public void UseMasteryDust()
    {
        if (dustMasteryCount > 0)
        {
            dustMasteryCount--;
            Debug.Log("Пыль Мастерства активирована: x2 опыт зелий!");
        }
    }
}
