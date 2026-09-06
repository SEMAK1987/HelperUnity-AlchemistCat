using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Вспомогательный контроллер главного меню для управления красивым
/// параллаксом, инициализацией и анимациями.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Элементы Анимации Главного Экрана")]
    public RectTransform gameTitleText; // Логотип / Название игры для эффекта парения
    public CanvasGroup mainMenuCanvasGroup; // Прозрачность всего главного меню для плавного появления

    [Header("Параллакс Фонового Рисунка")]
    public RectTransform backgroundLayer; // Слой фонового арта с параллакс-сдвигом
    public float parallaxStrength = 20f; // Сила отклонения фона при движении курсора

    [Header("Настройки Дня и Ночи (Day/Night Blending)")]
    [Tooltip("Картинка Дневного Фона (Day Background Image)")]
    public Image dayBackgroundImage; // Слой дневного пейзажа (солнце, светлая палитра)
    [Tooltip("Картинка Ночного Фона (Night Background Image)")]
    public Image nightBackgroundImage; // Слой ночного пейзажа (луна, темная палитра)
    [Tooltip("Включить автоматическую плавную смену суток в меню")]
    public bool autoCycleBackgrounds = true; // Флаг плавного перетекания дня и ночи
    [Tooltip("Скорость перехода (чем выше, тем быстрее меняются день и ночь)")]
    public float dayNightCycleSpeed = 0.5f; // Скорость смены суток
    [Tooltip("Ручное смешивание (0 - чистый день, 1 - чистая ночь)")]
    [Range(0f, 1f)]
    public float dayNightBlendFactor = 0f; // Коэффициент смешивания (0 = день, 1 = ночь)

    [Header("Настройки")]
    public float titleAnimSpeed = 3f; // Скорость анимации покачивания названия

    private Vector2 bgStartPos; // Начальные координаты фона
    private float titleTimer = 0f; // Таймер синусоиды для покачивания
    private bool cycleDirectionUp = true; // Направление перехода дня/ночи

    private void Start()
    {
        if (backgroundLayer != null)
        {
            bgStartPos = backgroundLayer.anchoredPosition; // Запоминаем исходную позицию фона
        }

        // Инициализация прозрачности фонов на старте
        UpdateBackgroundBlending(); // Применяем начальные цвета и альфа-каналы фонов

        // Плавное проявление меню
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 0f; // Делаем меню невидимым
            StartCoroutine(FadeInMenuCoroutine()); // Запускаем корутину плавного появления
        }

        // Автоматически запускаем музыку меню через SettingsManager
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.PlayThemeForActiveScene(); // Включаем музыкальную тему главного меню
        }
    }

    private void Update() // Покадровое обновление эффектов главного меню
    {
        // 1. Анимация парения заголовка (Легкое дыхание)
        if (gameTitleText != null) // Проверка компонента логотипа
        {
            titleTimer += Time.deltaTime * titleAnimSpeed; // Наращиваем таймер
            float offset = Mathf.Sin(titleTimer) * 12f; // Вычисляем смещение по синусоиде
            gameTitleText.anchoredPosition = new Vector2(gameTitleText.anchoredPosition.x, offset); // Применяем новую высоту
        }

        // 2. Интерактивный Параллакс фона за счет наклона мыши
        if (backgroundLayer != null) // Проверка слоя фона
        {
            Vector2 mousePos = Vector2.zero; // Позиция мыши
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue(); // Новая система ввода
            }
            else
            {
                mousePos = Input.mousePosition; // Запасной ввод
            }
#else
            mousePos = Input.mousePosition; // Старая система ввода
#endif

            float normX = (mousePos.x / Screen.width) - 0.5f; // Нормализованное смещение по X (-0.5 .. +0.5)
            float normY = (mousePos.y / Screen.height) - 0.5f; // Нормализованное смещение по Y (-0.5 .. +0.5)

            Vector2 targetPos = bgStartPos + new Vector2(normX * parallaxStrength, normY * parallaxStrength); // Целевая точка параллакса
            backgroundLayer.anchoredPosition = Vector2.Lerp(backgroundLayer.anchoredPosition, targetPos, Time.deltaTime * 5f); // Плавное следование фона
        }

        // 3. Плавный цикл смены дня и ночи
        if (autoCycleBackgrounds) // Если активен авто-цикл
        {
            if (cycleDirectionUp) // Переход в сторону ночи
            {
                dayNightBlendFactor += Time.deltaTime * dayNightCycleSpeed; // Увеличение коэффициента
                if (dayNightBlendFactor >= 1f) // Достигнута ночь
                {
                    dayNightBlendFactor = 1f; // Ограничение
                    cycleDirectionUp = false; // Смена направления в сторону дня
                }
            }
            else // Переход в сторону дня
            {
                dayNightBlendFactor -= Time.deltaTime * dayNightCycleSpeed; // Уменьшение коэффициента
                if (dayNightBlendFactor <= 0f) // Достигнут день
                {
                    dayNightBlendFactor = 0f; // Ограничение
                    cycleDirectionUp = true; // Смена направления в сторону ночи
                }
            }
        }

        UpdateBackgroundBlending(); // Обновление прозрачности слоев
    }

    /// <summary>
    /// Обновляет прозрачность дневного и ночного слоев на основе dayNightBlendFactor (0 = чистый день, 1 = чистая ночь)
    /// </summary>
    public void UpdateBackgroundBlending() // Применение коэффициента прозрачности к слоям
    {
        if (dayBackgroundImage != null) // Дневной фон
        {
            Color c = dayBackgroundImage.color; // Текущий цвет
            // Дневной фон плавно затухает от 1 до 0
            c.a = 1f - dayNightBlendFactor; // Расчет альфы дня
            dayBackgroundImage.color = c; // Применение
        }

        if (nightBackgroundImage != null) // Ночной фон
        {
            Color c = nightBackgroundImage.color; // Текущий цвет
            // Ночной фон плавно проявляется от 0 до 1
            c.a = dayNightBlendFactor; // Расчет альфы ночи
            nightBackgroundImage.color = c; // Применение
        }
    }

    private System.Collections.IEnumerator FadeInMenuCoroutine() // Корутина плавного проявления меню
    {
        float elapsed = 0f; // Счетчик времени
        float duration = 1.2f; // Длительность перехода (сек)

        while (elapsed < duration) // Цикл фейда
        {
            elapsed += Time.deltaTime; // Прирост времени
            mainMenuCanvasGroup.alpha = elapsed / duration; // Установка альфа-прозрачности
            yield return null; // Ожидание кадра
        }
        mainMenuCanvasGroup.alpha = 1f; // Фиксация 100% видимости
    }
}
