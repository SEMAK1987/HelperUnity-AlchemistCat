# DEVELOPMENT LOG

## [2026-09-08] - Версия 18.12.52
- **Алхимическая Рыбалка: Исправление запуска после диалога Кота**:
  - `DialogueSystem_Manager.cs`: метод `OpenFishingUI()` теперь автоматически активирует родительский контейнер `MinigamesPanel` и все родительские панели до `Canvas`, устраняя проблему невидимости окна.
  - `AlchemyFishing_Minigame.cs`: добавлен метод `OpenMinigame()` с автоматическим обнаружением и переключением на `Difficulty_Selection_Panel` или `Active_Fishing_Stage` и безопасным закрытием через `HandleCloseClicked()`.
  - Улучшена обработка чит-клавиши `F9` для поддержки всех вариантов Unity Input (Legacy и New Input System).
  - Горизонтальная шкала: реализовано слитное непрерывное расширение полос от центра к краям без разрывов (толщина 80-90 px).

## [2026-05-14]
- Версия 18.5.8: Zenith Multi-Tool Synergy & Settings Fix.

