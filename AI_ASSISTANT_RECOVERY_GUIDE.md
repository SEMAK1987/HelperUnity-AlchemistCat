# Руководство по Восстановлению и Синхронизации ИИ-Помощника (Alchemist Cat Studio)

## Версия: 18.12.51
**Дата обновления:** 2026-09-08

---

## 1. Главное правило проекта
Все скрипты во вкладке **Code** (проводник проекта) должны постоянно поддерживаться в актуальном, рабочем и проверенном компилятором состоянии.
При каждом изменении логики или структуры компонентов:
1. Код каждого затронутого `.cs` скрипта немедленно перезаписывается в проектной директории.
2. Соблюдаются обязательные боковые комментарии `//` на русском языке.
3. Поддерживается **Dual-UI** архитектура (одновременная работа с `TextMeshProUGUI` и `Text`, `Image (Fill)` и `Slider`) для устранения любых сбоев при перетаскивании элементов в инспекторе Unity.
4. **Логика сброса и сохранения**: До тех пор, пока обучающий диалог с Котом не завершен полностью (`Tutorial_Full_Flow_Done == 0`), при каждом старте игры все ресурсы (Золото, Камни, Свитки, Кристаллы = 0), дни календаря, уровни и мастерство сбрасываются к начальным значениям. После полного завершения диалога прогресс сохраняется в `PlayerPrefs`.

---

## 2. Карта основных компонентов и скриптов

| Скрипт | Назначение | Ключевые поля Inspector |
|---|---|---|
| `AlchemyFishing_Minigame.cs` | Алхимическая Рыбалка: 2 шкалы, 10 попыток, расчет зон | `fishRodButton`, `actionButtonText` (`FishRod_Visual_Button -> Text (TMP)`), `delimiterZone4`, `attemptsCounterText` |
| `CatchMouse_Minigame.cs` | Мини-игра «Поймай Мышь» (Road_Track, 3 сложности) | `roadTrackPanel`, `mouseRunnerPrefab`, `scoreTextTMP`, `phaseNoticePanel` |
| `DailyRewardSystem.cs` | Цикл 7 дней + супер-вехи (1, 3, 6, 8, 12 месяцев) | `claimButton`, `timerTextTMP`/`timerText`, `statusTextTMP`/`statusText`, `calendarDaySlots` |
| `GameManager.cs` | Главный синглтон игры, ресурсы, опыт, прокачка | `goldText`, `crystalsText`, `stonesText`, `scrollsText`, `levelText`, `xpText`, `xpSlider`, `xpFillImage` |
| `Calendar_Manager.cs` | Динамическая генерация календарной сетки дней | Настройки префабов дней, автоматическая разметка в Content |
| `RecipeCrafting_Manager.cs` | Алхимические зелья, крафт, сундуки | `ResetCraftingAndChestProgress`, `OpenInventory` |
| `DialogueSystem_Manager.cs` | Диалоговая ветвящаяся система кота | `speakerNameText`, `dialogueContentText`, `catAvatarImage`, `choiceButtons`, `HideHUDForMinigame`, `RestoreHUDAfterMinigame` |
| `UnityConnector.cs` | Мост синхронизации Unity с AI Assistant | `serverUrl`, `autoSyncOnStart` |
| `blender_connector.py` | Экспорт моделей и анимаций из Blender в Unity | Автоматическая выгрузка FBX с запеканием масштабов |

---

## 3. Решение типовых проблем Inspector

1. **Элемент не перетаскивается в текстовое поле:**
   - Если это `TextMeshPro` — используйте поле с суффиксом `TMP` (например, `statusTextTMP`).
   - Если это стандартный `Text` — используйте базовое поле.
   - Скрипты автоматически распознают и обновляют оба компонента.

2. **Белый прямоугольник сверху экрана в Рыбалке:**
   - Выделите `Attempts_Badge` и выключите галочку на компоненте `Image` (или назначьте спрайт рамки).

3. **Кнопка действия в Рыбалке на самой Удочке:**
   - Перетащите `FishRod_Visual_Button` в `Fish Rod Button` и в `Action Button`.
   - В поле `Action Button Text` перетащите дочерний `Text (TMP)` из-под `FishRod_Visual_Button`.

4. **Полоса опыта не перетаскивается в Slider:**
   - Для объектов типа `Image` с `Image Type = Filled` (например, `Exp_Progress_Bar`) используйте поле `Xp Fill Image`.
   - Поле `Xp Slider` оставьте пустым (`None`).

---

## 4. Контрольный чеклист синхронизации
- [x] `AlchemyFishing_Minigame.cs` обновлен: интеграция надписи «ЗАБРОС!» в удочку и авто-поиск
- [x] `CatchMouse_Minigame.cs` исправлен (CS0111)
- [x] `DialogueSystem_Manager.cs` скрывает/восстанавливает HUD на время мини-игр
- [x] `DailyRewardSystem.cs` и `GameManager.cs` поддерживают Dual-UI
- [x] `UnityConnector.cs` и `blender_connector.py` обновлены до v18.12.51
- [x] `version.json`, `package.json`, `knowledge_base.json`, `metadata.json` синхронизированы
