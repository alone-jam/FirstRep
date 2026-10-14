# FirstRep — Unity FPS migration checkpoint

Unity 6000.6.5f1 / HDRP 17.6 / EjoyFramework v2.1.0. This is a work-in-progress single-player adaptation of Unity FPSSample, not a finished port or a visual-fidelity acceptance build.

> Framework adoption is in progress. The pinned upstream Git UPM package compiles, and single-target module/gameplay tests pass. [Upstream #17](https://github.com/one2world/EjoyFramework/issues/17) aliases three same-prefab target entities to one instance, blocking full gameplay acceptance. Current source stops at that defect; the retained old Linux executable is a separate checkpoint. New Player/GIF publication is pending.

See the [file/API adoption audit and actual test boundaries](Docs/FRAMEWORK-ADOPTION.md).

## Checkout and run

Install Git LFS before cloning, then run `git lfs pull`. Original binary art is stored in LFS; it must be downloaded before opening the project. Unity resolves EjoyFramework from the pinned Git URL in `Packages/manifest.json`. Current source hits the documented upstream entity-isolation blocker when entering Play Mode; no complete-game pass is claimed. See [controls and build instructions](Docs/FIRST-PLAYABLE.md).

## Verified checkpoint and current limitations

A prior Linux development build ran with four scenes, three targets, ground collision and a nonblack screenshot. Editor gameplay tests covered movement, jump, aiming, shooting, reload and target respawn. That executable is retained locally and is intentionally not committed.

The previous build reported three instances of one invalid Ejoy test metadata GUID and 70 warnings. The former local framework patch is replaced by upstream Git revision 641b43dc75f6fdae9acecc37d1c1e632d7f0aff4, which contains the earlier input/GUID/entity-ID fixes. Current source routes resources, actor lifetimes, procedures, UI, audio and locomotion through framework modules. Headless compilation and scoped tests pass, but the three-target isolation regression fails; see the audit. The source has NOT been rebuilt or visually accepted. The latest graphical Editor retry reached this environment's 16 GiB memory limit and was stopped. Known color/material artifacts remain unresolved; animation/effects visual regression and real audio-device verification remain pending. Do not interpret a nonblack screenshot as correct rendering.

No Unity credentials, licenses/activation files, local caches, build binaries or diagnostic authentication data are included. Historical source staging and local reports are also excluded.

## Attribution and licenses

Original models, textures, audio and source retain their upstream licenses. Unity FPSSample is subject to the Unity Companion License for Unity-dependent projects; see [original notices](ThirdPartyNotices/FPSSample/LICENSE.md) and [full asset license](ThirdPartyNotices/FPSSample/Assets/LICENSE.md). Component notices retain their original paths beneath `ThirdPartyNotices/FPSSample`. EjoyFramework retains [its MIT license](ThirdPartyNotices/EjoyFramework/LICENSE.md). No blanket replacement license is applied. Upstream versions are recorded in [Docs/UpstreamVersions.json](Docs/UpstreamVersions.json).
