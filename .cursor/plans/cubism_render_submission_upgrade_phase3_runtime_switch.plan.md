---
name: Cubism Render Upgrade Phase 3
overview: Switch runtime submission to merged geometry segments across drawables, masks, and offscreen while keeping conservative hazard splitting and immediate legacy fallback.
todos:
  - id: phase3-submit-path-integration
    content: Integrate merged segment submission into optimized pass render loop.
    status: pending
  - id: phase3-mask-offscreen-routing
    content: Route masks and offscreen composition through compatibility segments with strict dependency ordering.
    status: pending
  - id: phase3-hazard-splitting
    content: Implement conservative segment splits for RT transitions and dependency hazards.
    status: pending
  - id: phase3-feature-flags
    content: Add runtime flags for merged paths and legacy fallback switches.
    status: pending
  - id: phase3-functional-verify
    content: Verify draw-count reduction and visual parity against legacy in representative scenes.
    status: pending
isProject: false
---

# Phase 3 - Runtime Submission Switch

## Goal
Enable merged geometry submission in runtime for all paths, with conservative correctness-first splitting and fast rollback.

## Target Files
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/URP/CubismRenderPassFeatureOptimized.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/URP/CubismRenderPassFeatureOptimized.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRendererUsingBlendMode.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRendererUsingBlendMode.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderController.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderController.cs)

## Runtime Integration
- Insert merged-segment submission path into `DrawObjects` flow.
- For each controller/group:
  - submit merged segments where compatibility key allows
  - split immediately on hazards:
    - render-target domain changes
    - mask dependency boundaries
    - offscreen parent-child dependency boundaries
    - blend/material incompatibility

## Feature Flags
- `EnableCubismCpuSubmissionCleanup`
- `EnableCubismMergedSubmission`
- `EnableCubismMergedMasks`
- `EnableCubismMergedOffscreen`
- `EnableCubismLegacyFallback`

## Safety Rules
- If any uncertainty exists about dependency correctness, split segment and keep behavior conservative.
- Allow instant fallback to legacy path without scene restart.
- Keep legacy and merged metrics side-by-side for debugging.

## Exit Criteria
- Verified draw-call reduction in RenderDoc for representative models.
- Visual parity in masks, offscreen composition, and blend ordering.
- No correctness regressions under camera movement and sorting-group interaction.
