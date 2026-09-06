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

    private void OnEnable()
    {
        Translator.Add(this);
        UpdateText();
    }

    private void OnDisable()
    {
        Translator.Delete(this);
    }

    public void UpdateText()
    {
        Translator.FormatText(this);
    }
}
