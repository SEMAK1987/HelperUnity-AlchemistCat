# DEVELOPMENT LOG

## [2026-09-08] - Версия 18.12.55
- **Алхимическая Рыбалка: Устранение предупреждения OnValidate SendMessage**:
  - `AlchemyFishing_Minigame.cs`: вызов изменения размеров `RectTransform` в режиме редактора обернут в `UnityEditor.EditorApplication.delayCall`, что полностью устраняет предупреждения движка Unity о запрете `SendMessage` во время `OnValidate`.
  - Метод `ApplyBeamHeight()` вызывается корректно в `OnEnable()` и `Start()`.

## [2026-05-14]
- Версия 18.5.8: Zenith Multi-Tool Synergy & Settings Fix.

