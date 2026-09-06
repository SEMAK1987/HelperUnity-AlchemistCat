using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Скрипт глобальной системы сохранения и загрузки прогресса Кота-Алхимика.
/// </summary>
public static class SaveGameSystem
{
    public static SaveData CurrentData = new SaveData(); // Текущие активные данные сохранения в оперативной памяти
    public static bool IsStartedFromMenu = false; // Флаг: была ли игра запущена из главного меню

    [Serializable]
    public class SaveData
    {
        [Header("Основное")]
        public string saveName = "Кот-Алхимик"; // Название слота сохранения
        public int currentSceneIndex = 1; // Номер сохраненной сцены Unity
        public string saveDateTime = ""; // Дата и время создания файла сохранения
        
        [Header("Экономика и Валюта")]
        public int gold = 100; // Количество золотых монет
        public int crystals = 0; // Количество кристаллов
        public int vipXP = 0; // Опыт VIP системы
        public int daysActive = 0; // Число активных игровых дней

        [Header("Развитие Кота")]
        public int catLevel = 1; // Уровень персонажа
        public int currentXP = 0; // Текущий опыт
        public int cauldronLevel = 1; // Уровень котла
        public int potionsBrewed = 0; // Сварено зелий

        [Header("Миниигры и Разблокировки")]
        public bool unlockedDarts = false; // Доступность игры в дартс
        public bool unlockedMouseCatch = false; // Доступность игры "Поймай мышь"
    }

    /// <summary>
    /// Полное сохранение игры в определенный слот.
    /// </summary>
    public static void Save(int slotIndex) // Метод сохранения игры по номеру слота
    {
        CurrentData.currentSceneIndex = SceneManager.GetActiveScene().buildIndex; // Фиксация индекса текущей сцены
        CurrentData.saveDateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm"); // Форматирование текущей даты и времени

        // Синхронизируем с текущим состоянием GameManager, если он запущен
        if (GameManager.Instance != null) // Проверка активного GameManager
        {
            CurrentData.gold = GameManager.Instance.gold; // Сохранение запаса золота
            CurrentData.crystals = GameManager.Instance.crystals; // Сохранение запаса кристаллов
            CurrentData.vipXP = GameManager.Instance.vipXP; // Сохранение очков VIP
            CurrentData.daysActive = GameManager.Instance.daysActive; // Сохранение игровых дней
            CurrentData.catLevel = GameManager.Instance.catLevel; // Сохранение уровня кота
            CurrentData.currentXP = GameManager.Instance.currentXP; // Сохранение текущего опыта
            CurrentData.cauldronLevel = GameManager.Instance.cauldronLevel; // Сохранение уровня котла
            CurrentData.potionsBrewed = GameManager.Instance.potionsBrewed; // Сохранение количества зелий
            CurrentData.unlockedDarts = GameManager.Instance.unlockedDarts; // Сохранение доступа к дартсу
            CurrentData.unlockedMouseCatch = GameManager.Instance.unlockedMouseCatch; // Сохранение доступа к ловле мышей
        }

        string json = JsonUtility.ToJson(CurrentData, true); // Сериализация структуры в формат JSON

        // Краткое описание для отображения в UI слота
        string langPrefix = GetLanguageInfoPrefix(); // Получение локализованного префикса "Ур."
        string infoText = $"{CurrentData.saveDateTime} | {langPrefix} {CurrentData.catLevel} | {SceneManager.GetActiveScene().name}"; // Формирование превью слота

        PlayerPrefs.SetInt("Alchemist_Slot_Used_" + slotIndex, 1); // Пометка: слот занят
        PlayerPrefs.SetInt("Alchemist_Slot_Scene_" + slotIndex, CurrentData.currentSceneIndex); // Запись номера сцены
        PlayerPrefs.SetString("Alchemist_Slot_Info_" + slotIndex, infoText); // Запись превью данных
        PlayerPrefs.SetString("Alchemist_Slot_Data_" + slotIndex, json); // Запись JSON строки прогресса
        PlayerPrefs.Save(); // Сохранение на диск

        Debug.Log($"[ALCHEMIST SAVE] Игра успешно СОХРАНЕНА в Слот {slotIndex}. Данные: {infoText}"); // Лог успеха
    }

    /// <summary>
    /// Загрузка игры из слота.
    /// </summary>
    public static bool Load(int slotIndex, bool loadScene = true) // Метод загрузки сохранения
    {
        if (!PlayerPrefs.HasKey("Alchemist_Slot_Used_" + slotIndex)) // Проверка существования слота
        {
            Debug.LogWarning($"[ALCHEMIST LOAD] Попытка загрузки пустого слота {slotIndex}"); // Лог предупреждения
            return false; // Выход с ошибкой
        }

        string json = PlayerPrefs.GetString("Alchemist_Slot_Data_" + slotIndex); // Чтение строки данных
        if (string.IsNullOrEmpty(json)) // Проверка на пустые данные
        {
            Debug.LogError($"[ALCHEMIST LOAD] Пустые данные сохранения в слоте {slotIndex}"); // Лог ошибки
            return false; // Выход с ошибкой
        }

        try
        {
            CurrentData = JsonUtility.FromJson<SaveData>(json); // Десериализация JSON в объект данных
            
            // Записываем данные в GameManager
            if (GameManager.Instance != null) // Если игровой менеджер активен
            {
                GameManager.Instance.gold = CurrentData.gold; // Восстановление золота
                GameManager.Instance.crystals = CurrentData.crystals; // Восстановление кристаллов
                GameManager.Instance.vipXP = CurrentData.vipXP; // Восстановление VIP очков
                GameManager.Instance.daysActive = CurrentData.daysActive; // Восстановление дней
                GameManager.Instance.catLevel = CurrentData.catLevel; // Восстановление уровня кота
                GameManager.Instance.currentXP = CurrentData.currentXP; // Восстановление опыта
                GameManager.Instance.cauldronLevel = CurrentData.cauldronLevel; // Восстановление котла
                GameManager.Instance.potionsBrewed = CurrentData.potionsBrewed; // Восстановление числа зелий
                GameManager.Instance.unlockedDarts = CurrentData.unlockedDarts; // Восстановление статуса дартса
                GameManager.Instance.unlockedMouseCatch = CurrentData.unlockedMouseCatch; // Восстановление статуса миниигры
                GameManager.Instance.SyncUI(); // Синхронизация интерфейса
            }

            IsStartedFromMenu = true; // Установка флага старта из меню
            Debug.Log($"[ALCHEMIST LOAD] Успешная загрузка слота {slotIndex}. Уровень Кота: {CurrentData.catLevel}"); // Лог успеха

            if (loadScene) // Если требуется загрузить сцену
            {
                Debug.Log($"[FATE DIAGNOSTIC] Запрошена загрузка сцены {CurrentData.currentSceneIndex} из SaveGameSystem..."); // Диагностика
                if (LoadingScreenManager.Instance != null) // Проверка менеджера загрузки
                {
                    Debug.Log($"[FATE DIAGNOSTIC] Загружаем сцену {CurrentData.currentSceneIndex} через LoadingScreenManager.Instance..."); // Лог
                    LoadingScreenManager.Instance.LoadScene(CurrentData.currentSceneIndex); // Плавная загрузка через корутину
                }
                else
                {
                    Debug.LogWarning($"[FATE DIAGNOSTIC] LoadingScreenManager.Instance не найден! Загружаем сцену {CurrentData.currentSceneIndex} напрямую."); // Лог
                    SceneManager.LoadScene(CurrentData.currentSceneIndex); // Прямая загрузка сцены Unity
                }
            }
            return true; // Успешное завершение
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ALCHEMIST LOAD] Критическая ошибка при загрузке слота {slotIndex}: {ex}"); // Лог ошибки
            return false; // Выход с ошибкой
        }
    }

    /// <summary>
    /// Очистка слота сохранения.
    /// </summary>
    public static void DeleteSave(int slotIndex) // Метод удаления сохранения из слота
    {
        PlayerPrefs.DeleteKey("Alchemist_Slot_Used_" + slotIndex); // Удаление ключа использования
        PlayerPrefs.DeleteKey("Alchemist_Slot_Scene_" + slotIndex); // Удаление ключа сцены
        PlayerPrefs.DeleteKey("Alchemist_Slot_Info_" + slotIndex); // Удаление превью
        PlayerPrefs.DeleteKey("Alchemist_Slot_Data_" + slotIndex); // Удаление JSON данных
        PlayerPrefs.Save(); // Сохранение изменений
        Debug.Log($"[ALCHEMIST SAVE] Слот сохранения {slotIndex} успешно очищен."); // Лог завершения
    }

    private static string GetLanguageInfoPrefix() // Определение текстового префикса уровня по языку
    {
        int lang = PlayerPrefs.GetInt("Alchemist_Language", 0); // Получение активного языка
        switch (lang) // Выбор языка
        {
            case 0: return "Ур."; // Русский префикс
            case 2: return "Seviye"; // Турецкий префикс уровня
            case 7: return "레벨"; // Корейский префикс
            case 6: return "レベル"; // Японский префикс
            case 8: return "等级"; // Китайский префикс
            default: return "Lvl"; // Английский / стандартный префикс
        }
    }
}
