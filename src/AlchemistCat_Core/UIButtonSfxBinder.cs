using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Автоматический биндер неоновых и алхимических звуков на все кнопки в сцене.
/// Сканирует активную иерархию и добавляет UIButtonHoverEffect.
/// </summary>
public class UIButtonSfxBinder : MonoBehaviour
{
    public static UIButtonSfxBinder Instance { get; private set; } // Статический синглтон биндера звуков на кнопки

    [Header("Настройки Биндера")]
    public bool scanOnStart = true; // Сканировать ли все кнопки при старте игры
    public bool scanOnSceneLoaded = true; // Сканировать ли сцену при каждой смене уровня

    private void Awake() // Инициализация синглтона и DontDestroyOnLoad
    {
        if (Instance == null) // Если экземпляр еще не создан
        {
            Instance = this; // Инициализация единственного экземпляра
            DontDestroyOnLoad(gameObject); // Сохраняем объект между сценами
        }
        else // Если дубликат
        {
            Destroy(gameObject); // Уничтожаем дубликат
            return; // Выход
        }
    }

    private void OnEnable() // Подписка на события загрузки сцен
    {
        if (scanOnSceneLoaded) // Если включено сканирование при загрузке
        {
            SceneManager.sceneLoaded += OnSceneLoaded; // Подписка на событие загрузки новой сцены
        }
    }

    private void OnDisable() // Отписка от событий
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Отписка от событий загрузки сцен
    }

    private void Start() // Стартовое сканирование кнопок
    {
        if (scanOnStart) // Если включено сканирование на старте
        {
            ScanAndBindAllButtons(); // Первичное сканирование и привязка звуков ко всем кнопкам
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // Обработчик завершения загрузки сцены
    {
        ScanAndBindAllButtons(); // Запуск автоматической привязки звуков к новым кнопкам
    }

    /// <summary>
    /// Сканирует всю сцену и вешает компоненты озвучки на кнопки, у которых их еще нет.
    /// </summary>
    public void ScanAndBindAllButtons() // Метод сканирования и добавления эффектов
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>(); // Поиск всех кнопок в сцене
        int boundCount = 0; // Счетчик вновь озвученных кнопок

        foreach (Button btn in buttons) // Перебор всех найденных кнопок
        {
            // Пропускаем префабы в ассетах
            if (btn.gameObject.scene.name == null) continue; // Игнорирование объектов вне сцены

            // Проверяем наличие эффекта наведения
            UIButtonHoverEffect effect = btn.GetComponent<UIButtonHoverEffect>(); // Проверка существующего компонента
            if (effect == null) // Если эффекта еще нет на кнопке
            {
                // Добавляем скрипт hover эффекта
                effect = btn.gameObject.AddComponent<UIButtonHoverEffect>(); // Добавление компонента анимации и звука
                effect.playSfxOnHover = true; // Включение звука наведения
                effect.playSfxOnClick = true; // Включение звука клика
                boundCount++; // Увеличение счетчика обработанных кнопок
            }
        }

        Debug.Log($"[ALCHEMIST SFX BINDER] Сканирование завершено. Озвучено новых кнопок: {boundCount}"); // Лог результатов
    }
}
