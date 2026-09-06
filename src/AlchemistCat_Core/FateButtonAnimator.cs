using UnityEngine; // Базовое пространство имен игрового движка Unity
using UnityEngine.EventSystems; // Интерфейсы обработки событий мыши и сенсорного ввода
using UnityEngine.UI; // Работа со стандартными UI элементами интерфейса

public class FateButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler // Аниматор кнопок с реакцией на курсор и клик
{
    private RectTransform rectTransform; // Ссылка на компонент трансформации UI элемента
    private Vector3 originalScale; // Исходный масштаб кнопки при запуске
    private Vector3 targetScale; // Текущий целевой масштаб для плавной анимации

    [Header("Параметры масштабирования")]
    [Tooltip("Во сколько раз увеличивается кнопка при наведении мыши")]
    public float hoverScaleMultiplier = 1.08f; // Множитель масштаба при наведении курсора
    [Tooltip("Во сколько раз сжимается кнопка в момент клика")]
    public float clickScaleMultiplier = 0.93f; // Множитель масштаба при нажатии (эффект отдачи)
    [Tooltip("Скорость плавного перехода (сглаживание Lerp)")]
    public float animationSpeed = 16f; // Скорость сглаживания интерполяции размера

    private void Start() // Инициализация начальных размеров кнопки при старте
    {
        rectTransform = GetComponent<RectTransform>(); // Получение ссылки на RectTransform кнопки
        if (rectTransform != null) // Проверка наличия компонента
        {
            originalScale = rectTransform.localScale; // Запоминание базового масштаба
            targetScale = originalScale; // Установка целевого масштаба в исходный
        }
    }

    private void Update() // Плавное обновление масштаба в каждом кадре
    {
        if (rectTransform != null) // Если RectTransform присутствует
        {
            // Плавное интерполирование размеров к целевому результату
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * animationSpeed); // Интерполяция Lerp к targetScale
        }
    }

    // Событие: Курсор мыши зашел в область кнопки
    public void OnPointerEnter(PointerEventData eventData) // Обработка наведения курсора
    {
        targetScale = originalScale * hoverScaleMultiplier; // Увеличение целевого масштаба при наведении
        PlaySoundSafe("UI_Hover_Soft"); // Воспроизведение мягкого звука наведения
    }

    // Событие: Курсор мыши покинул область видимости кнопки
    public void OnPointerExit(PointerEventData eventData) // Обработка ухода курсора с кнопки
    {
        targetScale = originalScale; // Возврат целевого масштаба к исходному
    }

    // Событие: Игрок нажал на кнопку
    public void OnPointerDown(PointerEventData eventData) // Обработка нажатия клавиши мыши/пальца
    {
        targetScale = originalScale * clickScaleMultiplier; // Уменьшение размера для эффекта нажатия
        PlaySoundSafe("UI_Click_Metallic"); // Воспроизведение звука клика
    }

    // Событие: Игрок отпустил кнопку
    public void OnPointerUp(PointerEventData eventData) // Обработка отпускания нажатия
    {
        targetScale = originalScale * hoverScaleMultiplier; // Возврат к масштабу наведения
    }

    /// <summary>
    /// Безопасное звуковое воздействие через SettingsManager без прямых жестких связей
    /// </summary>
    private void PlaySoundSafe(string sfxName) // Метод безопасного воспроизведения звука через рефлексию
    {
        System.Type settingsType = System.Type.GetType("SettingsManager"); // Поиск типа SettingsManager в сборке
        if (settingsType == null) // Если тип не найден в текущей сборке
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) // Перебор всех загруженных сборок проекта
            {
                settingsType = assembly.GetType("SettingsManager"); // Попытка найти тип в сборке
                if (settingsType != null) break; // Прерывание цикла при нахождении
            }
        }

        if (settingsType != null) // Если класс менеджера настроек успешно найден
        {
            var instanceProperty = settingsType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static); // Получение синглтона Instance
            if (instanceProperty != null) // Если свойство Instance существует
            {
                var instance = instanceProperty.GetValue(null); // Получение объекта синглтона
                if (instance != null) // Если синглтон инициализирован
                {
                    var playMethod = settingsType.GetMethod("PlaySFX", new System.Type[] { typeof(string) }); // Поиск метода PlaySFX
                    if (playMethod != null) // Если метод найден
                    {
                        playMethod.Invoke(instance, new object[] { sfxName }); // Вызов метода с именем звукового файла
                        return; // Завершение выполнения
                    }

                    var playSfxMethod = settingsType.GetMethod("PlaySfx", new System.Type[] { typeof(string) }); // Поиск альтернативного метода PlaySfx
                    if (playSfxMethod != null) // Если альтернативный метод найден
                    {
                        playSfxMethod.Invoke(instance, new object[] { sfxName }); // Вызов метода воспроизведения звука
                        return; // Завершение выполнения
                    }
                }
            }
        }
    }
}
