# Gameplay Session Architecture

## Composition

`GameSessionRoot` is the only gameplay-scene composition root. It owns the
serialized references to the player, inventory, collision reporter, impact
settings, settlement area, timer, scoreboard, pause service, restart service,
candy effect library, presentation controllers, and the active score targets.

No gameplay component may create another gameplay component, search the active
scene for a required dependency, or use `Resources.Load` for session data. A
missing reference is a scene-validation error, not a runtime fallback case.

## Event ownership

| Producer | Event | Consumer | Result |
| --- | --- | --- | --- |
| SettlementArea | PlayerExited | TimerManager | Starts the first countdown |
| TimerManager | Started | ScoreboardManager | Creates objective set |
| PlayerCollisionReporter | CollisionEntered | ScoreboardManager, CollisionFeedbackManager | Awards items and plays feedback |
| ScoreboardManager | SettlementCompleted | CandyRewardWindowManager | Shows the reward choice |
| TimerManager | Completed | GameResultScreenManager | Shows the final score |
| PlayerInventory | InventoryChanged | InventoryInfluenceSettings, HUD views | Updates movement penalty and presentation |

## Ownership rules

- `Runtime/Gameplay` owns rules and state, and exposes events or read-only
  state. It does not create UI.
- `Runtime/Presentation` owns views and input adaptation. UI Prefabs are the
  source of truth for layout, labels, colors, and Canvas hierarchy.
- `Runtime/Bootstrap` owns the explicit wiring only; it must not contain score,
  movement, or reward rules.
- `Data` contains authored ScriptableObjects. Session data is referenced from
  the scene root, never discovered by resource scanning.

## Asset naming

- Folders use PascalCase. Assets use PascalCase nouns with an optional category
  prefix such as `World_`, `Gameplay_`, or `UI_`.
- Do not introduce `New Material`, `1`, `Copy`, or scene-action names such as
  `idou`. Use a role-based name instead.
- New gameplay scenes must include `GameSessionRoot`, pass its validation, and
  be added to Build Settings only after a Play Mode smoke test.
