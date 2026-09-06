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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // Инициализация единственного экземпляра
            DontDestroyOnLoad(gameObject); // Сохраняем объект между сценами
        }
        else
        {
            Destroy(gameObject); // Уничтожаем дубликат
            return;
        }
    }

    private void OnEnable()
    {
        if (scanOnSceneLoaded)
        {
            SceneManager.sceneLoaded += OnSceneLoaded; // Подписка на событие загрузки новой сцены
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Отписка от событий загрузки сцен
    }

    private void Start()
    {
        if (scanOnStart)
        {
            ScanAndBindAllButtons(); // Первичное сканирование и привязка звуков ко всем кнопкам
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScanAndBindAllButtons();
    }

    /// <summary>
    /// Сканирует всю сцену и вешает компоненты озвучки на кнопки, у которых их еще нет.
    /// </summary>
    public void ScanAndBindAllButtons()
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        int boundCount = 0;

        foreach (Button btn in buttons)
        {
            // Пропускаем префабы в ассетах
            if (btn.gameObject.scene.name == null) continue;

            // Проверяем наличие эффекта наведения
            UIButtonHoverEffect effect = btn.GetComponent<UIButtonHoverEffect>();
            if (effect == null)
            {
                // Добавляем скрипт hover эффекта
                effect = btn.gameObject.AddComponent<UIButtonHoverEffect>();
                effect.playSfxOnHover = true;
                effect.playSfxOnClick = true;
                boundCount++;
            }
        }

        Debug.Log($"[ALCHEMIST SFX BINDER] Сканирование завершено. Озвучено новых кнопок: {boundCount}");
    }
}
