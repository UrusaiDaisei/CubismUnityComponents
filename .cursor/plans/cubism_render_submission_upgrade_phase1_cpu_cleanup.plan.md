---
name: Cubism Render Upgrade Phase 1
overview: Reduce CPU submission overhead without changing rendering behavior by eliminating redundant per-draw state/property churn in existing Cubism paths.
todos:
  - id: map-phase1-hotpaths
    content: Identify repeated property/state writes in drawable and offscreen draw paths and define dedup boundaries.
    status: pending
  - id: phase1-propertyblock-dedup
    content: Consolidate MaterialPropertyBlock updates and guard unchanged writes in hot draw paths.
    status: pending
  - id: phase1-pass-loop-cleanup
    content: Remove duplicated per-draw setup in optimized pass while preserving ordering semantics.
    status: pending
  - id: phase1-diagnostics
    content: Add lightweight counters for draw submissions, property writes, clears/blits/RT binds.
    status: pending
  - id: phase1-validate
    content: Validate visual parity and measure CPU-side improvements in existing scenes.
    status: pending
isProject: false
---

# Phase 1 - CPU Submission Cleanup

## Goal
Lower CPU and render-thread overhead in the current submission path without changing draw ordering or visual output.

## Target Files
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRendererUsingBlendMode.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRendererUsingBlendMode.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderer.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderer.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/URP/CubismRenderPassFeatureOptimized.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/URP/CubismRenderPassFeatureOptimized.cs)

## Work Items
- Audit and reduce repeated `GetPropertyBlock` and `SetPropertyBlock` calls in `DrawDrawable`, `DrawOffscreenMesh`, and related apply methods.
- Introduce change-detection guards for frequently stable values:
  - main texture binding
  - blended render texture binding
  - multiply/screen colors
  - transform vectors when unchanged
- Keep command ordering and current buffer semantics exactly intact.
- Add diagnostic counters (development/editor only) for:
  - Cubism draw submissions per frame
  - property block writes per frame
  - offscreen clears, blits, and render-target binds

## Diagnostics Visibility In Unity
- Primary surface: Unity Profiler custom counters/markers for Cubism render path metrics.
- Optional surface: lightweight editor-only on-screen/debug readout for fast iteration without opening Profiler.
- Cross-check workflow: compare profiler counters with Frame Debugger and RenderDoc capture counts when validating changes.

### Diagnostics Implementation Notes
- Use allocation-free per-frame counters reset once per frame.
- Increment counters at the exact call sites where Cubism emits:
  - draw submissions
  - `SetPropertyBlock` writes
  - offscreen `SetRenderTarget`, `ClearRenderTarget`, and `Blit` operations
- Compile instrumentation only for editor/development builds using conditional compilation.

## Constraints
- No batching logic in this phase.
- No behavior changes for masks/offscreen/blends.
- Legacy output must remain bit-for-bit close in test scenes.

## Exit Criteria
- No visual regressions in all baseline scenes.
- Measurable CPU reduction in Cubism submission path.
- Diagnostic counters are available and consistent across runs.
