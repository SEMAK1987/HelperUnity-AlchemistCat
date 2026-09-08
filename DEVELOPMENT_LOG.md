# DEVELOPMENT LOG

## [2026-09-08] - v18.12.51
- **Alchemy Fishing Minigame (`AlchemyFishing_Minigame.cs`)**:
  - Упразднена лишняя центральная кнопка `Action_Main_Button`. Текст действия («ЗАБРОС!», «СТОП!», «ПОДСЕЧЬ!», «ТЯНЕМ УЛОВ... 🌊») перенесен непосредственно на саму Удочку (`FishRod_Visual_Button` ➔ `Text (TMP)`).
  - Реализован автоматический fallback поиск дочернего `TextMeshProUGUI` удочки и бейджа `Attempts_Badge` в методе `Start()`, что исключает `MissingReferenceException` при неполных ссылках в инспекторе.
  - Исправлен белый артефакт в верхней части экрана (на `Attempts_Badge` отключен пустой белый компонент `Image`).
  - Интегрировано автоматическое скрытие аватара Кота и боковых кнопок HUD (`DialogueSystem_Manager.HideHUDForMinigame` / `RestoreHUDAfterMinigame`) на время работы мини-игр.
- **Catch Mouse Minigame (`CatchMouse_Minigame.cs`)**:
  - Устранена ошибка дублирования `OnDisable` (CS0111).
  - Настроены 3 фазы сложности (Easy, Medium, Hard) с разворотом мышей и анимациями.
- **Синхронизация окружения и коннекторов (v18.12.51)**:
  - Актуализированы `UnityConnector.cs`, `blender_connector.py`, `server.ts`, `src/App.tsx`, `package.json`, `version.json`, `knowledge_base.json`, `metadata.json`, `PROJECT_MASTER_BLUEPRINT.md`, `AI_ASSISTANT_RECOVERY_GUIDE.md` и `AGENTS.md`.

## [2026-05-14] - v18.5.8
- Zenith Multi-Tool Synergy & Settings Fix.
