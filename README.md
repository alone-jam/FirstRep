# FirstRep — Unity FPS migration checkpoint

Unity 6000.6.5f1 / HDRP 17.6 / EjoyFramework v2.1.0. This is a work-in-progress single-player adaptation of Unity FPSSample, not a finished port or a visual-fidelity acceptance build.

## Checkout and run

Install Git LFS before cloning, then run `git lfs pull`. Original binary art is stored in LFS; it must be downloaded before opening the project. Open `Assets/FirstRep/Scenes/Bootstrap.unity` in Unity 6000.6.5f1 and enter Play Mode. See [controls and build instructions](Docs/FIRST-PLAYABLE.md).

## Verified checkpoint and current limitations

A prior Linux development build ran with four scenes, three targets, ground collision and a nonblack screenshot. Editor gameplay tests covered movement, jump, aiming, shooting, reload and target respawn. That executable is retained locally and is intentionally not committed.

The previous build reported three instances of one invalid Ejoy test metadata GUID and 70 warnings. This source snapshot repairs that GUID and restores converted original exposure, postprocessing and subsurface-scattering settings. The updated source passed headless Editor compilation/audit, but has NOT been rebuilt or visually accepted. The latest graphical Editor retry reached this environment's 16 GiB memory limit and was stopped. Known color/material artifacts remain unresolved; animation/effects visual regression and real audio-device verification remain pending. Do not interpret a nonblack screenshot as correct rendering.

No Unity credentials, licenses/activation files, local caches, build binaries or diagnostic authentication data are included. Historical source staging and local reports are also excluded.

## Attribution and licenses

Original models, textures, audio and source retain their upstream licenses. Unity FPSSample is subject to the Unity Companion License for Unity-dependent projects; see [original notices](ThirdPartyNotices/FPSSample/LICENSE.md) and [full asset license](ThirdPartyNotices/FPSSample/Assets/LICENSE.md). Component notices retain their original paths beneath `ThirdPartyNotices/FPSSample`. EjoyFramework retains [its MIT license](Packages/com.ejoy.framework/LICENSE.md). No blanket replacement license is applied. Upstream versions are recorded in [Docs/UpstreamVersions.json](Docs/UpstreamVersions.json).
