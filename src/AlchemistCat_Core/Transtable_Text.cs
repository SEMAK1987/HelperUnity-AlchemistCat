using UnityEngine;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Автоматический мост локализации для надписей TextMeshPro.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class Transtable_Text : MonoBehaviour
{
    [Tooltip("ID текстовой строки в базе переводчика Translator (например, 0 - Старт, 1 - Продолжить...)")]
    public int TextID; // ID текстовой строки в базе данных переводчика

    [Tooltip("Принудительно делать текст жирным (Bold) для русского языка")]
    public bool boldForRussian = false; // Флаг: включать ли жирное начертание для русского языка

    [HideInInspector]
    public TextMeshProUGUI UIText; // Кэшированная ссылка на компонент TextMeshProUGUI
    
    [HideInInspector]
    public TMP_FontAsset originalFont; // Оригинальный шрифт компонента

    private void Awake()
    {
        UIText = GetComponent<TextMeshProUGUI>(); // Получение ссылки на TextMeshProUGUI
        if (UIText != null)
        {
            originalFont = UIText.font; // Сохранение оригинального шрифта
        }
    }

    private void OnEnable() // Событие включения объекта в иерархии
    {
        Translator.Add(this); // Регистрация компонента в базе переводчика
        UpdateText(); // Немедленное обновление локализованного текста
    }

    private void OnDisable() // Событие выключения объекта
    {
        Translator.Delete(this); // Удаление из списка активных текстов переводчика
    }

    public void UpdateText() // Метод принудительного обновления текста
    {
        Translator.FormatText(this); // Форматирование и применение перевода
    }
}
