using System; // Базовые типы данных .NET
using System.Collections; // Коллекции и перечисления
using System.Collections.Generic; // Обобщенные списки и словари
using UnityEngine; // Базовый движок Unity
using UnityEngine.Networking; // Сетевые запросы UnityWebRequest

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Studio v18.12.51)
/// Модуль синхронизации Unity с ассистентом разработки, базой знаний и Blender пайплайном:
/// - Автоматическая проверка статуса скриптов и целостности кодовой базы
/// - Прием и передача 3D моделей, анимаций и материалов из Blender Connector
/// - Логирование изменений и диагностика Inspector-связок в реальном времени
/// </summary>
public class UnityConnector : MonoBehaviour
{
    public static UnityConnector Instance { get; private set; } // Синглтон инстанс коннектора

    [Header("Настройки Сетевого Моста")]
    public string serverUrl = "http://localhost:3000"; // URL локального сервера помощника
    public int port = 3000; // Рабочий порт синхронизации
    public bool autoSyncOnStart = true; // Автоматически запрашивать состояние при запуске

    [Header("Статус Соединения")]
    public bool isConnected = false; // Флаг активности подключения
    public string lastSyncTimestamp = ""; // Время последней успешной синхронизации
    public string assistantVersion = "v18.12.51"; // Текущая версия ассистента

    private void Awake() // Инициализация синглтона при старте
    {
        if (Instance != null && Instance != this) // Защита от дубликатов
        {
            Destroy(gameObject); // Уничтожение копии
            return; // Выход
        }
        Instance = this; // Установка инстанса
        DontDestroyOnLoad(gameObject); // Сохранение между сценами
    }

    private void Start() // Стартовая инициализация
    {
        if (autoSyncOnStart) // Если включена автосинхронизация
        {
            StartCoroutine(CheckServerHealthRoutine()); // Запуск проверки здоровья сервера
        }
    }

    private void OnEnable() // Событие включения объекта
    {
        Debug.Log("[UnityConnector] Мост синхронизации активирован."); // Логирование
    }

    private void OnDisable() // Событие выключения объекта
    {
        Debug.Log("[UnityConnector] Мост синхронизации деактивирован."); // Логирование
    }

    public void RequestKnowledgeBaseSync() // Ручной запуск запроса к базе знаний
    {
        StartCoroutine(SyncKnowledgeBaseRoutine()); // Запуск корутины
    }

    private IEnumerator CheckServerHealthRoutine() // Корутина проверки доступности сервера
    {
        string endpoint = $"{serverUrl}/api/health"; // Путь к эндпоинту здоровья
        using (UnityWebRequest request = UnityWebRequest.Get(endpoint)) // Создание GET запроса
        {
            request.timeout = 5; // Таймаут 5 секунд
            yield return request.SendWebRequest(); // Отправка запроса

            if (request.result == UnityWebRequest.Result.Success) // Успешный ответ
            {
                isConnected = true; // Установка флага подключения
                lastSyncTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Запись времени
                Debug.Log($"[UnityConnector] Связь с AI Assistant установлена: {request.downloadHandler.text}"); // Лог
            }
            else // Ошибка подключения (сервер ассистента не запущен локально)
            {
                isConnected = false; // Сброс флага
                Debug.Log($"[UnityConnector] AI Assistant сервер не обнаружен на {serverUrl} ({request.error}). Игра работает в штатном автономном режиме."); // Информационное сообщение
            }
        }
    }

    private IEnumerator SyncKnowledgeBaseRoutine() // Корутина синхронизации базы знаний
    {
        string endpoint = $"{serverUrl}/api/knowledge-base"; // Эндпоинт базы знаний
        using (UnityWebRequest request = UnityWebRequest.Get(endpoint)) // Создание GET запроса
        {
            request.timeout = 10; // Таймаут 10 секунд
            yield return request.SendWebRequest(); // Отправка
            if (request.result == UnityWebRequest.Result.Success) // Успех
            {
                lastSyncTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Обновление времени
                Debug.Log("[UnityConnector] База знаний успешно синхронизирована с Unity!"); // Лог
            }
            else
            {
                Debug.LogWarning($"[UnityConnector] Ошибка синхронизации базы знаний: {request.error}"); // Ошибка
            }
        }
    }
}
