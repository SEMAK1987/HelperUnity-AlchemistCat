using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Контроллер загрузочного экрана с веселыми кошачьими цитатами и советами.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; } // Статический синглтон загрузочного экрана

    [Header("UI Ссылки")]
    public GameObject loadingPanel; // Панель загрузочного экрана
    public Slider progressBar; // Полоса прогресса загрузки сцены
    public TextMeshProUGUI progressText; // Текстовый процент загрузки ("75%")
    public TextMeshProUGUI funnyQuoteText; // Текст кошачьих алхимических цитат и подсказок
    public Image kittenSilhouette; // Силуэт котенка для анимации проявления

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // Назначение глобального синглтона
            DontDestroyOnLoad(gameObject); // Сохранение при переходах между сценами
        }
        else
        {
            Destroy(gameObject); // Уничтожение дубликата менеджера
            return;
        }
    }

    private void Start()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false); // Скрываем загрузочный экран при старте сцены
    }

    private bool isLoading = false; // Флаг: выполняется ли загрузка сцены в данный момент

    /// <summary>
    /// Асинхронный запуск загрузки любой сцены по индексу.
    /// </summary>
    public void LoadScene(int sceneBuildIndex) // Метод запуска загрузки по индексу сцены
    {
        if (isLoading) // Защита от повторного запуска загрузки
        {
            Debug.LogWarning($"[FATE DIAGNOSTIC] Загрузка уже активна! Блокируем дублирующий вызов LoadScene(индекс: {sceneBuildIndex})"); // Предупреждение о повторе
            return; // Выход
        }
        isLoading = true; // Установка флага процесса загрузки
        Debug.Log($"<color=#00FFCC>[FATE DIAGNOSTIC]</color> Вызван публичный метод LoadScene(индекс: {sceneBuildIndex}). Запускаем корутину."); // Лог старта
        StartCoroutine(LoadAsynchronously(sceneBuildIndex)); // Запуск асинхронной корутины
    }

    /// <summary>
    /// Асинхронный запуск загрузки любой сцены по имени.
    /// </summary>
    public void LoadScene(string sceneName) // Метод запуска загрузки по строковому имени сцены
    {
        if (isLoading) // Защита от повторного вызова
        {
            Debug.LogWarning($"[FATE DIAGNOSTIC] Загрузка уже активна! Блокируем дублирующий вызов LoadScene(имя: '{sceneName}')"); // Лог предупреждения
            return; // Выход
        }
        isLoading = true; // Установка флага процесса загрузки
        Debug.Log($"<color=#00FFCC>[FATE DIAGNOSTIC]</color> Вызван публичный метод LoadScene(имя: '{sceneName}'). Запускаем корутину."); // Лог старта
        StartCoroutine(LoadAsynchronouslyByName(sceneName)); // Запуск асинхронной корутины
    }

    private IEnumerator LoadAsynchronouslyByName(string sceneName) // Корутина загрузки сцены по имени
    {
        Debug.Log($"<color=#FF3366>[FATE DIAGNOSTIC]</color> Корутина LoadAsynchronouslyByName НАЧАТА для сцены: '{sceneName}'. Текущий Time.timeScale = {Time.timeScale}"); // Диагностический лог
        
        if (loadingPanel != null) // Проверка наличия UI панели загрузки
        {
            loadingPanel.SetActive(true); // Включение экрана загрузки
            Debug.Log("[FATE DIAGNOSTIC] Панель загрузки активирована (SetActive(true))."); // Лог активации
        }
        else
        {
            Debug.LogError("[FATE DIAGNOSTIC] КРИТИЧЕСКАЯ ОШИБКА: loadingPanel равен NULL! Экрану загрузки нечего показывать."); // Ошибка отсутствия панели
        }

        // Сбрасываем прозрачность силуэта кота в 0 в начале загрузки
        if (kittenSilhouette != null) // Проверка силуэта котенка
        {
            Color c = kittenSilhouette.color; // Текущий цвет
            c.a = 0f; // Начальная прозрачность 0
            kittenSilhouette.color = c; // Применение цвета
            Debug.Log("[FATE DIAGNOSTIC] Прозрачность силуэта кота сброшена в 0."); // Лог сброса
        }
        else
        {
            Debug.LogWarning("[FATE DIAGNOSTIC] Предупреждение: kittenSilhouette равен NULL."); // Предупреждение
        }

        // Показываем случайный совет про зельеварение
        if (funnyQuoteText != null) // Проверка текстового поля для цитат
        {
            string quote = GetRandomCatQuote(); // Получение случайной фразы кота
            funnyQuoteText.text = quote; // Установка текста в UI
            Debug.Log($"[FATE DIAGNOSTIC] Установлен текст совета: '{quote}'"); // Лог цитаты
        }

        AsyncOperation operation = null; // Дескриптор асинхронной операции
        try
        {
            Debug.Log($"[FATE DIAGNOSTIC] Запуск асинхронной загрузки сцены через SceneManager.LoadSceneAsync('{sceneName}')..."); // Лог начала вызова
            operation = SceneManager.LoadSceneAsync(sceneName); // Старт асинхронной загрузки Unity
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FATE DIAGNOSTIC] ИСКЛЮЧЕНИЕ при вызове LoadSceneAsync: {ex.Message}\n{ex.StackTrace}"); // Лог исключения
        }

        if (operation == null) // Если сцена не найдена в Build Settings
        {
            Debug.LogError($"[FATE DIAGNOSTIC] КРИТИЧЕСКАЯ ОШИБКА: AsyncOperation равен NULL! Проверьте, добавлена ли сцена '{sceneName}' в Build Settings (меню File -> Build Settings)."); // Ошибка
            if (loadingPanel != null) loadingPanel.SetActive(false); // Выключение панели
            isLoading = false; // Сброс флага
            yield break; // Прерывание корутины
        }

        operation.allowSceneActivation = false; // Блокировка авто-перехода до окончания анимации
        float visualProgress = 0f; // Переменная плавного визуального прогресса
        float minLoadDuration = 2.5f; // Минимальная длительность показа загрузки
        int loopTicks = 0; // Счетчик тактов цикла

        Debug.Log("[FATE DIAGNOSTIC] Вход в цикл загрузки while (!operation.isDone)..."); // Лог входа в цикл

        while (!operation.isDone) // Цикл ожидания завершения операции
        {
            loopTicks++; // Инкремент счетчика
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f); // Нормализация прогресса (0..1)
            
            // Плавно накапливаем визуальный прогресс с течением времени (используем unscaledDeltaTime)
            float step = Time.unscaledDeltaTime / minLoadDuration; // Шаг сглаживания шкалы
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, step); // Плавное движение к целевому прогрессу

            if (loopTicks <= 5 || loopTicks % 30 == 0) // Логирование ключевых итераций
            {
                Debug.Log($"[FATE DIAGNOSTIC] Итерация #{loopTicks}: RealProgress={operation.progress}, TargetProgress={targetProgress}, VisualProgress={visualProgress}, RealDeltaTime={Time.unscaledDeltaTime}, timeScale={Time.timeScale}"); // Диагностика
            }

            if (progressBar != null) progressBar.value = visualProgress; // Обновление слайдера
            if (progressText != null) progressText.text = $"Загрузка... {(visualProgress * 100f):F0}%"; // Обновление текста процентов

            // Плавно проявляем силуэт кота в соответствии с визуальным прогрессом
            if (kittenSilhouette != null) // Плавное увеличение непрозрачности котенка
            {
                Color c = kittenSilhouette.color; // Текущий цвет
                c.a = visualProgress; // Альфа-канал равен проценту загрузки
                kittenSilhouette.color = c; // Применение цвета
            }

            // Переходим на сцену только если реальная загрузка завершена И шкала доползла до 100%
            if (operation.progress >= 0.9f && visualProgress >= 0.99f) // Проверка полной готовности
            {
                Debug.Log($"[FATE DIAGNOSTIC] УСПЕХ: Загрузка завершена! Ждем полсекунды (Realtime) и активируем сцену."); // Лог перехода
                yield return new WaitForSecondsRealtime(0.5f); // Короткая пауза для плавности
                operation.allowSceneActivation = true; // Разрешение перехода на новую сцену
            }

            yield return null; // Ожидание следующего кадра
        }

        Debug.Log("[FATE DIAGNOSTIC] Выход из цикла корутины LoadAsynchronouslyByName. Загрузка завершена."); // Лог завершения
        if (loadingPanel != null) loadingPanel.SetActive(false); // Скрытие панели
        isLoading = false; // Сброс статуса загрузки
    }

    private IEnumerator LoadAsynchronously(int sceneBuildIndex) // Корутина асинхронной загрузки по индексу
    {
        Debug.Log($"<color=#FF3366>[FATE DIAGNOSTIC]</color> Корутина LoadAsynchronously НАЧАТА для сцены по индексу: {sceneBuildIndex}. Текущий Time.timeScale = {Time.timeScale}"); // Лог старта
        
        if (loadingPanel != null) // Проверка панели
        {
            loadingPanel.SetActive(true); // Включение экрана загрузки
            Debug.Log("[FATE DIAGNOSTIC] Панель загрузки активирована (SetActive(true))."); // Лог включения
        }
        else
        {
            Debug.LogError("[FATE DIAGNOSTIC] КРИТИЧЕСКАЯ ОШИБКА: loadingPanel равен NULL! Экрану загрузки нечего показывать."); // Ошибка
        }

        // Сбрасываем прозрачность силуэта кота в 0 в начале загрузки
        if (kittenSilhouette != null) // Проверка силуэта
        {
            Color c = kittenSilhouette.color; // Текущий цвет
            c.a = 0f; // Начальная прозрачность 0
            kittenSilhouette.color = c; // Применение цвета
            Debug.Log("[FATE DIAGNOSTIC] Прозрачность силуэта кота сброшена в 0."); // Лог
        }
        else
        {
            Debug.LogWarning("[FATE DIAGNOSTIC] Предупреждение: kittenSilhouette равен NULL."); // Предупреждение
        }

        // Показываем случайный совет про зельеварение
        if (funnyQuoteText != null) // Проверка текста цитаты
        {
            string quote = GetRandomCatQuote(); // Получение кошачьего совета
            funnyQuoteText.text = quote; // Установка цитаты в UI
            Debug.Log($"[FATE DIAGNOSTIC] Установлен текст совета: '{quote}'"); // Лог
        }

        AsyncOperation operation = null; // Дескриптор загрузки
        try
        {
            Debug.Log($"[FATE DIAGNOSTIC] Запуск асинхронной загрузки сцены через SceneManager.LoadSceneAsync({sceneBuildIndex})..."); // Лог
            operation = SceneManager.LoadSceneAsync(sceneBuildIndex); // Вызов загрузки сцены по номеру
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FATE DIAGNOSTIC] ИСКЛЮЧЕНИЕ при вызове LoadSceneAsync: {ex.Message}\n{ex.StackTrace}"); // Лог ошибки
        }

        if (operation == null) // Проверка валидности операции
        {
            Debug.LogError($"[FATE DIAGNOSTIC] КРИТИЧЕСКАЯ ОШИБКА: AsyncOperation равен NULL! Проверьте, добавлена ли сцена с индексом {sceneBuildIndex} в Build Settings (меню File -> Build Settings)."); // Ошибка
            if (loadingPanel != null) loadingPanel.SetActive(false); // Выключение панели
            isLoading = false; // Сброс статуса
            yield break; // Выход
        }

        operation.allowSceneActivation = false; // Остановка авто-перехода
        float visualProgress = 0f; // Визуальный прогресс
        float minLoadDuration = 2.5f; // Минимальная длина показа экрана
        int loopTicks = 0; // Счетчик тактов

        Debug.Log("[FATE DIAGNOSTIC] Вход в цикл загрузки while (!operation.isDone)..."); // Лог цикла

        while (!operation.isDone) // Цикл загрузки
        {
            loopTicks++; // Инкремент
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f); // Нормализация до 100%
            
            // Плавно накапливаем визуальный прогресс с течением времени (используем unscaledDeltaTime)
            float step = Time.unscaledDeltaTime / minLoadDuration; // Шаг сглаживания
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, step); // Интерполяция шкалы

            if (loopTicks <= 5 || loopTicks % 30 == 0) // Периодический лог
            {
                Debug.Log($"[FATE DIAGNOSTIC] Итерация #{loopTicks}: RealProgress={operation.progress}, TargetProgress={targetProgress}, VisualProgress={visualProgress}, RealDeltaTime={Time.unscaledDeltaTime}, timeScale={Time.timeScale}"); // Отладка
            }

            if (progressBar != null) progressBar.value = visualProgress; // Значение полосы
            if (progressText != null) progressText.text = $"Загрузка... {(visualProgress * 100f):F0}%"; // Текст прогресса

            // Плавно проявляем силуэт кота в соответствии с визуальным прогрессом
            if (kittenSilhouette != null) // Проявление котенка
            {
                Color c = kittenSilhouette.color; // Текущий цвет
                c.a = visualProgress; // Привязка альфы к шкале прогресса
                kittenSilhouette.color = c; // Применение
            }

            // Переходим на сцену только если реальная загрузка завершена И шкала доползла до 100%
            if (operation.progress >= 0.9f && visualProgress >= 0.99f) // Проверка 100%
            {
                Debug.Log($"[FATE DIAGNOSTIC] УСПЕХ: Загрузка завершена! Ждем полсекунды (Realtime) и активируем сцену."); // Лог
                yield return new WaitForSecondsRealtime(0.5f); // Пауза полсекунды
                operation.allowSceneActivation = true; // Активация сцены
            }

            yield return null; // Ожидание кадра
        }

        Debug.Log("[FATE DIAGNOSTIC] Выход из цикла корутины LoadAsynchronouslyByIndex. Загрузка завершена."); // Лог завершения
        if (loadingPanel != null) loadingPanel.SetActive(false); // Скрытие экрана
        isLoading = false; // Сброс переменной
    }

    private string GetRandomCatQuote() // Метод выборки случайной кошачьей цитаты
    {
        string[][] quotes = { // Мультиязычный массив цитат
            // Russian
            new string[] {
                "Добавляем капельку рыбьего жира в котел...",
                "Натираем когти перед важной миссией...",
                "Прячем валерьянку от строгого наставника...",
                "Учим мышей стоять смирно во время варки...",
                "Проверяем температуру лапками...",
                "Выметаем шерсть из магического зелья..."
            },
            // English
            new string[] {
                "Adding a drop of fish oil to the cauldron...",
                "Sharpening claws before the big brew...",
                "Hiding catnip from the strict mentor...",
                "Teaching mice to sit still during alchemy...",
                "Testing cauldron temperature with paws...",
                "Sweeping fur out of the magic potion..."
            },
            // Turkish
            new string[] {
                "Kazana bir damla balık yağı ekleniyor...",
                "Büyük iksir yapımından önce pençeler keskinleştiriliyor...",
                "Kedi nanesi sert akıl hocasından saklanıyor...",
                "Simya sırasında farelere uslu durmaları öğretiliyor...",
                "Kazan sıcaklığı patilerle test ediliyor...",
                "Sihirli iksirden tüyler temizleniyor..."
            }
        };

        int lang = PlayerPrefs.GetInt("Alchemist_Language", 0); // Получение языка игрока
        if (lang < 0 || lang >= quotes.Length) lang = 1; // Защита от выхода за границы массива
        int index = Random.Range(0, quotes[lang].Length); // Выбор случайного индекса фразы
        return quotes[lang][index]; // Возврат строки цитаты
    }
}
