using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Система смены дня и ночи комнаты Кота-Алхимика.
/// Поддерживает работу по РЕАЛЬНОМУ местному времени игрока (системное время устройства)
/// и мгновенно/плавно переключает дневной и ночной фон.
/// </summary>
public class TimeOfDaySystem : MonoBehaviour
{
    public static TimeOfDaySystem Instance; // Статический синглтон системы смены времени суток

    [Header("Смена Дня и Ночи в комнате")]
    [Tooltip("Дневной фон комнаты алхимика (яркий день)")]
    public Image dayRoomImage; // Слой дневной комнаты алхимика
    [Tooltip("Ночной фон комнаты алхимика (уютный свет свечей и луны)")]
    public Image nightRoomImage; // Слой ночной комнаты алхимика

    [Header("Настройки реального времени суток")]
    [Tooltip("Использовать реальное время устройства игрока")]
    public bool useRealTime = true; // Синхронизация с системными часами устройства игрока
    [Range(0, 23)]
    [Tooltip("Час начала дня (например, 6 утра)")]
    public int dayStartHour = 6; // Час наступления утра (по умолчанию 6:00)
    [Range(0, 23)]
    [Tooltip("Час начала ночи (например, 21 вечера)")]
    public int nightStartHour = 21; // Час наступления ночи (по умолчанию 21:00)

    [Header("Плавность перехода")]
    [Tooltip("Длительность плавного фейда между днем и ночью в секундах")]
    public float transitionDuration = 2.5f; // Время плавного перетекания света в секундах

    [Header("Текущее состояние (Debug)")]
    public bool isNight = false; // Флаг: сейчас ли ночь в игре

    [Header("Тестирование / Ручной режим")]
    public bool manualOverride = false; // Ручной режим тестирования без системных часов

    private Coroutine blendCoroutine; // Ссылка на корутину анимации смены освещения

    private void Awake() // Инициализация синглтона
    {
        Instance = this; // Инициализация синглтона при старте
    }

    private void Start() // Запуск первичной проверки и таймера
    {
        // Первичная проверка при запуске игры
        CheckAndApplyTimeOfDay(instant: true); // Мгновенное применение освещения без задержки
        // Запуск периодической проверки раз в минуту
        StartCoroutine(PeriodicTimeCheck()); // Фоновая корутина проверки времени
    }

    private void Update() // Покадровое обновление ручного режима
    {
        if (manualOverride) // Если включен ручной режим отладки
        {
            ApplyBlendDirect(isNight ? 1f : 0f); // Принудительно устанавливаем день или ночь
        }
    }

    /// <summary>
    /// Проверяет системное время игрока и плавно или мгновенно применяет фон.
    /// </summary>
    public void CheckAndApplyTimeOfDay(bool instant = false) // Проверка и применение освещения по часам
    {
        if (manualOverride) return; // Пропуск при включенном ручном режиме

        if (useRealTime) // Если включен режим реального времени
        {
            // Получаем местный час из системы устройства игрока
            int currentHour = DateTime.Now.Hour; // Получение текущего часа из ОС
            
            // Если час между днем и ночью -> День, иначе -> Ночь
            bool shouldBeNight = false; // Флаг: должна ли сейчас быть ночь
            if (dayStartHour < nightStartHour) // Стандартный суточный интервал
            {
                shouldBeNight = (currentHour < dayStartHour || currentHour >= nightStartHour); // Определение ночного времени
            }
            else
            {
                // Нестандартный диапазон через полночь
                shouldBeNight = (currentHour >= nightStartHour && currentHour < dayStartHour); // Ночь через полночь
            }

            SetDayNightState(shouldBeNight, instant); // Применение состояния дня/ночи
        }
    }

    private IEnumerator PeriodicTimeCheck() // Периодический таймер проверки времени
    {
        while (true) // Бесконечный цикл проверки
        {
            yield return new WaitForSeconds(30f); // Пауза 30 секунд между проверками
            if (!manualOverride && useRealTime) // Если активен авто-режим
            {
                CheckAndApplyTimeOfDay(instant: false); // Плавное обновление освещения
            }
        }
    }

    public void SetDayNightState(bool night, bool instant) // Установка режима день/ночь
    {
        isNight = night; // Обновление переменной состояния

        if (dayRoomImage != null)
            dayRoomImage.gameObject.SetActive(true); // Активация дневного слоя
        if (nightRoomImage != null)
            nightRoomImage.gameObject.SetActive(true); // Активация ночного слоя

        float targetNightAlpha = night ? 1f : 0f; // Целевая прозрачность ночного фона (1 = ночь, 0 = день)

        if (instant) // Мгновенная смена
        {
            ApplyBlendDirect(targetNightAlpha); // Мгновенное применение прозрачности
        }
        else // Плавная анимация
        {
            if (blendCoroutine != null) StopCoroutine(blendCoroutine); // Остановка предыдущего перехода
            blendCoroutine = StartCoroutine(BlendTransition(targetNightAlpha, transitionDuration)); // Запуск плавного перехода
        }
    }

    private void ApplyBlendDirect(float nightAlpha) // Прямая установка прозрачности слоев
    {
        if (dayRoomImage != null) // Обновление дневного слоя
        {
            Color dColor = dayRoomImage.color; // Текущий цвет
            dColor.a = 1f - nightAlpha; // Инвертированная прозрачность дня
            dayRoomImage.color = dColor; // Применение цвета
        }

        if (nightRoomImage != null) // Обновление ночного слоя
        {
            Color nColor = nightRoomImage.color; // Текущий цвет
            nColor.a = nightAlpha; // Прямая прозрачность ночи
            nightRoomImage.color = nColor; // Применение цвета
        }
    }

    private IEnumerator BlendTransition(float targetNightAlpha, float duration) // Корутина плавного фейда освещения
    {
        float startNightAlpha = nightRoomImage != null ? nightRoomImage.color.a : (isNight ? 0f : 1f); // Начальная прозрачность
        float elapsed = 0f; // Счетчик прошедшего времени

        while (elapsed < duration) // Цикл интерполяции
        {
            elapsed += Time.deltaTime; // Прирост времени кадра
            float t = elapsed / duration; // Нормализованный прогресс от 0 до 1
            float currentAlpha = Mathf.Lerp(startNightAlpha, targetNightAlpha, t); // Плавное вычисление прозрачности
            ApplyBlendDirect(currentAlpha); // Применение прозрачности на экран
            yield return null; // Ожидание следующего кадра
        }

        ApplyBlendDirect(targetNightAlpha); // Фиксация финальной прозрачности
    }
}
