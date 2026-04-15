# CubismCreatedAssetList Migration Note

## Context

`CubismCreatedAssetList` is a legacy deferred queue used to finalize imported/created assets after import processing (`OnPostImport()`).

Our current import flow is primarily based on `ScriptedImporter` and `AssetImportContext`, so this legacy queue is a technical-debt area that should be phased out carefully.

## Why we are keeping it for now

Some current editor import paths still rely on this behavior for standalone asset creation/update and dirty tracking:

- `Packages/com.live2d.cubism/Framework/Pose/Editor/CubismPoseMotionImporter.cs`
- `Packages/com.live2d.cubism/Framework/MotionFade/Editor/CubismFadeMotionImporter.cs`
- `Packages/com.live2d.cubism/Editor/CubismAssetProcessor.cs` (post-import bridge)

## Planned follow-up

When we have time, migrate these paths to pure importer-safe flows and then remove `CubismCreatedAssetList` and the post-import bridge safely.

High-level steps:

1. Remove standalone `AssetDatabase.CreateAsset(...)` writes during importer callbacks where possible.
2. Prefer `AssetImportContext.AddObjectToAsset(...)` for deterministic importer outputs.
3. Replace `CubismCreatedAssetList` cache usage with direct importer context/state.
4. Delete `CubismCreatedAssetList` and its `OnPostImport()` integration after verification.

