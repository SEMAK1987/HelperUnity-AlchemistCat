using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Главный контроллер меню игры "Алхимический Кот".
/// </summary>
public class Menu_Game : MonoBehaviour
{
    public static Menu_Game Instance { get; private set; } // Статический синглтон главного меню игры

    private void Awake() // Инициализация синглтона меню
    {
        if (Instance == null) // Если первый запуск
        {
            Instance = this; // Инициализация синглтона при первом запуске
        }
        else if (Instance != this) // Если повторная сцена
        {
            Instance.TransferNewReferences(this); // Автоматический перенос ссылок на новый инстанс
            Destroy(gameObject); // Уничтожение лишнего объекта
            return; // Выход
        }
    }

    private void OnDestroy() // Очистка синглтона при удалении
    {
        if (Instance == this) // Если текущий объект
        {
            Instance = null; // Очистка синглтона при уничтожении
        }
    }

    public void TransferNewReferences(Menu_Game newInstance) // Метод обновления ссылок при повторной загрузке сцены
    {
        try
        {
            this.mainMenuPanel = newInstance.mainMenuPanel; // Панель главного меню
            this.settingsPanel = newInstance.settingsPanel; // Панель настроек

            this.startButton = newInstance.startButton; // Кнопка старта
            this.settingsButton = newInstance.settingsButton; // Кнопка настроек
            this.exitButton = newInstance.exitButton; // Кнопка выхода
            this.settingsBackButton = newInstance.settingsBackButton; // Кнопка возврата

            this.startButtonPadding = newInstance.startButtonPadding; // Отступы клика старта
            this.settingsButtonPadding = newInstance.settingsButtonPadding; // Отступы клика настроек
            this.exitButtonPadding = newInstance.exitButtonPadding; // Отступы клика выхода
            this.backButtonPadding = newInstance.backButtonPadding; // Отступы клика возврата

            this.dayBackgroundImage = newInstance.dayBackgroundImage; // Дневной фон
            this.nightBackgroundImage = newInstance.nightBackgroundImage; // Ночной фон
            this.autoCycleBackgrounds = newInstance.autoCycleBackgrounds; // Флаг авто-цикла
            this.dayNightCycleSpeed = newInstance.dayNightCycleSpeed; // Скорость смены дня/ночи
            this.dayNightBlendFactor = newInstance.dayNightBlendFactor; // Коэффициент дня/ночи
            this.cycleType = newInstance.cycleType; // Тип суточного цикла

            this.gameTitleText = newInstance.gameTitleText; // Логотип игры
            this.backgroundLayer = newInstance.backgroundLayer; // Слой фона
            this.parallaxStrength = newInstance.parallaxStrength; // Сила параллакса

            SetupListeners(); // Повторная привязка обработчиков нажатий
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ALCHEMIST MENU ERROR] Ошибка при автоматическом переносе ссылок: {ex}"); // Лог ошибки
        }
    }

    public enum DayNightCycleType
    {
        AutomaticPingPong, // Бесконечный цикл туда-обратно
        RealTimeClock, // Привязка к реальному времени компьютера (00:00 - ночь, 12:00 - день)
        Manual // Ручное управление через ползунок или внешние скрипты
    }

    [Header("Панели Меню")]
    [Tooltip("Основная панель главного меню")]
    public GameObject mainMenuPanel; // Основная панель главного меню
    [Tooltip("Панель настроек")]
    public GameObject settingsPanel; // Панель настроек звука и графики

    [Header("Кнопки Меню")]
    [Tooltip("Кнопка запуска/продолжения игры (Играть)")]
    public Button startButton; // Кнопка "Играть"
    [Tooltip("Кнопка открытия настроек")]
    public Button settingsButton; // Кнопка "Настройки"
    [Tooltip("Кнопка выхода")]
    public Button exitButton; // Кнопка "Выход"
    [Tooltip("Кнопка возврата из настроек в главное меню (Назад)")]
    public Button settingsBackButton; // Кнопка "Назад" в настройках

    [Header("Ограничение Клик-Зоны (Raycast Padding)")]
    [Tooltip("Отступы внутрь для клик-зоны Кнопки ИГРАТЬ (X=Слева, Y=Снизу, Z=Справа, W=Сверху)")]
    public Vector4 startButtonPadding = new Vector4(0f, 0f, 0f, 0f); // Отступы клика кнопки Старт
    [Tooltip("Отступы внутрь для клик-зоны Кнопки НАСТРОЙКИ (X=Слева, Y=Снизу, Z=Справа, W=Сверху)")]
    public Vector4 settingsButtonPadding = new Vector4(0f, 0f, 0f, 0f); // Отступы клика кнопки Настройки
    [Tooltip("Отступы внутрь для клик-зоны Кнопки ВЫХОД (X=Слева, Y=Снизу, Z=Справа, W=Сверху)")]
    public Vector4 exitButtonPadding = new Vector4(0f, 0f, 0f, 0f); // Отступы клика кнопки Выход
    [Tooltip("Отступы внутрь для клик-зоны Кнопки НАЗАД (X=Слева, Y=Снизу, Z=Справа, W=Сверху)")]
    public Vector4 backButtonPadding = new Vector4(0f, 0f, 0f, 0f); // Отступы клика кнопки Назад

    [Header("Настройки Дня и Ночи (Day/Night Blending)")]
    [Tooltip("Режим смены дня и ночи:\n- AutomaticPingPong: Плавное качание туда-обратно\n- RealTimeClock: Привязка к реальному времени компьютера\n- Manual: Смена происходит вручную (из инспектора или внешних скриптов)")]
    public DayNightCycleType cycleType = DayNightCycleType.AutomaticPingPong; // Тип смены дня и ночи
    [Tooltip("Картинка Дневного Фона (Day Background Image)")]
    public Image dayBackgroundImage; // Изображение дневного фона
    [Tooltip("Картинка Ночного Фона (Night Background Image)")]
    public Image nightBackgroundImage; // Изображение ночного фона
    [Tooltip("Включить автоматическую плавную смену суток в меню (Устаревшее, используйте cycleType)")]
    public bool autoCycleBackgrounds = true; // Авто-смена суток
    [Tooltip("Скорость перехода (чем выше, тем быстрее меняются день и ночь)")]
    public float dayNightCycleSpeed = 0.15f; // Скорость смены времени суток
    [Tooltip("Ручное смешивание (0 - чистый день, 1 - чистая ночь)")]
    [Range(0f, 1f)]
    public float dayNightBlendFactor = 0f; // Коэффициент смешивания дня и ночи (0..1)

    [Header("Элементы Анимации и Параллакса")]
    [Tooltip("Объект названия игры (для эффекта парения)")]
    public RectTransform gameTitleText; // Трансформ заголовка для эффекта парения
    [Tooltip("Слой заднего фона для параллакса")]
    public RectTransform backgroundLayer; // Слой фона для параллакса
    [Tooltip("Сила параллакса")]
    public float parallaxStrength = 20f; // Сила смещения параллакса
    [Tooltip("Скорость плавного парения заголовка")]
    public float titleAnimSpeed = 3f; // Скорость колебания заголовка

    private Vector2 bgStartPos; // Начальная позиция фона
    private float titleTimer = 0f; // Таймер анимации заголовка
    private float titleStartY = 0f; // Начальная высота Y заголовка
    private bool cycleDirectionUp = true; // Направление перехода цикла дня/ночи

    private void Start() // Инициализация параметров меню и запуск темы
    {
        // Поддержка совместимости со старыми сценами
        if (!autoCycleBackgrounds && cycleType == DayNightCycleType.AutomaticPingPong) // Проверка устаревшего флага
        {
            cycleType = DayNightCycleType.Manual; // Переключение в ручной режим
        }

        if (backgroundLayer != null) // Если слой фона задан
        {
            bgStartPos = backgroundLayer.anchoredPosition; // Запоминаем исходную позицию фона
        }

        if (gameTitleText != null) // Если заголовок задан
        {
            titleStartY = gameTitleText.anchoredPosition.y; // Запоминаем исходную высоту заголовка
        }

        UpdateBackgroundBlending(); // Обновление прозрачности дня и ночи
        SetupListeners(); // Привязка обработчиков нажатий на кнопки
        ShowPanel(mainMenuPanel); // Открытие главного экрана меню

        // Автоматически запускаем музыку меню через SettingsManager
        if (SettingsManager.Instance != null) // Проверка менеджера настроек
        {
            SettingsManager.Instance.PlayThemeForActiveScene(); // Запуск фоновой музыки для меню
        }
    }

    private void Update() // Анимация заголовка, параллакс и смена дня/ночи
    {
        // 1. Анимация парения заголовка (Легкое дыхание)
        if (gameTitleText != null) // Если заголовок существует
        {
            titleTimer += Time.deltaTime * titleAnimSpeed; // Инкремент таймера парения
            float offset = Mathf.Sin(titleTimer) * 12f; // Синусоидальное смещение по высоте
            gameTitleText.anchoredPosition = new Vector2(gameTitleText.anchoredPosition.x, titleStartY + offset); // Применение смещения
        }

        // 2. Интерактивный Параллакс фона за счет наклона мыши
        if (backgroundLayer != null) // Если слой фона существует
        {
            Vector2 mousePos = Vector2.zero; // Позиция мыши
            bool gotMouse = false; // Флаг считывания
#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM
            try
            {
                if (Mouse.current != null) // Если New Input System активен
                {
                    mousePos = Mouse.current.position.ReadValue(); // Чтение координат
                    gotMouse = true; // Успех
                }
            }
            catch {}
#endif
            if (!gotMouse) // Резервный Input Manager
            {
                try
                {
                    mousePos = Input.mousePosition; // Получение координат мыши
                }
                catch (System.InvalidOperationException)
                {
                    // Игнорируем ошибку, если Input Manager полностью отключен в Player Settings
                    mousePos = new Vector2(Screen.width / 2f, Screen.height / 2f); // Центр экрана по умолчанию
                }
            }

            float normX = (mousePos.x / Screen.width) - 0.5f; // Нормализация координаты X (-0.5..0.5)
            float normY = (mousePos.y / Screen.height) - 0.5f; // Нормализация координаты Y (-0.5..0.5)

            Vector2 targetPos = bgStartPos + new Vector2(normX * parallaxStrength, normY * parallaxStrength); // Целевое смещение параллакса
            backgroundLayer.anchoredPosition = Vector2.Lerp(backgroundLayer.anchoredPosition, targetPos, Time.deltaTime * 5f); // Плавная интерполяция
        }

        // 3. Плавный цикл смены дня и ночи в зависимости от выбранного режима
        if (cycleType == DayNightCycleType.AutomaticPingPong) // Автоматический цикл
        {
            if (cycleDirectionUp) // Фаза наступления ночи
            {
                dayNightBlendFactor += Time.deltaTime * dayNightCycleSpeed; // Нарастание ночи
                if (dayNightBlendFactor >= 1f) // Достижение максимума
                {
                    dayNightBlendFactor = 1f; // Ограничение
                    cycleDirectionUp = false; // Смена направления на день
                }
            }
            else // Фаза наступления дня
            {
                dayNightBlendFactor -= Time.deltaTime * dayNightCycleSpeed; // Нарастание дня
                if (dayNightBlendFactor <= 0f) // Достижение минимума
                {
                    dayNightBlendFactor = 0f; // Ограничение
                    cycleDirectionUp = true; // Смена направления на ночь
                }
            }
        }
        else if (cycleType == DayNightCycleType.RealTimeClock) // Реальное время ПК
        {
            // Получаем часы и минуты реального компьютера
            System.DateTime now = System.DateTime.Now; // Текущее время системы
            float hour = (float)now.Hour + (float)now.Minute / 60f; // Отрезок от 0 до 24
            
            // Формула плавной гармонической волны:
            // В 12:00 -> cos(PI) = -1.0 -> dayNightBlendFactor = 0.0 (Чистый день)
            // В 00:00 -> cos(0)  = 1.0  -> dayNightBlendFactor = 1.0 (Чистая ночь)
            // В 06:00 -> cos(PI/2) = 0.0 -> dayNightBlendFactor = 0.5 (Рассвет / Сумерки)
            // В 18:00 -> cos(3PI/2)= 0.0 -> dayNightBlendFactor = 0.5 (Закат / Полумрак)
            float angle = (hour / 24f) * 2f * Mathf.PI; // Угол в радианах
            dayNightBlendFactor = (Mathf.Cos(angle) + 1f) / 2f; // Коэффициент дня/ночи
        }
        // Если выбран режим DayNightCycleType.Manual, мы ничего не делаем автоматически.
        // Значение dayNightBlendFactor полностью контролируется вручную в инспекторе или из других скриптов.

        UpdateBackgroundBlending(); // Обновление альфы слоев фона
    }

    /// <summary>
    /// Обновляет прозрачность дневного и ночного слоев на основе dayNightBlendFactor (0 = чистый день, 1 = чистая ночь)
    /// </summary>
    public void UpdateBackgroundBlending() // Смешивание прозрачности дневного и ночного фона
    {
        if (dayBackgroundImage != null) // Если дневной фон задан
        {
            Color c = dayBackgroundImage.color; // Текущий цвет
            // Дневной фон плавно затухает от 1 до 0
            c.a = 1f - dayNightBlendFactor; // Расчет альфы дня
            dayBackgroundImage.color = c; // Применение цвета
        }

        if (nightBackgroundImage != null) // Если ночной фон задан
        {
            Color c = nightBackgroundImage.color; // Текущий цвет
            // Ночной фон плавно проявляется от 0 до 1
            c.a = dayNightBlendFactor; // Расчет альфы ночи
            nightBackgroundImage.color = c; // Применение цвета
        }
    }

    private void SetupListeners() // Настройка слушателей событий кнопок интерфейса
    {
        if (startButton != null) // Кнопка "Играть"
        {
            startButton.onClick.RemoveAllListeners(); // Сброс старых слушателей
            startButton.onClick.AddListener(OnStartPressed); // Добавление перехода в игру
            SetAlphaHitThreshold(startButton, 0.5f); // Настройка чувствительности к прозрачности
        }
        if (settingsButton != null) // Кнопка "Настройки"
        {
            settingsButton.onClick.RemoveAllListeners(); // Сброс старых слушателей
            settingsButton.onClick.AddListener(OnSettingsPressed); // Добавление открытия настроек
            SetAlphaHitThreshold(settingsButton, 0.5f); // Настройка чувствительности
        }
        if (exitButton != null) // Кнопка "Выход"
        {
            exitButton.onClick.RemoveAllListeners(); // Сброс старых слушателей
            exitButton.onClick.AddListener(OnExitPressed); // Добавление выхода из приложения
            SetAlphaHitThreshold(exitButton, 0.5f); // Настройка чувствительности
        }

        // Автоматический поиск кнопки Назад в панели настроек, если она не задана вручную
        if (settingsBackButton == null && settingsPanel != null) // Авто-поиск кнопки "Назад"
        {
            Button[] buttons = settingsPanel.GetComponentsInChildren<Button>(true); // Поиск дочерних кнопок
            foreach (var b in buttons) // Перебор кнопок
            {
                string nameLower = b.name.ToLower(); // Имя в нижнем регистре
                if (nameLower.Contains("back") || nameLower.Contains("назад") || nameLower.Contains("close") || nameLower.Contains("return") || nameLower.Contains("geri")) // Поиск по ключевым словам
                {
                    settingsBackButton = b; // Назначение кнопки
                    break; // Прерывание поиска
                }
            }
        }

        if (settingsBackButton != null) // Кнопка "Назад"
        {
            settingsBackButton.onClick.RemoveAllListeners(); // Сброс слушателей
            settingsBackButton.onClick.AddListener(OnBackPressed); // Возврат в главное меню
            SetAlphaHitThreshold(settingsBackButton, 0.5f); // Настройка чувствительности
        }

        // Применяем отступы кликабельной зоны (Raycast Padding)
        ApplyRaycastPadding(startButton, startButtonPadding); // Отступы для кнопки старта
        ApplyRaycastPadding(settingsButton, settingsButtonPadding); // Отступы для кнопки настроек
        ApplyRaycastPadding(exitButton, exitButtonPadding); // Отступы для кнопки выхода
        ApplyRaycastPadding(settingsBackButton, backButtonPadding); // Отступы для кнопки возврата
    }

    private void ApplyRaycastPadding(Button button, Vector4 padding) // Метод установки внутренних отступов клика
    {
        if (button == null) return; // Пропуск если кнопка пуста
        Image img = button.GetComponent<Image>(); // Получение Image компонента
        if (img != null) // Если Image найден
        {
            try
            {
                img.raycastPadding = padding; // Применение внутренних отступов клика
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ALCHEMIST MENU] Не удалось установить raycastPadding для {button.name}: {ex.Message}"); // Предупреждение
            }
        }
    }

    private void SetAlphaHitThreshold(Button button, float threshold) // Настройка порога прозрачности клика по спрайту
    {
        if (button == null) return; // Пропуск пустых ссылок
        Image img = button.GetComponent<Image>(); // Image компонент
        if (img != null && img.sprite != null) // Проверка наличия спрайта
        {
            // Пропускаем стандартные спрайты Unity, чтобы избежать ошибки в консоли
            string spriteName = img.sprite.name; // Имя спрайта
            if (spriteName == "UISprite" || spriteName == "Background" || spriteName == "Knob" || spriteName == "Checkmark" || spriteName == "InputPen") // Стандартные системные спрайты
            {
                return; // Пропуск
            }

            try
            {
                img.alphaHitTestMinimumThreshold = threshold; // Установка порога прозрачности
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ALCHEMIST MENU] Не удалось установить alphaHitTestMinimumThreshold для {button.name}. " +
                                 $"Убедитесь, что в настройках импорта текстуры '{img.sprite.texture.name}' включена галочка 'Read/Write' в Unity Inspector! Ошибка: {ex.Message}"); // Предупреждение
            }
        }
    }

    private void ShowPanel(GameObject panel) // Переключение активной панели меню
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panel == mainMenuPanel); // Включение/выключение главного меню
        if (settingsPanel != null) // Настройки
        {
            settingsPanel.SetActive(panel == settingsPanel); // Включение/выключение панели настроек
            if (panel == settingsPanel) // Если открываются настройки
            {
                // Адаптивное масштабирование панели настроек под разрешение экрана, чтобы ничего не обрезалось по высоте
                RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>(); // Трансформ панели
                if (settingsRect != null) // Проверка трансформа
                {
                    // Проверяем высоту экрана или родительского Canvas
                    float screenHeight = Screen.height; // Высота экрана
                    Canvas parentCanvas = settingsPanel.GetComponentInParent<Canvas>(); // Родительский Canvas
                    if (parentCanvas != null && parentCanvas.GetComponent<RectTransform>() != null) // Проверка Canvas
                    {
                        screenHeight = parentCanvas.GetComponent<RectTransform>().rect.height; // Высота Canvas
                    }

                    // Если экран ландшафтный (ширина > высоты) или высота экрана меньше 850 пикселей
                    if (Screen.width > Screen.height || screenHeight < 850f) // Условие мобильного / компактного экрана
                    {
                        // Подбираем оптимальный масштаб: чем меньше высота экрана, тем меньше масштаб
                        float targetScale = Mathf.Clamp(screenHeight / 850f, 0.65f, 0.9f); // Расчет масштаба
                        
                        settingsRect.localScale = new Vector3(targetScale, targetScale, 1f); // Применение масштаба
                        
                        // Слегка приподнимаем панель, чтобы компенсировать уменьшение размера снизу
                        settingsRect.anchoredPosition = new Vector2(settingsRect.anchoredPosition.x, 10f); // Смещение по Y
                    }
                    else
                    {
                        settingsRect.localScale = Vector3.one; // Стандартный масштаб 1:1
                        settingsRect.anchoredPosition = Vector2.zero; // Центрирование
                    }
                }
            }
        }
    }

    private bool isStartingGame = false; // Флаг: игра уже запускается

    public void OnStartPressed() // Обработчик нажатия кнопки "Играть"
    {
        if (isStartingGame) // Защита от спама кликов
        {
            return; // Выход
        }
        isStartingGame = true; // Установка флага старта

        Debug.Log("<color=#FFFF00>[FATE DIAGNOSTIC]</color> НАЖАТА КНОПКА СТАРТ (OnStartPressed) в Menu_Game!"); // Лог нажатия

        // Деактивируем кнопку, чтобы избежать повторных нажатий
        if (startButton != null) // Кнопка старта
        {
            startButton.interactable = false; // Блокировка кнопки
        }
        
        // Автоматическая загрузка или старт новой игры в единственный слот 0
        if (PlayerPrefs.HasKey("Alchemist_Slot_Used_0")) // Если сохранение уже есть
        {
            Debug.Log("[FATE DIAGNOSTIC] Найдено существующее сохранение в слоте 0. Загружаем данные без активации сцены..."); // Лог
            SaveGameSystem.Load(0, false); // Загрузка данных без переключения сцены
        }
        else // Если это первый запуск
        {
            Debug.Log("[FATE DIAGNOSTIC] Сохранений нет. Инициализируем новую игру..."); // Лог
            SaveGameSystem.DeleteSave(0); // Очистка слота 0
            SaveGameSystem.CurrentData = new SaveGameSystem.SaveData(); // Создание чистого сохранения
            SaveGameSystem.CurrentData.saveName = Translator.GetText9(
                "Кот-Алхимик", "Alchemist Cat", "Alchemist Cat", "Chat Alchimiste", "Gato Alquimista", "Gato Alquimista", "錬金術師の猫", "연금술사 고양이", "炼金猫"
            ); // Локализация названия кота
            SaveGameSystem.Save(0); // Первичное сохранение
        }

        // Запуск сцены лаборатории (Индекс 1) с использованием нашего экрана загрузки
        if (LoadingScreenManager.Instance != null) // Если менеджер загрузки доступен
        {
            Debug.Log("[FATE DIAGNOSTIC] Найден LoadingScreenManager.Instance! Запускаем сцену 1 асинхронно через него..."); // Лог
            LoadingScreenManager.Instance.LoadScene(1); // Плавная загрузка сцены 1
        }
        else
        {
            Debug.LogError("[FATE DIAGNOSTIC] ОШИБКА: LoadingScreenManager.Instance не найден! Загружаем сцену 1 НАПРЯМУЮ и мгновенно."); // Лог ошибки
            SceneManager.LoadScene(1); // Мгновенная загрузка сцены 1
        }
    }

    public void OnSettingsPressed() // Обработчик нажатия кнопки "Настройки"
    {
        ShowPanel(settingsPanel); // Открытие панели настроек
        if (SettingsManager.Instance != null) // Проверка менеджера настроек
        {
            SettingsManager.Instance.BindUIElements(); // Привязка ползунков звука к настройкам
        }
    }

    public void OnBackPressed() // Обработчик нажатия кнопки "Назад"
    {
        ShowPanel(mainMenuPanel); // Возврат в главное меню
        if (SettingsManager.Instance != null) // Проверка менеджера настроек
        {
            SettingsManager.Instance.PlayClickSound(); // Воспроизведение звука клика
        }
    }

    public void OnExitPressed() // Обработчик нажатия кнопки "Выход"
    {
        Debug.Log("[ALCHEMIST MENU] Выход из игры..."); // Лог выхода
        Application.Quit(); // Закрытие приложения
    }
}
