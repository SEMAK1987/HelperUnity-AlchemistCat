using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Эффект плавного масштабирования и проигрывания звуков при наведении курсора на кнопки.
/// </summary>
public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Настройки анимации")]
    public float hoverScaleMultiplier = 1.05f; // Множитель увеличения кнопки при наведении мыши (например, 105%)
    public float animationSpeed = 15f; // Скорость сглаживания анимации масштабирования

    [Header("Звуковые эффекты")]
    public bool playSfxOnHover = true; // Проигрывать ли звуковой эффект при наведении курсора
    public bool playSfxOnClick = true; // Проигрывать ли звуковой эффект при клике по кнопке

    private Vector3 originalScale; // Исходный локальный масштаб кнопки
    private Vector3 targetScale; // Целевой масштаб для плавной интерполяции
    private bool isHovered = false; // Флаг: находится ли курсор над кнопкой

    private void Start()
    {
        originalScale = transform.localScale; // Запоминаем базовый размер кнопки
        targetScale = originalScale; // Устанавливаем целевой размер равным базовому
    }

    private void Update()
    {
        // Плавная интерполяция размера для предотвращения резкого дергания
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed); // Плавный переход к targetScale
    }

    public void OnPointerEnter(PointerEventData eventData) // Событие: наведение курсора мыши
    {
        isHovered = true; // Устанавливаем статус наведения
        targetScale = originalScale * hoverScaleMultiplier; // Вычисляем увеличенный целевой размер

        if (playSfxOnHover && SettingsManager.Instance != null) // Проверка разрешения на звук наведения
        {
            SettingsManager.Instance.PlayHoverSound(); // Воспроизведение звука наведения
        }
    }

    public void OnPointerExit(PointerEventData eventData) // Событие: увод курсора мыши
    {
        isHovered = false; // Сброс статуса наведения
        targetScale = originalScale; // Возврат к стандартному размеру кнопки
    }

    public void OnPointerClick(PointerEventData eventData) // Событие: нажатие (клик) по кнопке
    {
        targetScale = originalScale; // Сброс размера при клике
        
        if (playSfxOnClick && SettingsManager.Instance != null) // Проверка разрешения на звук клика
        {
            SettingsManager.Instance.PlayClickSound(); // Воспроизведение звука клика
        }
    }

    private void OnDisable() // Событие: отключение или скрытие объекта
    {
        transform.localScale = originalScale; // Мгновенный сброс масштаба в базовый
        targetScale = originalScale; // Сброс целевого масштаба
    }
}
