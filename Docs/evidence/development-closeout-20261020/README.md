# Development closeout evidence

Five explicitly sanitized copies, not originals. See manifest.json for original/copy hashes and redaction scope. Original logs/XML remain local with no configured external raw-evidence backup. Decompress with `gzip -dc <file.gz>`.

Actual Unity 6000.6.5f1: dependency check exit 0, consumer EditMode 9/9 and PlayMode 20/20, both exit 0. The latter includes the opt-in smoke menu command regression. Expected resource/actor failure diagnostics are matched by LogAssert. No new Framework suite run, Player/bundle build, rendering, physical input, audio-device or performance acceptance is claimed.
