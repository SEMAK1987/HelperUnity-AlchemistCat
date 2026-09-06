using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Разработчик: Алхимический Кот (Alchemist Cat Core)
/// Контроллер милого Кота-Алхимика. Управляет анимацией,
/// реакцией на поглаживание (клики), мурлыканьем и варкой зелий.
/// </summary>
public class CatController : MonoBehaviour
{
    public static CatController Instance { get; private set; } // Статический синглтон для глобального доступа к коту

    [Header("Спрайты Кота")]
    public Sprite idleSprite; // Спрайт кота в обычном спокойном состоянии
    public Sprite happySprite; // Спрайт довольного кота при поглаживании
    public Sprite sleepingSprite; // Спрайт спящего кота
    public Sprite brewingSprite; // Спрайт кота во время варки зелий

    [Header("UI Ссылки")]
    public Image catImage; // UI-компонент Image для отображения спрайта кота
    public TextMeshProUGUI meowBubbleText; // Текст всплывающего облачка с мяуканьем
    public GameObject bubbleObject; // Родительский объект облачка с текстом

    [Header("Звуки Кота")]
    public AudioClip meowSound; // Звуковой эффект мяуканья
    public AudioClip purrSound; // Звуковой эффект мурлыканья

    private Coroutine bubbleCoroutine; // Ссылка на корутину анимации облачка
    private Vector3 originalScale; // Исходный масштаб объекта для эффекта покачивания

    public enum CatState { Idle, Happy, Sleeping, Brewing } // Перечисление состояний поведения кота
    private CatState currentState = CatState.Idle; // Текущее состояние кота

    private void Awake() // Инициализация синглтона при создании объекта
    {
        if (Instance == null) Instance = this; // Инициализация синглтона
        else Destroy(gameObject); // Уничтожение дубликата при повторном создании
    }

    private void Start() // Стартовая инициализация кота
    {
        if (catImage == null) catImage = GetComponent<Image>(); // Автопоиск Image если ссылка не назначена
        originalScale = transform.localScale; // Запоминаем исходный размер

        if (bubbleObject != null) bubbleObject.SetActive(false); // Прячем речевое облачко на старте
        SetState(CatState.Idle); // Устанавливаем спокойное состояние кота
    }

    /// <summary>
    /// Меняет состояние кота и обновляет его визуальный спрайт.
    /// </summary>
    public void SetState(CatState newState) // Смена состояния и спрайта кота
    {
        currentState = newState; // Запоминаем новое состояние
        if (catImage == null) return; // Проверка наличия компонента Image

        switch (currentState) // Переключение по состояниям
        {
            case CatState.Idle: // Состояние покоя
                catImage.sprite = idleSprite; // Спрайт покоя
                break;
            case CatState.Happy: // Состояние радости
                catImage.sprite = happySprite; // Спрайт радости
                break;
            case CatState.Sleeping: // Состояние сна
                catImage.sprite = sleepingSprite; // Спрайт сна
                break;
            case CatState.Brewing: // Состояние варки
                catImage.sprite = brewingSprite; // Спрайт варки
                break;
        }
    }

    /// <summary>
    /// Вызывается при клике/поглаживании кота в интерфейсе.
    /// </summary>
    public void OnCatClicked() // Обработка клика по коту
    {
        if (currentState == CatState.Sleeping) // Если кот спит
        {
            WakeUp(); // Пробуждение кота
            return; // Выход
        }

        // Запускаем анимацию покачивания
        StopAllCoroutines(); // Остановка текущих корутин
        StartCoroutine(BounceCatCoroutine()); // Запуск анимации сжатия-растяжения

        // Добавляем немного опыта Коту
        if (GameManager.Instance != null) // Если GameManager доступен
        {
            GameManager.Instance.AddXP(5); // Начисление 5 опыта
        }

        // Проигрываем звук и показываем "Мяу!"
        if (meowSound != null && SettingsManager.Instance != null) // Если звук и настройки есть
        {
            SettingsManager.Instance.PlaySoundEffect(meowSound); // Воспроизведение мяуканья
        }

        string[] catPhrases = { // Набор фраз кота
            "Муррр... Погладь еще!", "Мяу! Котел готов к варке!", "Дай мышку, хозяин!", // Фразы 1-3
            "Ура, алхимия!", "Мяу! Наставник спит!", "Фррр... Зелье пахнет вкусно!" // Фразы 4-6
        };
        string phrase = catPhrases[Random.Range(0, catPhrases.Length)]; // Выбор случайной фразы
        ShowMeowBubble(phrase); // Отображение речевого облачка
    }

    public void ShowMeowBubble(string text) // Отображение речевого облачка с фразой
    {
        if (bubbleObject == null || meowBubbleText == null) return; // Проверка наличия UI компонентов

        if (bubbleCoroutine != null) StopCoroutine(bubbleCoroutine); // Остановка старого показа
        bubbleCoroutine = StartCoroutine(ShowBubbleCoroutine(text)); // Запуск новой корутины показа
    }

    private IEnumerator ShowBubbleCoroutine(string text) // Корутина отображения облачка на 3 секунды
    {
        bubbleObject.SetActive(true); // Включение облачка
        meowBubbleText.text = text; // Установка текста
        yield return new WaitForSeconds(3f); // Пауза 3 секунды
        bubbleObject.SetActive(false); // Скрытие облачка
    }

    private IEnumerator BounceCatCoroutine() // Корутина пружинящей анимации кота при клике
    {
        float duration = 0.15f; // Длительность полуфазы
        float elapsed = 0f; // Таймер анимации

        // Быстрое сжатие
        while (elapsed < duration) // Цикл сжатия
        {
            elapsed += Time.deltaTime; // Прирост времени
            float t = elapsed / duration; // Нормализация от 0 до 1
            transform.localScale = Vector3.Lerp(originalScale, new Vector3(originalScale.x * 1.15f, originalScale.y * 0.85f, originalScale.z), t); // Сплющивание
            yield return null; // Ожидание следующего кадра
        }

        elapsed = 0f; // Сброс таймера
        // Возврат
        while (elapsed < duration) // Цикл возврата в норму
        {
            elapsed += Time.deltaTime; // Прирост времени
            float t = elapsed / duration; // Нормализация
            transform.localScale = Vector3.Lerp(new Vector3(originalScale.x * 1.15f, originalScale.y * 0.85f, originalScale.z), originalScale, t); // Восстановление
            yield return null; // Ожидание следующего кадра
        }

        transform.localScale = originalScale; // Фиксация исходного масштаба
    }

    public void WakeUp() // Пробуждение спящего кота
    {
        SetState(CatState.Idle); // Переход в спокойное состояние
        ShowMeowBubble("Мяу! Я проснулся!"); // Фраза пробуждения
        if (meowSound != null && SettingsManager.Instance != null) // Если звук назначен
        {
            SettingsManager.Instance.PlaySoundEffect(meowSound); // Воспроизведение звука
        }
    }
}
