# DEVELOPMENT LOG

## [2026-09-08]
- **Версия 18.12.49: Fix CS0111 Duplicate OnDisable in CatchMouse_Minigame.cs**:
  - Объединены дублирующиеся методы `OnDisable()` в файле `CatchMouse_Minigame.cs`.
  - Все операции остановки корутин (`StopAllCoroutines()`), очистки бегающих мышек (`ClearAllMice()`) и автоматического восстановления верхнего интерфейса HUD (`DialogueSystem_Manager.Instance.RestoreHUDAfterMinigame()`) собраны в единый метод жизненного цикла Unity.
  - Ошибка компиляции `error CS0111: Type 'CatchMouse_Minigame' already defines a member called 'OnDisable'` полностью устранена.

## [2026-05-14]
- Версия 18.5.8: Zenith Multi-Tool Synergy & Settings Fix.

