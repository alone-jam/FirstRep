# FirstRep — Unity FPS migration checkpoint

Unity 6000.6.5f1 / HDRP 17.6 / EjoyFramework v2.1.0. This is a work-in-progress single-player adaptation of Unity FPSSample, not a finished port or a visual-fidelity acceptance build.

> Official EjoyFramework fix `a5e1ec6` is pinned through Git UPM. Actual Unity tests pass: FirstRep EditMode **3/3**, PlayMode **5/5**, including concurrent entities/effects and real scene exit/re-entry. Upstream affected tests pass **22/23**; the remaining historical mesh-overlap assertion is recorded separately. Production AssetBundles and manifest were generated (9 assets). A lower-concurrency retry passed shader processing without OOM, then stopped when the 8.8 GiB scratch filesystem filled during final packing. No new Player or GIF is published.

See the [file/API adoption audit and actual test boundaries](Docs/FRAMEWORK-ADOPTION.md).

## Checkout and run

Install Git LFS before cloning, then run `git lfs pull`. Original binary art is stored in LFS; it must be downloaded before opening the project. Unity resolves EjoyFramework from the pinned Git URL in `Packages/manifest.json`. See [controls and build instructions](Docs/FIRST-PLAYABLE.md).

## Verification and limitations

The official dependency and actual consumer test evidence are published in [the adoption audit](Docs/FRAMEWORK-ADOPTION.md) and [structured results](Docs/official-framework-validation.json). The dependency has no local framework patches. Production builds use the upstream AssetBundle builder. See [bundle hashes and the concrete Player build blocker](Docs/production-validation.json).

Visual acceptance is separate from headless gameplay tests. Known migration color/material artifacts remain under investigation. The retained old executable does not establish the result of the current source. Actual audio-device playback remains unverified in this environment. No credentials, licenses, local caches, binaries or diagnostic authentication data are included.

## Attribution and licenses

Original models, textures, audio and source retain their upstream licenses. Unity FPSSample is subject to the Unity Companion License for Unity-dependent projects; see [original notices](ThirdPartyNotices/FPSSample/LICENSE.md) and [full asset license](ThirdPartyNotices/FPSSample/Assets/LICENSE.md). Component notices retain their original paths beneath `ThirdPartyNotices/FPSSample`. EjoyFramework retains [its MIT license](ThirdPartyNotices/EjoyFramework/LICENSE.md). No blanket replacement license is applied. Upstream versions are recorded in [Docs/UpstreamVersions.json](Docs/UpstreamVersions.json).
