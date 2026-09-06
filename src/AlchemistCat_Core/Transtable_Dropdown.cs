using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Автоматический мост локализации для выпадающих списков TMP_Dropdown.
/// </summary>
[RequireComponent(typeof(TMP_Dropdown))]
public class Transtable_Dropdown : MonoBehaviour
{
    [System.Serializable]
    public struct DropdownOptionTranslation
    {
        [Tooltip("Массив ID строк из Translator для каждой опции выпадающего списка")]
        public int[] optionTextIDs; // Массив идентификаторов строк локализации для вариантов
    }

    public DropdownOptionTranslation translations; // Конфигурация переводов выпадающего списка

    [Tooltip("Принудительно делать текст жирным (Bold) для русского языка")]
    public bool boldForRussian = true; // Делать ли жирным шрифт для русской версии

    private TMP_Dropdown dropdown; // Ссылка на компонент TMP_Dropdown
    private TMP_FontAsset originalCaptionFont; // Исходный шрифт заголовка списка
    private TMP_FontAsset originalItemFont; // Исходный шрифт элементов списка
    private bool isLocalUpdating = false; // Флаг: выполняется ли локальное обновление

    private void Awake() // Инициализация и сохранение базовых шрифтов
    {
        dropdown = GetComponent<TMP_Dropdown>(); // Получение компонента Dropdown
        if (dropdown != null) // Если компонент присутствует
        {
            if (dropdown.captionText != null) originalCaptionFont = dropdown.captionText.font; // Сохранение шрифта заголовка
            if (dropdown.itemText != null) originalItemFont = dropdown.itemText.font; // Сохранение шрифта элементов
        }
    }

    private void OnEnable() // Событие включения объекта
    {
        Translator.AddDropdown(this); // Добавление выпадающего списка в реестр локализации
        UpdateDropdown(); // Немедленный пересчет текста вариантов
    }

    private void OnDisable() // Событие отключения объекта
    {
        Translator.DeleteDropdown(this); // Удаление списка из реестра переводчика
    }

    public void UpdateDropdown() // Метод обновления переводов элементов списка
    {
        if (isLocalUpdating) return; // Защита от рекурсивного вызова
        isLocalUpdating = true; // Установка флага обновления

        try
        {
            if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>(); // Кэширование компонента Dropdown
            if (dropdown == null || Translator.Instance == null) return; // Проверка готовности синглтонов

            bool oldIsUpdating = false; // Сохранение предыдущего состояния настроек
            if (SettingsManager.Instance != null)
            {
                oldIsUpdating = SettingsManager.Instance.isUpdatingSettings; // Запоминаем флаг обновления настроек
                SettingsManager.Instance.isUpdatingSettings = true; // Временно блокируем триггеры событий
            }

            try
            {
                // Сохраняем оригинальные шрифты при первом обновлении, если Awake еще не отработал
                if (originalCaptionFont == null && dropdown.captionText != null) originalCaptionFont = dropdown.captionText.font; // Сохранение шрифта подписи
                if (originalItemFont == null && dropdown.itemText != null) originalItemFont = dropdown.itemText.font; // Сохранение шрифта элементов

                int lang = Translator.LanguageID; // Текущий ID языка
                TMP_FontAsset font = Translator.Instance.defaultFont; // Шрифт для заголовка
                TMP_FontAsset itemFont = Translator.Instance.defaultFont; // Шрифт для выпадающих пунктов

                // Для сохранения оригинальных шрифтов, если это русский или английский и оригинальные шрифты заданы.
                // Для турецкого (2), корейского (7), китайского (8/6) принудительно используем шрифты с полной поддержкой символов.
                if (lang == 0 || lang == 1) // Русский или Английский
                {
                    if (originalCaptionFont != null) font = originalCaptionFont; // Использование исходного шрифта
                    if (originalItemFont != null) itemFont = originalItemFont; // Исходный шрифт элементов
                }
                else if (lang == 7) // Корейский язык
                {
                    font = Translator.Instance.koreanFont; // Корейский шрифт
                    itemFont = Translator.Instance.koreanFont; // Корейский шрифт элементов
                }
                else if (lang == 8 || lang == 6) // Китайский язык
                {
                    font = Translator.Instance.chineseFont; // Китайский шрифт
                    itemFont = Translator.Instance.chineseFont; // Китайский шрифт элементов
                }
                float charSpacing = 0f; // Межбуквенный интервал
                charSpacing = 0f; // Сбрасываем межбуквенный интервал, чтобы русский и турецкий помещались идеально

                // Если включена опция boldForRussian и активный язык русский, делаем текст жирным (Bold)
                FontStyles style = (boldForRussian && lang == 0) ? FontStyles.Bold : FontStyles.Normal; // Выбор начертания

                if (dropdown.captionText != null) // Настройка главного текста текущего выбора
                {
                    dropdown.captionText.font = font; // Назначение шрифта
                    dropdown.captionText.characterSpacing = charSpacing; // Межбуквенный интервал
                    dropdown.captionText.wordSpacing = 0; // Межсловный интервал
                    dropdown.captionText.alignment = TextAlignmentOptions.Center; // Выравнивание по центру
                    dropdown.captionText.fontStyle = style; // Стиль начертания
                    dropdown.captionText.textWrappingMode = TextWrappingModes.NoWrap; // Отключение переноса строк
                    dropdown.captionText.overflowMode = TextOverflowModes.Overflow; // Режим переполнения
                }

                if (dropdown.itemText != null) // Настройка текста элементов в выпадающем окне
                {
                    dropdown.itemText.font = itemFont; // Назначение шрифта элементов
                    dropdown.itemText.characterSpacing = charSpacing; // Межбуквенный интервал
                    dropdown.itemText.wordSpacing = 0; // Межсловный интервал
                    dropdown.itemText.alignment = TextAlignmentOptions.Center; // Выравнивание по центру
                    dropdown.itemText.fontStyle = style; // Стиль начертания
                    dropdown.itemText.textWrappingMode = TextWrappingModes.NoWrap; // Без переносов
                    dropdown.itemText.overflowMode = TextOverflowModes.Overflow; // Без обрезки
                }

                // Применяем перевод по ID или используем автоопределение
                if (gameObject.name.ToLower().Contains("lang") || gameObject.name.ToLower().Contains("language")) // Если это выпадающий список выбора языка
                {
                    if (dropdown.options.Count != 3) // Проверка количества языков
                    {
                        dropdown.ClearOptions(); // Очистка старых опций
                        dropdown.AddOptions(new List<string> { "Русский", "English", "Türkçe" }); // Добавление 3 доступных языков
                    }
                    else
                    {
                        dropdown.options[0].text = "Русский"; // Название опции 0
                        dropdown.options[1].text = "English"; // Название опции 1
                        dropdown.options[2].text = "Türkçe"; // Название опции 2
                    }
                }
                else if (translations.optionTextIDs != null && translations.optionTextIDs.Length > 0) // Если заданы явные ID переводов
                {
                    for (int i = 0; i < dropdown.options.Count; i++) // Перебор опций
                    {
                        if (i < translations.optionTextIDs.Length) // Проверка выхода за границы
                        {
                            dropdown.options[i].text = Translator.GetText(translations.optionTextIDs[i]); // Получение локализованного текста
                        }
                    }
                }
                else
                {
                    // AUTO-DETECT Logic for other types
                    string lowerName = gameObject.name.ToLower(); // Имя объекта в нижнем регистре
                    
                    if (dropdown.options.Count == 6) // Список качества графики (ID 37-42)
                    {
                        for (int i = 0; i < 6; i++) dropdown.options[i].text = Translator.GetText(37 + i); // Подстановка названий уровней качества
                    }
                    else if (dropdown.options.Count == 2) // Полноэкранный режим (Да/Нет)
                    {
                        dropdown.options[0].text = Translator.GetText(44); // Текст "Да"
                        dropdown.options[1].text = Translator.GetText(45); // Текст "Нет"
                    }
                }

                dropdown.RefreshShownValue(); // Обновление отображения выбранного значения
            }
            finally
            {
                if (SettingsManager.Instance != null)
                {
                    SettingsManager.Instance.isUpdatingSettings = oldIsUpdating; // Восстановление состояния настроек
                }
            }
        }
        finally
        {
            isLocalUpdating = false; // Снятие флага обновления
        }
    }
}
