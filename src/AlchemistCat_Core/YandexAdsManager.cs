using UnityEngine;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Менеджер интеграции Яндекс Игр (Yandex Games SDK) для показа рекламы и начисления бонусов.
/// </summary>
public class YandexAdsManager : MonoBehaviour
{
    public static YandexAdsManager Instance { get; private set; } // Статический синглтон менеджера рекламы Яндекс Игр

    [Header("Настройки")]
    public bool isTestMode = true; // Тестовый режим рекламы (для локальной отладки в редакторе)

    // Импортируем JavaScript функции для связи с Yandex SDK в WebGL
    [DllImport("__Internal")]
    private static extern void ShowYandexRewarded(); // Вызов внешнего JS метода показа рекламы с наградой

    [DllImport("__Internal")]
    private static extern void ShowYandexInterstitial(); // Вызов внешнего JS метода полноэкранного баннера

    private Action rewardedSuccessCallback; // Колбэк успешного завершения просмотра рекламы
    private Action rewardedCloseCallback; // Колбэк закрытия окна рекламы с наградой
    private Action interstitialCloseCallback; // Колбэк закрытия полноэкранной межстраничной рекламы

    private void Awake() // Инициализация синглтона и DontDestroyOnLoad
    {
        if (Instance == null) // Если экземпляр еще не создан
        {
            Instance = this; // Назначение глобального синглтона
            DontDestroyOnLoad(gameObject); // Сохранение объекта между сценами
        }
        else // Если дубликат
        {
            Destroy(gameObject); // Уничтожение дубликата
            return; // Выход
        }
    }

    /// <summary>
    /// Вызов показа вознаграждаемой рекламы (Rewarded Video).
    /// </summary>
    public void ShowRewarded(Action onSuccess, Action onClose = null) // Запуск показа видеорекламы с наградой
    {
        rewardedSuccessCallback = onSuccess; // Сохранение колбэка награды
        rewardedCloseCallback = onClose; // Сохранение колбэка закрытия

        Debug.Log("[YANDEX ADS] Запрос на показ Rewarded видео."); // Лог запроса

#if UNITY_WEBGL && !UNITY_EDITOR
        if (!isTestMode) // Если запущен реальный WebGL билд
        {
            try
            {
                ShowYandexRewarded(); // Вызов JavaScript SDK Яндекса
            }
            catch (Exception ex)
            {
                Debug.LogError($"[YANDEX ADS] Ошибка вызова JS SDK: {ex}"); // Лог ошибки SDK
                SimulateRewardedSuccess(); // Резервный запуск симулятора
            }
        }
        else
        {
            SimulateRewardedSuccess(); // Тестовый режим в браузере
        }
#else
        SimulateRewardedSuccess(); // Режим в редакторе Unity
#endif
    }

    /// <summary>
    /// Вызов показа межстраничной рекламы (Interstitial).
    /// </summary>
    public void ShowInterstitial(Action onClose = null) // Запуск полноэкранной рекламы
    {
        interstitialCloseCallback = onClose; // Сохранение колбэка закрытия

        Debug.Log("[YANDEX ADS] Запрос на показ Interstitial рекламы."); // Лог запроса

#if UNITY_WEBGL && !UNITY_EDITOR
        if (!isTestMode) // Если реальный WebGL
        {
            try
            {
                ShowYandexInterstitial(); // Вызов JS метода баннера
            }
            catch (Exception ex)
            {
                Debug.LogError($"[YANDEX ADS] Ошибка вызова JS SDK: {ex}"); // Лог ошибки
                SimulateInterstitialClose(); // Резервное закрытие
            }
        }
        else
        {
            SimulateInterstitialClose(); // Тестовое закрытие в WebGL
        }
#else
        SimulateInterstitialClose(); // Тестовое закрытие в редакторе
#endif
    }

    #region JS Обратные вызовы (Web -> Unity)
    // Эти методы вызываются из index.html / плагина Yandex SDK JS
    public void OnRewardedSuccess() // Колбэк при завершении просмотра рекламы
    {
        Debug.Log("[YANDEX ADS] Видео просмотрено! Начисляем награду."); // Лог начисления
        rewardedSuccessCallback?.Invoke(); // Вызов пользовательского действия начисления
        rewardedSuccessCallback = null; // Сброс ссылки
    }

    public void OnRewardedClosed() // Колбэк при закрытии окна вознаграждаемой рекламы
    {
        Debug.Log("[YANDEX ADS] Реклама Rewarded закрыта."); // Лог закрытия
        rewardedCloseCallback?.Invoke(); // Вызов действия закрытия
        rewardedCloseCallback = null; // Сброс ссылки
    }

    public void OnInterstitialClosed() // Колбэк при закрытии межстраничного баннера
    {
        Debug.Log("[YANDEX ADS] Межстраничная реклама закрыта."); // Лог закрытия
        interstitialCloseCallback?.Invoke(); // Вызов действия закрытия
        interstitialCloseCallback = null; // Сброс ссылки
    }
    #endregion

    #region Симулятор для Редактора / Тестов
    private void SimulateRewardedSuccess() // Метод имитации просмотра рекламы для тестирования
    {
        Debug.Log("[YANDEX ADS] Имитация успешного просмотра рекламы (Тестовый режим)."); // Лог имитации
        // Начисляем в симуляторе +500 золота
        if (GameManager.Instance != null) // Если игровой менеджер активен
        {
            GameManager.Instance.AddGold(500); // Тестовое начисление 500 золота
        }
        OnRewardedSuccess(); // Вызов успешного колбэка
        OnRewardedClosed(); // Вызов закрытия окна
    }

    private void SimulateInterstitialClose() // Метод имитации закрытия межстраничной рекламы
    {
        Debug.Log("[YANDEX ADS] Имитация закрытия межстраничной рекламы."); // Лог имитации
        OnInterstitialClosed(); // Вызов закрытия
    }
    #endregion
}
