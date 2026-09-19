# DEVELOPMENT LOG

## [2026-09-18] - Версия 18.12.99
- **Устранена ошибка компиляции CS0122 (Protection Level in CauldronDefense_Minigame.gameRootPanel)**:
  - В `CauldronDefense_Minigame.cs` поле `gameRootPanel` (а также `difficultyPanel`, `activeStagePanel`, `resultSummaryPanel`, `topBar`, `closeGameButton`) переведено в `public`, что обеспечивает безопасный доступ для `DialogueSystem_Manager.cs` и других систем.
  - Добавлен метод `CloseMinigame()` как публичный алиас к `CloseGame()`.
  - В `DialogueSystem_Manager.cs` метод `DeactivateAllMinigameSubPanels()` теперь корректно и без ошибок компиляции выключает корневые панели мини-игры `CauldronDefense_Minigame` и соседних игр при их смене.

## [2026-05-14]
- Версия 18.5.8: Zenith Multi-Tool Synergy & Settings Fix.
