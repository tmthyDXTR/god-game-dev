<link rel="stylesheet" href="assets/site.css">
<!-- Auto insert commit sha with hyperlink with `{{COMMIT_SHA} }` without spaces -->

<nav>
<ul style="list-style:none; padding:0; display:flex; gap:1rem;">
  <li><a href="index.html">Home</a></li>
  <li><a href="card-brainstorm.html">Card Ideas</a></li>
  <li><a href="proto-brainstorm.html">Prototype Ideas</a></li>
  <li><a href="localizationSetup.html">Localization Setup</a></li>
</ul>
</nav>
---

# GOD — Development Changelog 

---

**Recent Changes (Assets/)**


-- **2025-12-08:** [`6920049`](https://github.com/tmthyDXTR/god-game-dev/commit/6920049c080b7aecfc69b2b4b9f963bf436210aa) — prototype card + UI improvements
  - Added tile hover popup (runtime) and custom pixel font
  - Implemented `ForageCardSO` and `CardPlayManager` (play-on-map flow)
  - Added `CardTargetingController` for map targeting and tile highlight support
  - Made `CardSO` localization-ready (`LocalizedString` fields) and added Localization setup docs
  - Fixed hand UI layout so hand reflows immediately after play (forced rebuild)
  - Improved hover baseline handling and arc layout so cards fan tighter with fewer cards
  - Misc: camera movement and small doc updates

- **2025-12-05:** [`e06a773`](https://github.com/tmthyDXTR/god-game-dev/commit/e06a773ef4c4ad49dd1feda6200b13ffd021f990) — "auto insert hyperlinks to commit at sha in changelog" 

- **2025-12-04:** [`de34c9a`](https://github.com/tmthyDXTR/god-assets/commit/de34c9a)  — "gh pages deployment action to automate changelog sha insertion test"

- **2025-12-01:** [`62a4364`](https://github.com/tmthyDXTR/god-assets/commit/62a4364)  — "prototype: population / worshiper system"
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

- **2025-11-28:** [`01a9d1b`](https://github.com/tmthyDXTR/god-assets/commit/01a9d1b) — "seperate enter exit hover anim duration"
  - Updated card visuals and timing: modified `Prefabs/CardPrefab.prefab` and `Scripts/Prototype/CardView.cs` to refine hover/enter/exit animation durations.

- **2025-11-27:** [`75b3a07`](https://github.com/tmthyDXTR/god-assets/commit/75b3a07) — "arc, hand cards hover"
  - Added editor tooling for card view: `Editor/CardViewEditor.cs`.
  - Tweaked hand & arc layout visuals and card-prefab; updated `Scripts/Prototype/CardView.cs`, `Scripts/Prototype/ArcLayoutGroup.cs`, and relevant prefabs and scene (`Scenes/SampleScene.unity`).
  - (Repository housekeeping) removed some `FastScriptReload` packaged files under `Assets/`.

- **2025-11-26:** [`15e5cb9`](https://github.com/tmthyDXTR/god-assets/commit/15e5cb9) / [`6c8c6f0`](https://github.com/tmthyDXTR/god-assets/commit/6c8c6f0) — "arc layout hand" / "card draw, hand, hover"
  - Prototype card system added: `Scripts/Prototype/CardSO.cs`, `CardInstance.cs`, `DeckManager.cs`, `DeckTest.cs`, `HandUI.cs`, `CardView.cs` and `Prefabs/CardPrefab.prefab` were introduced to support card draw, hand display, drag interactions and hover behavior.
  - God-beast data and resources: `Scripts/GodBeast/GodBeast.cs`, `GodBeastData.cs`, and `ResourceItem_Sap.asset` were added.
  - Inventory support: `Scripts/Inventory/ResourceItem.cs` added as a resource prototype.
  - Hex/tile updates: `HexGridGenerator.cs`, `HexTile.cs` and `MapData_7x7forest.asset` were updated/added to support the sample map.
  - Managers: `SelectionManager.cs` and `TurnManager.cs` received updates to integrate with card and turn flow.

- **2025-11-18:** [`03f2723`](https://github.com/tmthyDXTR/god-assets/commit/03f2723) — "first"
  - Initial import of project assets and tooling; added core prefabs, hex tile assets, TextMesh Pro resources and supporting URP settings.

---
