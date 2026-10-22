# Changelog

## [0.3.3] - 2026-10-22

- Converts socket and auxiliary-joint position offsets through character space when source and target bone axes differ.
- Keeps `joint1`/`joint2` coincident with their matching weapon sockets after hand-length scaling.
- Extends editor regression coverage to the complete `transform`/`joint` hierarchy and position offsets.

## [0.3.2] - 2026-10-22

- Compensates differing source/target hand bone axes so weapon sockets preserve the reference character-space orientation.
- Adds a default-enabled compatibility option to fall back to legacy local-rotation copying when required.
- Adds editor tests for rotated source and target hand coordinate systems.

## [0.3.1] - 2026-10-21

- Automatically discovers `Human_WithReferenceBones.fbx` or legacy `T-pose-W.fbx` reference models anywhere under Assets.
- Synchronizes all 16 socket poses from the reference model instead of only the five hand sockets.
- Preserves per-hand length scaling for hand weapon/shield socket positions.
- Keeps `joint1`/`joint2` pose copying as a compatibility fallback when reference socket nodes are absent.

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
