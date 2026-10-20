# Framework failed-load retry evidence

Explicitly sanitized copies; original diagnostics remain unchanged locally. The manifest records original/copy hashes and redaction scope. Decompress with `gzip -dc <file.gz>`.

Actual Unity 6000.6.5f1 with fixed fork 093a175b60a196e2ad864a01ace49e90f58aacc3 completed Prepare/UPM verification, EditMode 9/9 and PlayMode 15/15, all exit 0. Expected injected entity-failure errors belong to the existing failure-flow tests and are matched by LogAssert. No Player build, rendered image, audio-device or performance acceptance is claimed. Tests use the existing active license without new login or activation. Repeated missing-resource failures and selected actor creation failures are intentional; expected errors are matched by LogAssert. EditorSimulation exercises real Framework caches/batches and native scenes, but does not establish production AssetBundle IO/reference-count behavior.
