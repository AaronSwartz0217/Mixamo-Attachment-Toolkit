# Changelog

## [0.2.2] - 2026-08-05

- Fixed reference helper discovery when imported FBX wrapper nodes change the direct hierarchy.
- Added recursive transform1/joint1 and transform2/joint2 lookup.
- Added explicit diagnostics when reference hands, helper nodes, or hand measurement bones are unavailable.

## [0.2.1] - 2026-08-05

- Added an optional, default-enabled T-pose-W hand reference workflow.
- Added copying of transform1/joint1 and transform2/joint2 auxiliary hand hierarchies.
- Added reference-based hand weapon and shield socket poses.
- Added optional hand-length scaling for characters with different proportions.

## [0.2.0] - 2026-08-05

- Added ScriptableObject weapon attachment profiles.
- Added persistent base pose plus Idle, Walk, Run, Sprint, Attack, Block, and Holster offsets.
- Added smooth runtime state transitions.
- Added editor controls to create profiles, capture base/state poses, and apply profile previews.

## [0.1.1] - 2026-08-05

- Added a scene working-copy workflow for original character Prefab and FBX assets.
- Added saving a configured scene character as a new attachment-ready Prefab.
- Prevented socket and preview operations from directly editing persistent model assets.

## [0.1.0] - 2026-07-31

- Added Humanoid and Mixamo fallback bone resolution.
- Added standard attachment socket definitions.
- Added idempotent socket creation with Undo support.
- Added attachment recognition by component, folder, and name.
- Added attachment preview and prefab pose saving.
- Added runtime attachment controller.
# 0.3.0

- Added optional export of the attachment-ready scene character as a new FBX.
- Export retains target dimensions and the T-pose-W auxiliary hand hierarchy/pose.
- Added automatic Humanoid importer setup for FBX files exported inside Assets.
