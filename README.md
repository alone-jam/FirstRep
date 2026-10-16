# FirstRep — Unity FPS migration checkpoint

Unity 6000.6.5f1 / HDRP 17.6 / EjoyFramework v2.1.0. This is a work-in-progress single-player adaptation of Unity FPSSample, not a finished port or a visual-fidelity acceptance build.

> Official EjoyFramework fix `a5e1ec6` is pinned through Git UPM. FirstRep EditMode **3/3** and PlayMode **5/5** pass; the latter was rerun after the application quit fix. Production AssetBundles (2 bundles, 9 assets) and a fresh Linux Player built successfully with **0 errors** on 2026-10-16. The new Player displays the scene with Bloom, ACES, grading, motion blur and subsurface scattering enabled after enabling HDRP camera NaN/Infinity cleanup. This prevents full-frame black propagation; it does not repair the remaining legacy material artifacts. A [real runtime GIF](#runtime-capture) is included below. See [current build and runtime evidence](Docs/current-player-validation.json).

See the [file/API adoption audit and actual test boundaries](Docs/FRAMEWORK-ADOPTION.md).

## Checkout and run

Install Git LFS before cloning, then run `git lfs pull`. Original binary art is stored in LFS; it must be downloaded before opening the project. Unity resolves EjoyFramework from the pinned Git URL in `Packages/manifest.json`. See [controls and build instructions](Docs/FIRST-PLAYABLE.md).

## Verification and limitations

The official dependency and actual consumer test evidence are published in [the adoption audit](Docs/FRAMEWORK-ADOPTION.md) and [structured results](Docs/official-framework-validation.json). The dependency has no local framework patches. Production builds use the upstream AssetBundle builder. See [bundle hashes and build history](Docs/production-validation.json).

Visual acceptance is separate from headless gameplay tests. Known migration color/material artifacts remain under investigation. The retained old executable does not establish the result of the current source. Actual audio-device playback remains unverified in this environment. No credentials, licenses, local caches, binaries or diagnostic authentication data are included.

## Runtime capture

![Actual FirstRep runtime: aiming, firing, reloading and movement](Docs/media/firstrep-runtime.gif)

Fresh Player, normal effects enabled. Real X11 mouse/keyboard input: 30→28 rounds, RELOADING, then 30/177 and movement. **8× playback** of a 96-second software Vulkan capture; native frame rate was about 0.2 fps. Pink/green materials, weapon stripes and intense highlights remain incorrect. This is a migration checkpoint, not visual-fidelity or performance acceptance. [Capture settings, hashes and state trace](Docs/runtime-capture-validation.json).

## Attribution and licenses

Original models, textures, audio and source retain their upstream licenses. Unity FPSSample is subject to the Unity Companion License for Unity-dependent projects; see [original notices](ThirdPartyNotices/FPSSample/LICENSE.md) and [full asset license](ThirdPartyNotices/FPSSample/Assets/LICENSE.md). Component notices retain their original paths beneath `ThirdPartyNotices/FPSSample`. EjoyFramework retains [its MIT license](ThirdPartyNotices/EjoyFramework/LICENSE.md). No blanket replacement license is applied. Upstream versions are recorded in [Docs/UpstreamVersions.json](Docs/UpstreamVersions.json).
