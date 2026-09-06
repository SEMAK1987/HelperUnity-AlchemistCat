using UnityEngine; // Основное пространство имен Unity
using TMPro; // Поддержка продвинутого текста TextMeshPro

[RequireComponent(typeof(TextMeshProUGUI))] // Автоматическое добавление TextMeshProUGUI при прикреплении скрипта
public class FateMainMenuTitleAnimator : MonoBehaviour // Аниматор парения, дыхания и магического сияния заголовка меню
{
    [Header("Настройки парения (Floating)")]
    [Tooltip("Амплитуда движения по вертикали (Y)")]
    public float floatAmplitude = 12f; // Высота колебаний парения заголовка по оси Y
    [Tooltip("Скорость изменения парения")]
    public float floatSpeed = 1.6f; // Скорость колебаний по синусоиде

    [Header("Настройки дыхания (Scale Breathing)")]
    [Tooltip("Диапазон изменения размера (например, от 97% до 103%)")]
    public float scaleAmplitude = 0.03f; // Амплитуда пульсации масштаба (эффект дыхания)
    [Tooltip("Скорость общения")]
    public float scaleSpeed = 1.3f; // Скорость цикла пульсации размера

    [Header("Настройки сияния (Glow Lerp)")]
    [Tooltip("Включить плавное перелив цвета текста / свечения")]
    public bool enableGlowLerp = true; // Флаг включения перелива цвета и свечения текста
    public Color glowColorStart = new Color(1f, 0.85f, 0.3f, 1f); // Начальный цвет сияния (теплый золотой)
    public Color glowColorEnd = new Color(0.85f, 0.35f, 1f, 1f); // Конечный цвет сияния (магический пурпурный)
    [Tooltip("Скорость перелива цветов")]
    public float glowSpeed = 1.8f; // Скорость смены оттенков сияния

    private RectTransform rectTransform; // Ссылка на RectTransform заголовка
    private TextMeshProUGUI titleText; // Ссылка на текстовый компонент TextMeshProUGUI
    private Vector2 startAnchoredPosition; // Исходная якорная позиция заголовка
    private Vector3 startScale; // Исходный масштаб объекта

    private void Start() // Инициализация компонентов и сохранение исходных координат
    {
        rectTransform = GetComponent<RectTransform>(); // Кэширование компонента RectTransform
        titleText = GetComponent<TextMeshProUGUI>(); // Кэширование компонента TextMeshProUGUI

        if (rectTransform != null) // Если RectTransform найден
        {
            startAnchoredPosition = rectTransform.anchoredPosition; // Сохранение стартовой позиции
            startScale = rectTransform.localScale; // Сохранение стартового масштаба
        }
    }

    private void Update() // Покадровая анимация парения, пульсации и цветового градиента
    {
        float time = Time.time; // Получение текущего игрового времени

        // 1. Плавное парение по синусоиде Y
        if (rectTransform != null) // Если компонент трансформации задан
        {
            float newY = startAnchoredPosition.y + Mathf.Sin(time * floatSpeed) * floatAmplitude; // Расчет новой высоты по синусоиде
            rectTransform.anchoredPosition = new Vector2(startAnchoredPosition.x, newY); // Применение смещения по вертикали

            // 2. Эффект мягкого дыхания (Scale)
            float scaleMultiplier = 1f + Mathf.Sin(time * scaleSpeed) * scaleAmplitude; // Вычисление коэффициента дыхания
            rectTransform.localScale = startScale * scaleMultiplier; // Применение масштабирования к объекту
        }

        // 3. Мягкое переливание цвета текста и свечения в материале
        if (enableGlowLerp && titleText != null) // Если включен перелив цвета и текст доступен
        {
            float t = (Mathf.Sin(time * glowSpeed) + 1f) * 0.5f; // Приведение значения синусоиды к диапазону 0..1
            Color lerpedColor = Color.Lerp(glowColorStart, glowColorEnd, t); // Плавная интерполяция между золотым и пурпурным цветом
            
            titleText.color = lerpedColor; // Применение вычисленного цвета к тексту
            
            // Безопасно обновляем цвет обводки в материале, если он поддерживает это
            if (titleText.fontSharedMaterial != null) // Проверка наличия материала шрифта
            {
                titleText.fontSharedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, lerpedColor * 0.4f); // Установка цвета обводки контура
            }
        }
    }
}
