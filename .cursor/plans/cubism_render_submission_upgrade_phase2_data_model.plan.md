---
name: Cubism Render Upgrade Phase 2
overview: Build merged-geometry cache and compatibility-segmentation data model that preserves strict draw order and supports future runtime submission switching.
todos:
  - id: phase2-cache-structs
    content: Define per-controller merged vertex/index cache structures and per-drawable range metadata.
    status: pending
  - id: phase2-segmentation-keys
    content: Define compatibility key model for segment boundaries across material, blend, RT domain, mask, and offscreen dependencies.
    status: pending
  - id: phase2-dirty-rules
    content: Implement deterministic invalidation and rebuild rules using existing dirty signals and ordering changes.
    status: pending
  - id: phase2-legacy-fallback-hook
    content: Preserve legacy per-drawable submission metadata so phase 3 can switch safely.
    status: pending
  - id: phase2-validate-determinism
    content: Validate segment stability and ordering determinism frame-to-frame.
    status: pending
isProject: false
---

# Phase 2 - Merged Geometry Data Model

## Goal
Create robust CPU-side data structures for merged submission, without enabling runtime merged draw execution yet.

## Target Files
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderController.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderController.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderer.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderer.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRendererUsingBlendMode.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRendererUsingBlendMode.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/URP/CubismRenderPassFeatureOptimized.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/URP/CubismRenderPassFeatureOptimized.cs)

## Data Model Requirements
- Per-controller merged buffer payloads:
  - merged vertices
  - merged indices
- Per-drawable range mapping:
  - `indexStart`
  - `indexCount`
  - `baseVertex`
- Compatibility segmentation metadata:
  - material/shader variant
  - blend mode class
  - render-target domain (camera/offscreen)
  - mask dependency class
  - offscreen parent-child hazard boundaries

## Dirty And Rebuild Policy
- Rebuild merged cache when any relevant signal changes:
  - vertex positions dirty
  - visibility/order dirty
  - blend/material state changes
  - mask topology/dependency changes
  - offscreen topology/dependency changes
- Keep deterministic ordering aligned with legacy draw sequence.

## Constraints
- No runtime merged submission in this phase.
- No change in final emitted draw calls yet.
- Must preserve ability to fall back to legacy per-draw metadata at any point.

## Exit Criteria
- Stable cache generation across frames with deterministic segment maps.
- Segment boundaries validated against legacy order expectations.
