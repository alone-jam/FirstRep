# Framework prefab and main-menu workflow evidence

Explicitly sanitized copies, not originals. The manifest records original/copy hashes and redaction scope. Original logs and failed attempts remain local. Decompress with `gzip -dc <file.gz>`.

Actual Unity 6000.6.5f1 on final fork c36cc4fff162994ec7f05f47a2f30cc0ec4e414c: consumer EditMode 9/9, PlayMode 19/19 and independent Framework workflow regressions 7/7, all exit 0. The corrected old baseline fails all six original cases; the earlier six-case fix passes. Earlier harness, startup readiness, naming-convention and stale-codegen failures are explicitly included and distinguished in the validation record. They are not omitted or counted as successes.

Expected actor/resource failures in passing tests are matched by LogAssert. No full upstream suite, production AssetBundle IO, new Player build, rendered graphics, physical mouse, audio-device or performance acceptance is claimed.
