---
name: Cubism Render Submission Upgrade
overview: Implement a full-path Cubism render submission upgrade that reduces draw-call overhead across drawables, masks, and offscreen composition, with CPU cleanup delivered first and merged-geometry submission following behind feature flags and strict fallbacks.
todos:
  - id: phase1-plan-file
    content: Execute Phase 1 plan file focused on CPU submission cleanup.
    status: pending
  - id: phase2-plan-file
    content: Execute Phase 2 plan file focused on merged geometry data model.
    status: pending
  - id: phase3-plan-file
    content: Execute Phase 3 plan file focused on runtime submission switching.
    status: pending
  - id: phase4-plan-file
    content: Execute Phase 4 plan file focused on validation and rollout.
    status: pending
isProject: false
---

# Cubism Render Submission Upgrade (Master Index)

## Rationale
This upgrade is intentionally split into one plan file per phase to reduce risk, keep scope bounded, and avoid cross-phase churn while implementing.

## Scope (Confirmed)
- All rendering paths are included in the full upgrade: drawables, masks, offscreen composition, and mixed blend modes.
- Execution order is fixed: CPU cleanup first, then merged-geometry submission.
- Legacy fallback remains available throughout rollout.

## Phase Plan Files
- [Phase 1 - CPU Submission Cleanup](D:/Jobs/Joana/Projects/CubismUnityComponents/.cursor/plans/cubism_render_submission_upgrade_phase1_cpu_cleanup.plan.md)
- [Phase 2 - Merged Geometry Data Model](D:/Jobs/Joana/Projects/CubismUnityComponents/.cursor/plans/cubism_render_submission_upgrade_phase2_data_model.plan.md)
- [Phase 3 - Runtime Submission Switch](D:/Jobs/Joana/Projects/CubismUnityComponents/.cursor/plans/cubism_render_submission_upgrade_phase3_runtime_switch.plan.md)
- [Phase 4 - Validation And Rollout](D:/Jobs/Joana/Projects/CubismUnityComponents/.cursor/plans/cubism_render_submission_upgrade_phase4_validation_rollout.plan.md)

## Shared Files Across Phases
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/URP/CubismRenderPassFeatureOptimized.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/URP/CubismRenderPassFeatureOptimized.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRendererUsingBlendMode.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRendererUsingBlendMode.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderer.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderer.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderController.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismRenderController.cs)
- [D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismOffscreenRenderTextureManager.cs](D:/Jobs/Joana/Projects/CubismUnityComponents/Packages/com.live2d.cubism/Rendering/CubismOffscreenRenderTextureManager.cs)

## Global Success Criteria
- Draw-call count decreases significantly on representative production models.
- No visual regressions in mask output, offscreen composition, or blend ordering.
- CPU submission overhead is lower and stable frame-to-frame.