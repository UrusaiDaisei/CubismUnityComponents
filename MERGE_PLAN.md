# Cubism package merge plan: lib_tweaks onto packaged-version

This document describes how to port **our tweaks** (from `lib_tweaks`) onto the **new package base** (from `packaged-version`) without doing a full branch merge. We use three copies of the package to make the port explicit and reviewable.

---

## 1. Goal

- **Target:** `Packages/com.live2d.cubism` — the new, working package from `packaged-version` (Cubism SDK 5.3, current APIs).
- **Source of tweaks:** Differences between `com.live2d.cubism-old` and `com.live2d.cubism-tweaked` (our customizations: ScriptedImporter-based import pipeline, editor UX, advanced look, etc.).
- **Outcome:** The new package keeps all 5.3 behavior and gains only our tweaks, adapted to the new APIs where needed.

---

## 2. Package layout (reference)

| Location | Role |
|--------|------|
| `Packages/com.live2d.cubism` | **Merge target.** New base (packaged-version). Do not replace wholesale; apply tweaks here. |
| `com.live2d.cubism-old` | Old package **without** our tweaks. Baseline to compute “what we changed”. |
| `com.live2d.cubism-tweaked` | Old package **with** our tweaks. Source of custom logic and new files to port. |

**Rule:** Treat the merge as “replay the (old → tweaked) delta onto `Packages/com.live2d.cubism`”, with API adaptations for 5.3.

**Origin of reference packages (root copies):** Both are copies of `Packages/com.live2d.cubism` from their respective commits, placed at repo root so a direct branch merge does not overwrite the current 5.3 package.

- **com.live2d.cubism-old:** commit `f483066d328234992e7f2e4a34e8f4833c8b80f9`
- **com.live2d.cubism-tweaked:** commit `a0ad96413ccc26d8abd8e28b0aaabdf6d0a9fdbb`

---

## 3. Strategy summary

1. **Identify the delta**  
   Diff `com.live2d.cubism-old` vs `com.live2d.cubism-tweaked` to get:
   - **Added in tweaked:** New files to copy or re-create under `Packages/com.live2d.cubism`.
   - **Modified in tweaked:** Changed files; port the *intent* of the change into the corresponding file in `Packages/com.live2d.cubism` (adapting to new types/APIs).
   - **Removed in tweaked:** Usually ignore for the port (we are not removing 5.3 features).

2. **Do not**  
   - Overwrite `Packages/com.live2d.cubism` with the full tweaked tree.  
   - Reintroduce old SDK APIs (e.g. old masking, `GetDrawableRenderOrders`) that the new package has already replaced.

3. **Do**  
   - Prefer the new package’s implementation for Core/Rendering/Framework unless a change in tweaked is clearly a **custom behavior** we want.  
   - For Editor: keep **our** import pipeline (ScriptedImporters + priorities), and wire it to the new package’s delegates and types (e.g. `DrawableMaterialPicker` / `OffscreenMaterialPicker`).

---

## 4. Tweaks to port (from old vs tweaked diff)

### 4.1 Files only in tweaked (add or align in new package)

Port these into `Packages/com.live2d.cubism` with paths adjusted and APIs adapted to 5.3.

**Editor**

- `Editor/CubismAnimationPreviewRefresher.cs` (+ .meta)
- `Editor/Importers/CubismImporterPriorities.cs` (+ .meta) — used by ScriptedImporter priority.
- `Editor/Importers/CubismMocImporter.cs` (+ .meta) — if we use MOC ScriptedImporter.
- `Editor/Inspectors/AdvancedLookParameterInspector.cs` (+ .meta, .uss, .uxml)
- `Editor/Inspectors/CubismParameterEditor.cs` (+ .meta)
- `Editor/Inspectors/CubismParametersInspectorInspector.uss`, `.uxml` (+ .meta)
- `Editor/Inspectors/CubismParameterXYSlider.cs` (+ .meta, .uss)
- `Editor/Inspectors/DeformationTracker` (+ .meta)

**Framework**

- `Framework/AssemblyDefinitions.cs` (+ .meta) — only if the new package still uses this pattern.
- `Framework/CubismParameterGroups.cs` (+ .meta)
- `Framework/DeformationTracker` (+ .meta)
- `Framework/LookAt/AdvancedLookController.cs` (+ .meta)
- `Framework/LookAt/AdvancedLookParameter.cs` (+ .meta)
- `Framework/LookAt/AdvancedLookTarget.cs` (+ .meta)
- `Framework/Utils/SpanExtentions.cs` (+ .meta) — fix typo to `SpanExtensions` if desired.

**Check:** For each, confirm whether the new package already has an equivalent (e.g. different name or structure). If it exists, merge behavior instead of blind copy.

### 4.2 Files modified in tweaked (port behavior, not necessarily full file)

Apply the **logic** of the tweaked version onto the **current** file in `Packages/com.live2d.cubism`. Prefer new package API (e.g. `DrawableMaterialPicker`, `AllDrawObjectsRenderOrder`, no old masking).

**Editor**

- `Editor/CubismAssetProcessor.cs` — Keep new package’s resource generation; only port any tweak-specific hooks (e.g. avoid re-enabling main-asset AssetPostprocessor if we use ScriptedImporter for model3.json).
- `Editor/CubismUnityEditorMenu.cs` — Port any custom menu items.
- `Editor/Importers/ComponentExtensionMethods.cs` — Port only our additions.
- `Editor/Importers/CubismImporter.cs` — Must expose delegates compatible with new `CubismModel3Json` (e.g. `OnPickDrawableMaterial`, `OnPickOffscreenMaterial`); keep ScriptedImporter entry points.
- `Editor/Importers/CubismModel3JsonImporter.cs` — **Critical.** Keep our ScriptedImporter, `IModelImportContext`, `OnDidImportModel`; adapt to new `ToModel(...)` and picker signatures.
- `Editor/Importers/CubismMotion3JsonImporter.cs` — Port our changes; keep new package’s API usage.
- `Editor/Inspectors/*` — CubismDisplayInfoParameterName, CubismDrawable, CubismMaskTexture (if still present in new package), CubismPart, CubismRenderController, CubismRenderer, CubismParametersInspectorInspector, etc. Port UI/tweaks only; keep 5.3 types (e.g. DrawObject, Offscreen).

**Framework**

- `Framework/ComponentExtensionMethods.cs`, `CubismDisplayInfo*`, `CubismInspectorAbstruct`, `CubismParametersInspector`, `CubismPartsInspector`, `CubismUpdateController`, Expression, Json (`CubismBuiltinPickers`, `CubismModel3Json`, `CubismDisplayInfo3Json`) — Port our behavior; keep new package’s types (e.g. `ColorBlend`/`AlphaBlend` from `CubismDrawable`/unmanaged, `DrawableMaterialPicker`/`OffscreenMaterialPicker`).

**Core (only if clearly our custom logic)**

- Most Core differences are likely SDK version (old vs 5.3). Only port Core changes that are **clearly** our customizations; otherwise keep `Packages/com.live2d.cubism` Core as-is.

### 4.3 Files only in old (not in tweaked)

Examples: `CubismModelTypes.cs`, `CubismOffscreen.cs`, `CubismUnmanagedOffscreens.cs`, `csc.rsp`, `cubism-info.yml`.  
Do **not** delete these from the new package. The new package already has the 5.3 equivalents; we are only adding tweaks.

---

## 5. Known API adaptations (5.3 vs old)

Use this when porting code that referenced the old SDK.

| Old (tweaked / pre-5.3) | New (Packages/com.live2d.cubism) |
|-------------------------|-----------------------------------|
| `CubismModel3Json.MaterialPicker` | `DrawableMaterialPicker` + `OffscreenMaterialPicker` |
| `CubismImporter.OnPickMaterial` | `OnPickDrawableMaterial`, `OnPickOffscreenMaterial` |
| `ToModel(moc, materialPicker, texturePicker)` | `ToModel(drawableMaterialPicker, texturePicker, offscreenMaterialPicker, shouldImportAsOriginalWorkflow)` (and similar overloads; no `moc` from caller) |
| `OverrideFlagForDrawableMultiplyColors` / `ScreenColors` | `OverrideFlagForDrawObjectMultiplyColors` / `OverrideFlagForDrawObjectScreenColors` |
| `GetDrawableRenderOrders` | `GetRenderOrders` (unified render order array) |
| Masking (e.g. `CubismMaskController`, mask texture) | Removed in 5.3; do not reintroduce |
| `Object` (ambiguous) | `UnityEngine.Object` where needed (e.g. in `IModelImportContext`) |
| Sample script `CubismImporter.OnDidImportModel` | Use `CubismModel3JsonImporter.OnDidImportModel` and `IModelImportContext` |

---

## 6. Step-by-step execution plan

Work in small, verifiable steps. After each step, build in Unity and run a quick smoke test (e.g. import a model3.json, open a scene with a Cubism model).

### Phase A0: Core (done)

- **Diff:** old vs tweaked Core had differences in ArrayExtensionMethods, CubismDrawable, CubismMoc, CubismModel, CubismParameter, CubismPart, and Unmanaged/*. Tweaked also *removed* CubismModelTypes, CubismOffscreen, CubismUnmanagedOffscreens (old masking).
- **Ported:** Only **CubismParameter** customizations: `[DisallowMultipleComponent]`, private method renamed `Reset` → `ResetParameter`, and public `ParameterValue` property (alias for `Value`). No other Core files changed.
- **Left as 5.3:** ArrayExtensionMethods (5.3 keeps Offscreens Revive), CubismDrawable, CubismMoc, CubismModel, CubismPart, and all Unmanaged types — differences are SDK/API or structural; keep packaged-version Core.

### Phase A: Import pipeline (ScriptedImporter) — done

1. **Importer priorities and entry point**  
   - Ensure `CubismImporterPriorities.cs` exists in `Packages/com.live2d.cubism/Editor/Importers/` and is used by `CubismModel3JsonImporter` (and optionally `CubismMocImporter`).  
   - Confirm `CubismModel3JsonImporter` is the ScriptedImporter for `model3.json` (not AssetPostprocessor for the main asset).

2. **CubismImporter delegates**  
   - In `CubismImporter.cs`, expose `OnPickDrawableMaterial`, `OnPickOffscreenMaterial`, `OnPickTexture` (or equivalent) matching `CubismModel3Json` and `CubismBuiltinPickers`.  
   - No `MaterialPicker`; use the 5.3 delegate names.

3. **CubismModel3JsonImporter**  
   - Keep our `IModelImportContext`, `OnDidImportModel`, and import flow.  
   - Call the new `ToModel(...)` overloads with the correct pickers and `ShouldImportAsOriginalWorkflow`.  
   - Use `UnityEngine.Object` in `IModelImportContext` to avoid ambiguous `Object`.  
   - Ensure `CubismMocImporter` is present if we use it, and that it fits the new package’s flow.

4. **CubismAssetProcessor**  
   - Do not use AssetPostprocessor for the main model import (we use ScriptedImporter).  
   - Keep the new package’s built-in resource/material generation as-is unless we have a specific tweak.

5. **Samples**  
   - Update sample script (e.g. `ImportCustomization.cs`) to use `CubismModel3JsonImporter.OnDidImportModel` and `IModelImportContext` with correct namespace and types.  
   - Implemented: legacy event still fired; sample comment added for IModelImportContext.

### Phase B: Editor-only additions (no Core/Runtime API)

6. **Editor menu**  
   - Port custom menu items from `CubismUnityEditorMenu.cs`.

7. **Animation preview**  
   - Add `CubismAnimationPreviewRefresher.cs` (and .meta); remove or stub any reference to old masking if present.

8. **Inspectors and UI**  
   - Port `AdvancedLookParameterInspector`, `CubismParameterEditor`, `CubismParameterXYSlider`, `CubismParametersInspectorInspector` UXML/USS, and `DeformationTracker` under Editor.  
   - Adapt any references to renderer/part/drawable to 5.3 names (e.g. DrawObject, OverrideFlagForDrawObject*).

### Phase C: Framework runtime + editor

9. **Framework additions**  
   - Add `CubismParameterGroups.cs`, `AdvancedLookController`, `AdvancedLookParameter`, `AdvancedLookTarget`, `SpanExtensions` (and .meta).  
   - Add `Framework/DeformationTracker` if used.  
   - Only add `AssemblyDefinitions.cs` if the new package still uses that pattern.

10. **Framework modifications**  
    - Apply tweaks to `CubismParametersInspector`, `CubismPartsInspector`, `CubismUpdateController`, Expression/Json as needed; keep 5.3 APIs (e.g. `ColorBlend`/`AlphaBlend` from drawables, new pickers).

### Phase D: Cleanup and verification

11. **Build and test**  
    - Full Unity build, import a model3.json, open sample scene, play.  
    - Confirm no “missing material” (UnlitBlit, UnlitBlendModeNormalOver, etc.); those should come from the new package’s Resources.

12. **Docs and meta**  
    - Update or add README/CHANGELOG in the package if we document “ScriptedImporter-based import” or “custom importer priorities”.  
    - Ensure .meta GUIDs are consistent (no duplicate GUIDs from copied files).

---

## 7. Verification checklist

- [ ] Project builds in Unity with no compile errors.
- [ ] Importing a `model3.json` uses the ScriptedImporter (our pipeline), not the AssetPostprocessor for the main asset.
- [ ] `OnDidImportModel` (or equivalent) is available and used by the sample if applicable.
- [ ] No references to `MaterialPicker`, `GetDrawableRenderOrders`, or old masking in the merged code.
- [ ] Built-in materials (e.g. UnlitBlit, UnlitBlendModeNormalOver) load from the new package’s Resources.
- [ ] Advanced Look and parameter inspector tweaks work if ported.
- [ ] No duplicate or conflicting .meta GUIDs for new/copied assets.

---

## 8. Reference commands

- **List files only in tweaked:**  
  `diff -rq com.live2d.cubism-old com.live2d.cubism-tweaked | grep "Only in com.live2d.cubism-tweaked"`

- **List modified files:**  
  `diff -rq com.live2d.cubism-old com.live2d.cubism-tweaked | grep "differ"`

- **Diff a single file (to port logic):**  
  `diff com.live2d.cubism-old/Editor/Importers/CubismModel3JsonImporter.cs com.live2d.cubism-tweaked/Editor/Importers/CubismModel3JsonImporter.cs`

---

## 9. Document history

- Created when starting the “tweaks onto packaged-version” port after resetting the branch from `packaged-version` and placing `com.live2d.cubism-old` and `com.live2d.cubism-tweaked` in the repo for reference.

- Documented commit origins: com.live2d.cubism-old = f483066, com.live2d.cubism-tweaked = a0ad964 (Packages at those commits; copies at repo root).
