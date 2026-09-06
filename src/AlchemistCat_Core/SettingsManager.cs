#pragma warning disable 0618
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Менеджер глобальных настроек, звука, музыки, лимитера кадров (защита от перегрева GPU) и локализации.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; } // Статический синглтон менеджера настроек

    [Header("UI Компоненты (Назначаются на сцене)")]
    public Slider soundSlider; // Слайдер регулировки громкости звуковых эффектов (SFX)
    public Slider musicSlider; // Слайдер регулировки громкости фоновой музыки (BGM)
    public TMP_Dropdown qualityDropdown; // Выпадающий список выбора графического качества
    public TMP_Dropdown resolutionDropdown; // Выпадающий список выбора экранного разрешения
    public TMP_Dropdown languageDropdown; // Выпадающий список выбора языка интерфейса
    public Toggle fullscreenToggle; // Переключатель полноэкранного / оконного режима

    [Header("Аудио Смеситель")]
    public AudioMixer masterMixer; // Главный аудио-микшер Unity

    [Header("Источники Аудио")]
    [SerializeField] private AudioSource sfxSource; // Источник воспроизведения звуковых эффектов
    [SerializeField] private AudioSource musicSource; // Источник воспроизведения фоновой музыки

    [Header("Клипы эффектов и музыки")]
    [SerializeField] private AudioClip[] hoverSounds; // Звуки наведения курсора на кнопки
    [SerializeField] private AudioClip[] clickSounds; // Звуки нажатия на интерактивные элементы
    [SerializeField] private AudioClip[] menuPlaylist; // Список музыкальных треков главного меню
    [SerializeField] private AudioClip[] labPlaylist; // Список музыкальных треков алхимической лаборатории
    [SerializeField] private AudioClip[] minigamePlaylist; // Музыкальные треки для мини-игр

    private List<Resolution> resolutionsList = new List<Resolution>(); // Список поддерживаемых монитором разрешений
    private int currentPlaylistIndex = 0; // Индекс текущего играющего трека
    private AudioClip[] activePlaylist; // Активный список треков для текущей локации
    public bool isUpdatingSettings = false; // Флаг: происходит ли программное обновление значений в UI

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // Инициализация единственного экземпляра
            if (transform.parent != null) transform.parent = null; // Открепление от родителя для DontDestroyOnLoad
            DontDestroyOnLoad(gameObject); // Сохраняем объект между сменами сцен
            CalibrateAudioSources(); // Автопоиск и настройка аудио источников
            RouteSourcesToMixer(); // Привязка к группам громкости микшера
            InitializeSettings(); // Загрузка и применение сохраненных настроек графики и звука
        }
        else if (Instance != this)
        {
            // Передаем новые UI ссылки в существующий инстанс
            Instance.soundSlider = this.soundSlider; // Перенос ссылки слайдера звука
            Instance.musicSlider = this.musicSlider; // Перенос ссылки слайдера музыки
            Instance.qualityDropdown = this.qualityDropdown; // Перенос списка графики
            Instance.resolutionDropdown = this.resolutionDropdown; // Перенос списка разрешений
            Instance.languageDropdown = this.languageDropdown; // Перенос списка языков
            Instance.fullscreenToggle = this.fullscreenToggle; // Перенос переключателя экрана

            // Останавливаем любые запущенные AudioSource на дубликате
            AudioSource[] duplicateSources = GetComponentsInChildren<AudioSource>(true); // Поиск источников на дубликате
            foreach (var src in duplicateSources) // Перебор источников
            {
                if (src != null) src.Stop(); // Остановка воспроизведения
            }

            Instance.BindUIElements(); // Привязка компонентов в существующем инстансе
            Destroy(gameObject); // Полностью уничтожаем дублирующий объект во избежание конфликтов
        }
    }

    private void Start()
    {
        BindUIElements(); // Первичная привязка элементов пользовательского интерфейса
        PlayThemeForActiveScene(); // Запуск фонового музыкального трека для текущей сцены
    }

    private void CalibrateAudioSources()
    {
        // Полностью отключено, чтобы Unity использовала только ручные ссылки из Inspector
    }

    private void RouteSourcesToMixer()
    {
        // Отключено перераспределение групп микшера, чтобы громкость менялась строго через AudioSource.volume
    }

    private void AutoCalibratePlaylistsAndSounds()
    {
        // Оставляем только базовую подстраховку для кликов
        if (clickSounds == null || clickSounds.Length == 0) // Проверка массива кликов
        {
            Debug.LogWarning("[ALCHEMIST AUDIO] Массив звуков клика пуст. Назначьте аудиоклип Click в Inspector."); // Предупреждение в консоль
        }
    }

    private void InitializeSettings()
    {
        AutoCalibratePlaylistsAndSounds(); // Запуск проверки аудиоклипов

        // 1. Загрузка громкости
        float sVol = PlayerPrefs.GetFloat("Vol_SFX", PlayerPrefs.GetFloat("SoundVolume", 0.75f)); // Считывание громкости эффектов
        float mVol = PlayerPrefs.GetFloat("Vol_Music", PlayerPrefs.GetFloat("MusicVolume", 0.5f)); // Считывание громкости музыки
        SetSFXVolume(sVol); // Применение громкости эффектов
        SetMusicVolume(mVol); // Применение громкости музыки

        // 2. Лимит кадров для защиты от перегрева (v18.11.16 Safeguard)
        int quality = PlayerPrefs.GetInt("QualitySetting", 2); // Среднее качество по умолчанию
        ApplyQualitySafeguards(quality); // Применение лимита FPS

        // 3. Восстановление разрешения
        bool isFull = PlayerPrefs.GetInt("FullscreenMode", Screen.fullScreen ? 1 : 0) == 1; // Загрузка полноэкранного режима
        Screen.fullScreen = isFull; // Установка полноэкранного режима экрана
    }

    public void BindUIElements()
    {
        isUpdatingSettings = true; // Блокировка событий на время заполнения UI
        try
        {
            // 1. Привязка Слайдера Звука (строго по ссылке из Inspector)
            if (soundSlider != null) // Если слайдер звука назначен
            {
                soundSlider.value = PlayerPrefs.GetFloat("Vol_SFX", 0.75f); // Установка сохраненного значения громкости
                ClearPersistentListeners(soundSlider.onValueChanged); // Очистка персистентных слушателей
                soundSlider.onValueChanged.RemoveAllListeners(); // Удаление динамических слушателей
                soundSlider.onValueChanged.AddListener(SetSFXVolume); // Привязка метода регулировки громкости SFX
            }

            // 2. Привязка Слайдера Музыки (строго по ссылке из Inspector)
            if (musicSlider != null) // Если слайдер музыки назначен
            {
                musicSlider.value = PlayerPrefs.GetFloat("Vol_Music", 0.5f); // Установка сохраненного значения музыки
                ClearPersistentListeners(musicSlider.onValueChanged); // Очистка персистентных слушателей
                musicSlider.onValueChanged.RemoveAllListeners(); // Удаление динамических слушателей
                musicSlider.onValueChanged.AddListener(SetMusicVolume); // Привязка метода регулировки громкости BGM
            }

            // 3. Выпадающий список Графики
            if (qualityDropdown != null) // Если выпадающий список качества назначен
            {
                Transtable_Dropdown transDD = qualityDropdown.GetComponent<Transtable_Dropdown>(); // Поиск компонента локализации
                if (transDD == null) transDD = qualityDropdown.gameObject.AddComponent<Transtable_Dropdown>(); // Добавление при отсутствии
                
                transDD.translations.optionTextIDs = new int[] { 37, 38, 39, 40, 41, 42 }; // ID переводов уровней графики

                qualityDropdown.value = PlayerPrefs.GetInt("QualitySetting", 2); // Установка сохраненного качества
                ClearPersistentListeners(qualityDropdown.onValueChanged); // Очистка слушателей
                qualityDropdown.onValueChanged.RemoveAllListeners(); // Удаление слушателей
                qualityDropdown.onValueChanged.AddListener(SetQuality); // Привязка метода смены качества графики
                
                transDD.UpdateDropdown(); // Немедленное обновление названий вариантов
            }

            // 4. Полноэкранный режим
            if (fullscreenToggle != null) // Если переключатель экрана назначен
            {
                fullscreenToggle.isOn = PlayerPrefs.GetInt("FullscreenMode", Screen.fullScreen ? 1 : 0) == 1; // Установка значения
                ClearPersistentListeners(fullscreenToggle.onValueChanged); // Очистка слушателей
                fullscreenToggle.onValueChanged.RemoveAllListeners(); // Удаление слушателей
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen); // Привязка метода переключения режима
            }

            // 5. Выпадающий список Языков (Строго 3 нативных языка)
            if (languageDropdown != null) // Если список языков назначен
            {
                Transtable_Dropdown transDD = languageDropdown.GetComponent<Transtable_Dropdown>(); // Поиск транслятора
                if (transDD != null) DestroyImmediate(transDD); // Удаление лишнего транслятора с языка

                languageDropdown.ClearOptions(); // Очистка старых названий языков
                languageDropdown.AddOptions(new List<string> { "Русский", "English", "Türkçe" }); // Добавление поддерживаемых языков

                AutoCalibrateDropdown(languageDropdown, 55f, 200f, 22f); // Автонастройка размеров выпадающего списка

                languageDropdown.value = PlayerPrefs.GetInt("Alchemist_Language", 0); // Установка выбранного языка
                ClearPersistentListeners(languageDropdown.onValueChanged); // Очистка слушателей
                languageDropdown.onValueChanged.RemoveAllListeners(); // Удаление слушателей
                languageDropdown.onValueChanged.AddListener(SetLanguage); // Привязка обработчика выбора языка
                languageDropdown.RefreshShownValue(); // Обновление плашки выбора
            }

            BuildResolutionsList(); // Формирование списка доступных разрешений
            UpdateDropdownFontStyles(); // Применение шрифтовых стилей выпадающих списков
        }
        finally
        {
            isUpdatingSettings = false; // Снятие флага обновления UI
        }
    }

    private void BuildResolutionsList() // Формирование выпадающего списка экранных разрешений
    {
        if (resolutionDropdown == null) return; // Выход при отсутствии ссылки

        resolutionDropdown.ClearOptions(); // Очистка списка опций
        resolutionsList.Clear(); // Очистка внутреннего списка разрешений

        Resolution[] systemResolutions = Screen.resolutions; // Считывание поддерживаемых разрешений монитора
        List<string> options = new List<string>(); // Список текстовых названий разрешений
        int currentResIndex = 0; // Индекс текущего разрешения

        for (int i = 0; i < systemResolutions.Length; i++) // Перебор системных разрешений
        {
            string option = systemResolutions[i].width + " x " + systemResolutions[i].height; // Форматирование строки разрешения
            options.Add(option); // Добавление варианта в список
            resolutionsList.Add(systemResolutions[i]); // Сохранение структуры разрешения

            if (systemResolutions[i].width == Screen.currentResolution.width && // Проверка соответствия ширины
                systemResolutions[i].height == Screen.currentResolution.height) // Проверка соответствия высоты
            {
                currentResIndex = i; // Запоминаем индекс текущего разрешения
            }
        }

        resolutionDropdown.AddOptions(options); // Заполнение списка опциями
        resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResIndex); // Установка сохраненного разрешения
        resolutionDropdown.onValueChanged.RemoveAllListeners(); // Удаление слушателей
        resolutionDropdown.onValueChanged.AddListener(SetResolution); // Привязка смены разрешения
    }

    private void TrySetMixerFloat(string parameterName, float volValue) // Вспомогательный метод установки параметра микшера
    {
        // Заглушка: очищено для предотвращения ошибок консоли
    }

    public void SetSFXVolume(float val) // Метод установки громкости звуковых эффектов
    {
        if (isUpdatingSettings) return; // Защита от срабатывания во время инициализации
        
        PlayerPrefs.SetFloat("Vol_SFX", val); // Сохранение громкости звуков в реестр

        if (sfxSource != null) // Если источник звука существует
        {
            sfxSource.volume = val; // Изменение громкости источника эффектов
        }
    }

    public void SetMusicVolume(float val) // Метод установки громкости фоновой музыки
    {
        if (isUpdatingSettings) return; // Защита от срабатывания при программной установке
        
        PlayerPrefs.SetFloat("Vol_Music", val); // Сохранение громкости музыки в реестр

        if (musicSource != null) // Если источник фоновой музыки назначен
        {
            musicSource.volume = val; // Изменение громкости источника музыки
        }
    }

    public void SetQuality(int index) // Метод изменения графического профиля качества
    {
        if (isUpdatingSettings) return; // Игнорирование при авто-обновлении

        isUpdatingSettings = true; // Блокировка рекурсии
        try
        {
            QualitySettings.SetQualityLevel(index); // Применение профиля графики в движке Unity
            PlayerPrefs.SetInt("QualitySetting", index); // Сохранение индекса графики
            ApplyQualitySafeguards(index); // Адаптация лимитера FPS под выбранное качество
            Debug.Log($"[ALCHEMIST SETTINGS] Успешно установлено качество графики: {index}"); // Лог успешной установки
        }
        finally
        {
            isUpdatingSettings = false; // Разблокировка
        }
    }

    private void ApplyQualitySafeguards(int qualityLevel) // Ограничение частоты кадров для предотвращения нагрева GPU
    {
        switch (qualityLevel) // Выбор лимита кадров по качеству
        {
            case 0: // Очень Низкое качество
                Application.targetFrameRate = 30; // 30 кадров/сек
                break;
            case 1: // Низкое качество
                Application.targetFrameRate = 30; // 30 кадров/сек
                break;
            case 2: // Среднее качество
                Application.targetFrameRate = 60; // 60 кадров/сек
                break;
            case 3: // Высокое качество
                Application.targetFrameRate = 60; // 60 кадров/сек
                break;
            case 4: // Очень Высокое качество
                Application.targetFrameRate = 120; // 120 кадров/сек
                break;
            case 5: // Ультра качество
                Application.targetFrameRate = 120; // 120 кадров/сек
                break;
            default: // Стандартный профиль
                Application.targetFrameRate = 60; // 60 кадров/сек
                break;
        }
        Debug.Log($"[ALCHEMIST SETTINGS] Лимит кадров установлен на {Application.targetFrameRate} FPS (Качество: {qualityLevel})"); // Лог ограничения
    }

    public void SetFullscreen(bool isFull) // Переключение полноэкранного режима
    {
        if (isUpdatingSettings) return; // Игнорирование при авто-обновлении
        isUpdatingSettings = true; // Блокировка событий
        try
        {
            Screen.fullScreen = isFull; // Установка состояния полного экрана в Unity
            PlayerPrefs.SetInt("FullscreenMode", isFull ? 1 : 0); // Сохранение выбора пользователя
        }
        finally
        {
            isUpdatingSettings = false; // Разблокировка
        }
    }

    public void SetResolution(int index) // Переключение разрешения экрана
    {
        if (isUpdatingSettings) return; // Защита от зацикливания
        isUpdatingSettings = true; // Блокировка
        try
        {
            if (index >= 0 && index < resolutionsList.Count) // Проверка корректности индекса
            {
                Resolution res = resolutionsList[index]; // Получение разрешения из списка
                Screen.SetResolution(res.width, res.height, Screen.fullScreen); // Применение разрешения к экрану
                PlayerPrefs.SetInt("ResolutionIndex", index); // Сохранение индекса разрешения
            }
        }
        finally
        {
            isUpdatingSettings = false; // Разблокировка
        }
    }

    public void SetLanguage(int index) // Смена языка интерфейса
    {
        if (isUpdatingSettings) return; // Игнорирование при обновлении

        isUpdatingSettings = true; // Установка флага блокировки
        try
        {
            Translator.SelectLanguage(index); // Вызов смены языка в переводчике
            Debug.Log($"[ALCHEMIST SETTINGS] Успешно установлен язык: {index} (0=RU, 1=EN, 2=TR)"); // Лог переключения
            UpdateDropdownFontStyles(); // Обновление стилей шрифтов
        }
        finally
        {
            isUpdatingSettings = false; // Разблокировка
        }
    }

    public void UpdateDropdownFontStyles() // Автоматическое обновление стиля шрифта выпадающих списков
    {
        int lang = PlayerPrefs.GetInt("Alchemist_Language", 0); // Считывание языка
        TMPro.FontStyles style = (lang == 0) ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal; // Жирный стиль для русского

        if (Translator.Instance != null && Translator.Instance.defaultFont != null) // Проверка доступности базового шрифта
        {
            if (languageDropdown != null) // Обновление шрифта списка языков
            {
                if (languageDropdown.captionText != null) languageDropdown.captionText.font = Translator.Instance.defaultFont; // Шрифт плашки
                if (languageDropdown.itemText != null) languageDropdown.itemText.font = Translator.Instance.defaultFont; // Шрифт пунктов
            }
            if (qualityDropdown != null) // Обновление шрифта списка графики
            {
                if (qualityDropdown.captionText != null) qualityDropdown.captionText.font = Translator.Instance.defaultFont; // Шрифт плашки
                if (qualityDropdown.itemText != null) qualityDropdown.itemText.font = Translator.Instance.defaultFont; // Шрифт пунктов
            }
            if (resolutionDropdown != null) // Обновление шрифта списка разрешений
            {
                if (resolutionDropdown.captionText != null) resolutionDropdown.captionText.font = Translator.Instance.defaultFont; // Шрифт плашки
                if (resolutionDropdown.itemText != null) resolutionDropdown.itemText.font = Translator.Instance.defaultFont; // Шрифт пунктов
            }
        }

        if (languageDropdown != null) // Обновление стиля списка языков
        {
            if (languageDropdown.captionText != null) languageDropdown.captionText.fontStyle = style; // Начертание плашки
            if (languageDropdown.itemText != null) languageDropdown.itemText.fontStyle = style; // Начертание пунктов
            languageDropdown.RefreshShownValue(); // Обновление плашки
        }

        if (qualityDropdown != null) // Обновление стиля списка качества
        {
            if (qualityDropdown.captionText != null) qualityDropdown.captionText.fontStyle = style; // Начертание плашки
            if (qualityDropdown.itemText != null) qualityDropdown.itemText.fontStyle = style; // Начертание пунктов
            qualityDropdown.RefreshShownValue(); // Обновление плашки
        }

        if (resolutionDropdown != null) // Обновление стиля списка разрешений
        {
            if (resolutionDropdown.captionText != null) resolutionDropdown.captionText.fontStyle = style; // Начертание плашки
            if (resolutionDropdown.itemText != null) resolutionDropdown.itemText.fontStyle = style; // Начертание пунктов
            resolutionDropdown.RefreshShownValue(); // Обновление плашки
        }
    }

    private void AutoCalibrateDropdown(TMP_Dropdown dropdown, float itemHeight, float templateHeight, float fontSize) // Калибровка верстки списка
    {
        if (dropdown == null) return; // Проверка валидности ссылки

        if (Translator.Instance != null && Translator.Instance.defaultFont != null) // Проверка системного шрифта
        {
            if (dropdown.captionText != null) dropdown.captionText.font = Translator.Instance.defaultFont; // Установка шрифта заголовка
            if (dropdown.itemText != null) dropdown.itemText.font = Translator.Instance.defaultFont; // Установка шрифта элементов
        }

        if (dropdown.captionText != null) // Настройка основного текста плашки
        {
            dropdown.captionText.fontSize = fontSize; // Размер шрифта
            dropdown.captionText.alignment = TextAlignmentOptions.Center; // Центрирование текста
            dropdown.captionText.textWrappingMode = TextWrappingModes.NoWrap; // Без переносов
            dropdown.captionText.overflowMode = TextOverflowModes.Overflow; // Без обрезки
            dropdown.captionText.characterSpacing = 0f; // Сброс межбуквенного интервала
            dropdown.captionText.wordSpacing = 0f; // Сброс межсловного интервала
        }

        if (dropdown.itemText != null) // Настройка текста пунктов списка
        {
            dropdown.itemText.fontSize = fontSize - 2f; // Размер шрифта пунктов
            dropdown.itemText.alignment = TextAlignmentOptions.Center; // Выравнивание по центру
            dropdown.itemText.textWrappingMode = TextWrappingModes.NoWrap; // Без переносов строк
            dropdown.itemText.overflowMode = TextOverflowModes.Overflow; // Режим без обрезки
            dropdown.itemText.characterSpacing = 0f; // Межбуквенный интервал
            dropdown.itemText.wordSpacing = 0f; // Межсловный интервал
            dropdown.itemText.color = new Color(0.12f, 0.12f, 0.12f, 1f); // Темно-серый контрастный цвет
        }

        Transform templateTransform = dropdown.transform.Find("Template"); // Поиск объекта шаблона выпадающего окна
        if (templateTransform != null) // Если шаблон найден
        {
            RectTransform templateRect = templateTransform.GetComponent<RectTransform>(); // Компонент RectTransform шаблона
            if (templateRect != null) // Проверка компонента
            {
                int optionCount = dropdown.options != null ? dropdown.options.Count : 3; // Количество вариантов выбора
                float spacingVal = 2f; // Расстояние между пунктами
                float paddingTotal = 16f; // Суммарные отступы сверху и снизу
                float dynamicHeight = (optionCount * itemHeight) + ((optionCount - 1) * spacingVal) + paddingTotal; // Расчет высоты
                
                if (dynamicHeight > 360f) dynamicHeight = 360f; // Ограничение максимальной высоты шторки

                templateRect.sizeDelta = new Vector2(templateRect.sizeDelta.x, dynamicHeight); // Применение вычисленной высоты
            }

            Transform viewport = templateTransform.Find("Viewport"); // Поиск Viewport
            if (viewport != null) // Если найден Viewport
            {
                Transform content = viewport.Find("Content"); // Поиск Content контейнера
                if (content != null) // Если найден Content
                {
                    ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>(); // Поиск ContentSizeFitter
                    if (csf != null) Destroy(csf); // Удаление конфликтующего ContentSizeFitter

                    VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>(); // Поиск VerticalLayoutGroup
                    if (vlg != null) // Если компонент компоновки найден
                    {
                        vlg.childControlHeight = true; // Авто-контроль высоты детей
                        vlg.childControlWidth = true; // Авто-контроль ширины детей
                        vlg.childForceExpandHeight = false; // Без принудительного растяжения высоты
                        vlg.childForceExpandWidth = true; // Принудительное заполнение ширины
                        vlg.spacing = 2f; // Интервал между строками
                        vlg.padding = new RectOffset(0, 0, 8, 8); // Внутренние отступы шторки
                    }

                    Transform item = content.Find("Item"); // Поиск образца элемента списка
                    if (item != null) // Если образец найден
                    {
                        RectTransform itemRect = item.GetComponent<RectTransform>(); // RectTransform элемента
                        if (itemRect != null) itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, itemHeight); // Высота строки

                        LayoutElement itemLayout = item.GetComponent<LayoutElement>(); // Поиск LayoutElement
                        if (itemLayout == null) itemLayout = item.gameObject.AddComponent<LayoutElement>(); // Добавление при отсутствии
                        itemLayout.preferredHeight = itemHeight; // Предпочтительная высота
                        itemLayout.minHeight = itemHeight; // Минимальная высота

                        Transform itemLabel = item.Find("Item Label"); // Поиск надписи элемента
                        if (itemLabel != null) // Если надпись найдена
                        {
                            RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>(); // RectTransform надписи
                            if (itemLabelRect != null) // Проверка компонента
                            {
                                itemLabelRect.anchorMin = Vector2.zero; // Нижний левый якорь
                                itemLabelRect.anchorMax = Vector2.one; // Верхний правый якорь
                                itemLabelRect.offsetMin = new Vector2(30f, 0f); // Отступ слева под галочку
                                itemLabelRect.offsetMax = new Vector2(-15f, 0f); // Отступ справа
                            }
                        }
                    }
                }
            }
        }
    }

    public void PlayHoverSound() // Воспроизведение случайного звука наведения мыши
    {
        if (sfxSource != null && hoverSounds != null && hoverSounds.Length > 0) // Проверка источника и массива клипов
        {
            AudioClip clip = hoverSounds[Random.Range(0, hoverSounds.Length)]; // Выбор случайного клипа наведения
            if (clip != null) sfxSource.PlayOneShot(clip); // Однократное воспроизведение
        }
    }

    public void PlayClickSound() // Воспроизведение случайного звука клика по кнопке
    {
        if (sfxSource != null && clickSounds != null && clickSounds.Length > 0) // Проверка источника и массива кликов
        {
            AudioClip clip = clickSounds[Random.Range(0, clickSounds.Length)]; // Выбор случайного клипа нажатия
            if (clip != null) sfxSource.PlayOneShot(clip); // Однократное воспроизведение клика
        }
    }

    public void PlaySoundEffect(AudioClip clip) // Воспроизведение указанного звукового эффекта
    {
        if (sfxSource != null && clip != null) // Проверка источника и клипа
        {
            sfxSource.PlayOneShot(clip); // Однократный запуск звука
        }
    }

    public void PlayHoverSound(int index) // Совместимость: вызов звука наведения с индексом
    {
        PlayHoverSound(); // Воспроизведение звука наведения
    }

    public void PlaySound(AudioClip clip) // Совместимость: проигрывание аудиоклипа
    {
        PlaySoundEffect(clip); // Воспроизведение звукового эффекта
    }

    public void PlaySfx(AudioClip clip) // Совместимость: проигрывание клипа эффекта
    {
        PlaySoundEffect(clip); // Воспроизведение звукового эффекта
    }

    public void PlaySfx(string sfxName) // Совместимость: проигрывание звука по названию
    {
        PlaySFX(sfxName); // Запуск звука по имени ресурса
    }

    public void PlaySFX(string sfxName) // Загрузка и воспроизведение звукового файла из Resources
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + sfxName); // Попытка загрузки из папки Audio
        if (clip == null) clip = Resources.Load<AudioClip>(sfxName); // Попытка загрузки из корня Resources
        if (clip != null) PlaySoundEffect(clip); // Воспроизведение загруженного звука
    }

    public void PlayMusicTrack(int playlistIndex, int trackIndex) // Запуск плейлиста по индексу
    {
        switch (playlistIndex) // Выбор плейлиста
        {
            case 0: ChangePlaylist(menuPlaylist); break; // Плейлист главного меню
            case 1: ChangePlaylist(menuPlaylist); break; // Дополнительный плейлист меню
            case 2: ChangePlaylist(labPlaylist); break; // Музыка алхимической лаборатории
            case 3: ChangePlaylist(minigamePlaylist); break; // Музыка для мини-игр
        }
    }

    public void BindLoadedUIElements() // Совместимость: принудительная привязка UI
    {
        BindUIElements(); // Вызов основной привязки элементов
    }

    public void PlayThemeForActiveScene() // Автоматический запуск фоновой музыки для текущей открытой сцены
    {
        string sceneName = SceneManager.GetActiveScene().name.ToLower(); // Получение имени активной сцены в нижнем регистре

        if (sceneName.Contains("menu") || sceneName.Contains("title")) // Если открыто главное меню
        {
            ChangePlaylist(menuPlaylist); // Включение музыки меню
        }
        else if (sceneName.Contains("lab") || sceneName.Contains("alchemy") || sceneName.Contains("game")) // Если открыта лаборатория
        {
            ChangePlaylist(labPlaylist); // Включение музыки лаборатории
        }
        else // Для всех остальных сцен и мини-игр
        {
            ChangePlaylist(minigamePlaylist); // Включение музыки мини-игр
        }
    }

    private void ChangePlaylist(AudioClip[] newPlaylist) // Метод плавной смены музыкального плейлиста
    {
        if (newPlaylist == null || newPlaylist.Length == 0) return; // Проверка на пустой плейлист

        if (activePlaylist == newPlaylist && musicSource != null && musicSource.isPlaying) // Если этот плейлист уже играет
        {
            return; // Выход для предотвращения повторного перезапуска трека
        }

        activePlaylist = newPlaylist; // Назначение нового активного плейлиста
        currentPlaylistIndex = 0; // Сброс индекса трека на начало
        PlayPlaylistTrack(); // Запуск первого трека плейлиста
    }

    private void PlayPlaylistTrack() // Запуск текущего трека из активного плейлиста
    {
        if (musicSource == null || activePlaylist == null || activePlaylist.Length == 0) return; // Проверка источника и треков

        AudioClip track = activePlaylist[currentPlaylistIndex]; // Получение аудиоклипа по индексу
        if (track != null) // Если клип валиден
        {
            if (sfxSource != null && sfxSource.clip == track) // Проверка: не занят ли клип на SFX источнике
            {
                sfxSource.Stop(); // Остановка SFX источника
                sfxSource.clip = null; // Очистка клипа
            }

            musicSource.clip = track; // Назначение трека на источник фоновой музыки
            musicSource.loop = true; // Зацикливание трека
            if (!musicSource.isPlaying) // Если трек еще не играет
            {
                musicSource.Play(); // Запуск воспроизведения музыки
            }
        }
    }

    private void ClearPersistentListeners(UnityEngine.Events.UnityEventBase unityEvent) // Очистка персистентных слушателей событий Unity
    {
        if (unityEvent == null) return; // Проверка на null
        int count = unityEvent.GetPersistentEventCount(); // Получение количества постоянных слушателей
        for (int i = 0; i < count; i++) // Перебор постоянных слушателей
        {
            unityEvent.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off); // Отключение постоянного слушателя
        }
    }
}
