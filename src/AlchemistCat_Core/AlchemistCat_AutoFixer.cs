using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AlchemistCat_AutoFixer : MonoBehaviour
{
    [Header("=== Авто-исправление Аватарок ===")]
    public bool fixAvatarOnStart = true; // Флаг: проверять и сбрасывать недопустимые платные аватарки при запуске игры
    public Image currentAvatarDisplay; // UI-компонент Image для отображения текущей аватарки в интерфейсе
    public Sprite defaultFreeAvatarSprite; // Спрайт начального бесплатного лисенка (id=0)

    [Header("=== Авто-исправление Диалоговых Окон ===")]
    public bool fixDialogueTextFormat = false; // Отключено, чтобы сохранялись точные настройки текста из инспектора Unity
    public TextMeshProUGUI catDialogueText; // Ссылка на текстовый блок реплик кота

    [Header("=== Принудительная блокировка VIP значка на старте ===")]
    public bool forceHideVIPIcon = true; // Скрывать ли значок VIP в начале игры до достижения 2 ранга
    public GameObject vipIconButtonObject; // Объект кнопки VIP в верхнем интерфейсе

    private void Awake()
    {
        // 1. Проверка и автоматическое исправление выбранной аватарки
        if (fixAvatarOnStart) // Если включена автопроверка
        {
            int savedAvatar = PlayerPrefs.GetInt("Selected_Avatar_Id", 0); // Считываем сохраненный ID выбранной аватарки
            int playerLevel = PlayerPrefs.GetInt("Player_Level", 1); // Считываем текущий уровень игрока
            
            if (savedAvatar > 2 && playerLevel < 13) // Если выбран платный облик, но уровень не позволяет
            {
                PlayerPrefs.SetInt("Selected_Avatar_Id", 0); // Принудительно сбрасываем на бесплатный облик #0
                PlayerPrefs.Save(); // Сохраняем изменения в реестре
            }

            if (currentAvatarDisplay != null && defaultFreeAvatarSprite != null) // Если ссылки на UI заданы
            {
                if (PlayerPrefs.GetInt("Selected_Avatar_Id", 0) == 0) // Если выбран базовый облик
                {
                    currentAvatarDisplay.sprite = defaultFreeAvatarSprite; // Устанавливаем спрайт бесплатного лисенка
                }
            }
        }

        // 2. Блокировка значка VIP на старте игры
        if (forceHideVIPIcon) // Если задано скрытие значка VIP
        {
            if (vipIconButtonObject != null) // Проверяем наличие объекта кнопки
            {
                vipIconButtonObject.SetActive(false); // Выключаем видимость кнопки VIP в интерфейсе
            }
        }

        // 3. Форматирование текста диалогов
        if (fixDialogueTextFormat && catDialogueText != null) // Если включена авто-подгонка текста
        {
            catDialogueText.enableAutoSizing = false; // Отключаем авто-размер, чтобы сохранить размер из инспектора
        }
    }
}
