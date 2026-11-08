# LunaReth Ghost Samurai — Project Status

## Current milestone

**Milestone 1: first reference-locked 3D base created; needs substantial sculpt refinement before it can be called final.** A starter armature is included to make the scene inspectable and testable, not as a finished animation rig.

## Environment and source

- Blender 5.2.3 LTS.
- Current source image: `reference_frame0000.png` (512 × 768, transparent).
- Layered source: `C:\Users\crescent\Pictures\Samurai.kra`.
- Working folder: `F:\Projects\LunaReth\GhostSamurai3D\`.
- Current direct-reference design has no hat. Do not restore the kasa from the older project brief unless the user asks.

## Current deliverables

- Blender project: `GhostSamurai_Model_v04.blend`
- Build script: `create_ghost_samurai_v04.py`
- Faithful front render: `GhostSamurai_Preview_v04.png`
- Three-quarter depth check: `GhostSamurai_DepthCheck_v04.png`
- Side-depth inspection: `GhostSamurai_SideDepth_v04.png`

Earlier v01–v03 files remain untouched.

## What v04 contains

- The exact supplied color art on a gently bent UV surface, with the front preview kept clean and aligned to the reference.
- Separate 3D skull and cloak volumes, individual flower and vine geometry, and a base armature in clearly named collections.
- No hat or invented sword equipment.

## Limits and next work

- v04 is a 2.5D sprite-model base, not a finished volumetric sculpt. The three-quarter view remains too shallow and the side view exposes blockout-level geometry.
- Refine the skull as an actual side-profile head, build coherent torso/limb and cloak volumes that follow the existing drawing, then fit the flower/vine geometry to the KRA groups. Keep the art locked in the gameplay-facing view.
- The armature is a starter skeleton; split and weight the deforming parts before making animations.
- Do not describe this as a finished match from every angle until the non-front renders have been reviewed and corrected.
