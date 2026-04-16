---
name: Cubism Render Upgrade Phase 4
overview: Validate correctness and performance across production scenarios, then roll out merged submission progressively with guardrails and rollback controls.
todos:
  - id: phase4-test-matrix
    content: Build validation matrix for simple, multi-texture, mask-heavy, and offscreen-heavy scenes.
    status: pending
  - id: phase4-metric-capture
    content: Capture before/after metrics for draw calls, render-thread CPU, RT operations, and frame time.
    status: pending
  - id: phase4-regression-gates
    content: Define fail criteria for visual differences in masks, offscreen composition, and blend ordering.
    status: pending
  - id: phase4-progressive-enable
    content: Roll out defaults in stages from CPU cleanup to full merged paths.
    status: pending
  - id: phase4-release-checklist
    content: Finalize rollback procedures and release checklist for production enablement.
    status: pending
isProject: false
---

# Phase 4 - Validation And Rollout

## Goal
Confirm correctness and measurable wins, then enable features in controlled stages to prevent regressions.

## Validation Matrix
- Simple model: single texture, no masks.
- Production multi-texture model.
- Mask-heavy model.
- Offscreen hierarchy-heavy model.
- Mixed sorting groups with camera movement.

## Metrics To Record
- DrawIndexed count inside Cubism pass.
- Render-thread CPU time for Cubism submission.
- Render-target operation counts:
  - clears
  - blits
  - target binds/switches
- Frame time (CPU and GPU where available).

## Regression Gates
- Mask edge quality and mask motion correctness.
- Offscreen parent-child composition correctness.
- Blend ordering correctness for mixed blend states.
- Stable output under animation and camera motion.

## Rollout Sequence
- Stage 1: enable CPU cleanup by default.
- Stage 2: enable merged drawables by default.
- Stage 3: enable merged masks after parity signoff.
- Stage 4: enable merged offscreen after parity signoff.
- Keep global legacy fallback available across all stages.

## Exit Criteria
- Performance improvements are repeatable across representative models.
- No critical visual regressions in production scenarios.
- Clear rollback and troubleshooting playbook is documented.
