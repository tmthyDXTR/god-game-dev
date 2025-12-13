<link rel="stylesheet" href="assets/site.css">

<!-- AUTO_NAV_START -->
<!-- AUTO_NAV_END -->


Localization Setup
==================

This file gives a minimal, practical workflow to add localization to the project using Unity's Localization package and how to wire it into your card assets/UI with safe fallbacks.

Goal
----
- Use Unity Localization package (recommended) for strings & localized assets.
- Keep current `cardName`/`description` fields as fallbacks while introducing `LocalizedString` for real localization.
- Provide small, testable steps so translators and QA can validate early.

Prerequisites
-------------
- Unity Editor (project already contains TextMeshPro and UI). 
- Install the Unity Localization package via Package Manager: Window → Package Manager → search "Localization" → Install.

Quick Steps (Editor)
--------------------
1. Install the Localization package (as above).
2. Open the Localization window: Window → Asset Management → Localization.
3. Create Locales: In Localization window -> Locales tab -> Create Locale (e.g. `en`, `fr`, `de`).
4. Create String Table Collections:
   - Add → String Table Collection → name it (e.g. `UI`, `Cards`, `Tiles`).
   - Add entries (keys) for each text you want to translate: e.g. `card.forage.name`, `card.forage.desc`, `tile.forest.name`.
5. For each Locale, open the String Table and enter translated values.
6. (Optional) Create Localized Assets: Fonts, Sprites or Audio — Localization supports localized asset tables.

Using `LocalizedString` in ScriptableObjects (cards)
---------------------------------------------------
- Add `using UnityEngine.Localization;` and a `LocalizedString` field to your `CardSO` so card assets reference a string table entry instead of a hard-coded string.

Example (CardSO):
```csharp
using UnityEngine;
using UnityEngine.Localization;

public class CardSO : ScriptableObject
{
    public string cardName;            // existing fallback
    public string description;         // existing fallback

    // new localized references
    public LocalizedString localizedName;
    public LocalizedString localizedDescription;
}
```

Populating UI (CardView example)
--------------------------------
- In UI code, show fallback text immediately, then asynchronously resolve localized values and overwrite when ready. This avoids blank UI while localization initializes.

Example (CardView.Refresh):
```csharp
// immediate fallback
cardName.text = card.cardName;
cardText.text = card.description;

// async overwrite if localized values assigned
try {
    var op = card.localizedName.GetLocalizedStringAsync();
    op.Completed += (a) => { if (!string.IsNullOrEmpty(a.Result)) cardName.text = a.Result; };
    var op2 = card.localizedDescription.GetLocalizedStringAsync();
    op2.Completed += (a) => { if (!string.IsNullOrEmpty(a.Result)) cardText.text = a.Result; };
} catch { /* fallback remains */ }
```

Inspector workflow for `LocalizedString`
---------------------------------------
- In the inspector for a `LocalizedString` field: choose the String Table Collection and pick a key (or create a new key) then click the small locale dropdown to assign translations.

Migration strategy (safe, incremental)
--------------------------------------
- Keep existing `cardName` and `description` fields on assets as the editable fallback.
- Add `LocalizedString` fields in parallel; UI reads fallback first then overwrites with localized values when available.
- Optionally create a small editor utility to create keys from existing card names and populate the default locale automatically.

Fonts and non-Latin scripts
---------------------------
- TextMeshPro requires separate SDF font assets for languages that use large glyph sets (CJK) or right-to-left scripts (Arabic, Hebrew).
- Use localized asset tables to swap TMP FontAssets per locale (LocalizedAsset<TMP_FontAsset> / LocalizedObject table).
- Increase atlas resolution and sampling point size for CJK or complex scripts.

Right-to-left (RTL) support
---------------------------
- TMP doesn't auto-flip layouts. For RTL locales you will likely need to:
  - Use an RTL text processing pipeline (reverse order, handle Arabic shaping) or a library that preprocesses the string.
  - Mirror UI layouts where appropriate (anchor/pivot adjustments or separate RTL layout prefabs).

Pseudo-localization & QA
------------------------
- Create a pseudo-locale (e.g. `pseudo`) and populate string table entries with expanded/altered text to reveal layout/truncation issues (e.g. surround with brackets and double length).
- Test switching locales at runtime to ensure UIs update correctly.

Runtime Locale Switching (simple example)
-----------------------------------------
```csharp
using UnityEngine.Localization.Settings;

// Set by Locale index or reference
LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];

// Or find by identifier
var locale = LocalizationSettings.AvailableLocales.GetLocale("fr");
LocalizationSettings.SelectedLocale = locale;
```

Performance tips
----------------
- Use `LocalizedStringEvent` on UI components when possible (editor-driven hookup) so you avoid manual async calls in code.
- Use caching for frequently requested strings if you resolve the same key many times per frame.

Testing checklist
-----------------
- Pseudo-localization pass for all UI screens.
- Missing-key detector (report empty table entries) before builds.
- Spot check fonts for each locale (legibility, atlas clipping).

Recommended file/key naming convention
-------------------------------------
- Group keys by domain and be descriptive: `card.forage.name`, `card.forage.desc`, `ui.button.ok`, `tile.forest.name`.
- Keep translator notes with string table entries explaining context where needed.

Next steps I can help with
-------------------------
- Patch `CardSO`/`CardView` across your repo to use `LocalizedString` with graceful fallbacks (I already made a sample change earlier). 
- Add a small editor script to batch-create string keys from existing cards and populate the default locale.
- Add pseudo-locale entries and a small QA script to flag missing translations.

Notes
-----
- The Unity Localization package is powerful but introduces editor workflow steps: string tables, locales, and asset tables. Starting early makes it easy to avoid string debt later.
- Keep UI resilient: always show a sensible fallback string so missing translations don't break the player experience.

