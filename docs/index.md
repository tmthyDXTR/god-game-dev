<link rel="stylesheet" href="assets/site.css">

# GOD — Development Changelog 

---

**Recent Changes (Assets/)**

- **2025-12-04:** `{{COMMIT_SHA}}`  — "gh pages deployment action to automate changelog sha insertion test"

- **2025-12-01:** `62a4364`  — "prototype: population / worshiper system"
  - Added a minimal population "worshiper" prototype under `Assets/Scripts/Prototype/`:
    - `PopulationAgent.cs` — lightweight agent that can idle-wander inside a tile or move between tiles. Supports `StartMovement()` to begin inter-tile movement later.
    - `PopulationManager.cs` — runtime pooling manager for agents, runtime-created tiny black sprite (no prefab required), spawn API `SpawnAgent(tile, stayOnTile=true)`, `StartAgentMovement(agent)`, and neighbor selection via `HexGridGenerator.tiles`.
    - `PopulationTestSpawner.cs` — test spawner that places agents at the god-beast origin tile (`Hex(0,0)`) when available, with fallback test-grid creation.
  - Hex tile integration:
    - `HexGrid/HexTile.cs` now includes a minimal `populationCount` field and `OnPopulationEnter/OnPopulationLeave` hooks so agents can update tile counts.
  - Behavior notes:
    - Agents spawn pooled (no Instantiate/Destroy spikes) and by default idle-wander inside their spawn tile (small jitter) until movement is started.
    - `PopulationManager.RequestNextMove` resolves neighbors via the existing `HexGridGenerator` and includes placeholder weights for future Faith/Heresy attraction/repulsion.
  - Quick testing: spawn and idle-visible behavior at the god-beast tile; call `PopulationManager.Instance.StartAgentMovement(agent)` to allow movement later.

- **2025-11-28:** `01a9d1b` — "seperate enter exit hover anim duration"
  - Updated card visuals and timing: modified `Prefabs/CardPrefab.prefab` and `Scripts/Prototype/CardView.cs` to refine hover/enter/exit animation durations.

- **2025-11-27:** `75b3a07` — "arc, hand cards hover"
  - Added editor tooling for card view: `Editor/CardViewEditor.cs`.
  - Tweaked hand & arc layout visuals and card-prefab; updated `Scripts/Prototype/CardView.cs`, `Scripts/Prototype/ArcLayoutGroup.cs`, and relevant prefabs and scene (`Scenes/SampleScene.unity`).
  - (Repository housekeeping) removed some `FastScriptReload` packaged files under `Assets/`.

- **2025-11-26:** `15e5cb9` / `6c8c6f0` — "arc layout hand" / "card draw, hand, hover"
  - Prototype card system added: `Scripts/Prototype/CardSO.cs`, `CardInstance.cs`, `DeckManager.cs`, `DeckTest.cs`, `HandUI.cs`, `CardView.cs` and `Prefabs/CardPrefab.prefab` were introduced to support card draw, hand display, drag interactions and hover behavior.
  - God-beast data and resources: `Scripts/GodBeast/GodBeast.cs`, `GodBeastData.cs`, and `ResourceItem_Sap.asset` were added.
  - Inventory support: `Scripts/Inventory/ResourceItem.cs` added as a resource prototype.
  - Hex/tile updates: `HexGridGenerator.cs`, `HexTile.cs` and `MapData_7x7forest.asset` were updated/added to support the sample map.
  - Managers: `SelectionManager.cs` and `TurnManager.cs` received updates to integrate with card and turn flow.

- **2025-11-18:** `03f2723` — "first"
  - Initial import of project assets and tooling; added core prefabs, hex tile assets, TextMesh Pro resources and supporting URP settings.

---
