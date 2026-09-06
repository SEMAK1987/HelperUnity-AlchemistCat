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

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = originalScale * hoverScaleMultiplier;

        if (playSfxOnHover && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.PlayHoverSound();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        targetScale = originalScale; // Сброс размера при клике
        
        if (playSfxOnClick && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.PlayClickSound();
        }
    }

    private void OnDisable()
    {
        // Сброс размера в исходное состояние при скрытии панели
        transform.localScale = originalScale;
        targetScale = originalScale;
    }
}
