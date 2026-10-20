# Framework entity failure-flow evidence

These are explicitly sanitized copies; original diagnostics remain unchanged locally. Manifest records original/copy hashes and redaction scope. Decompress with `gzip -dc <file.gz>`.

Two initial prepare runs failed during compilation (missing namespace import, then an incomplete test helper interface signature); no tests ran in those attempts. After correction, actual Unity 6000.6.5f1 with fixed fork 093a175b60a196e2ad864a01ace49e90f58aacc3 completed Prepare/UPM verification, EditMode 9/9 and PlayMode 10/10, all exit 0. Actor instantiation/creation failure logs in successful PlayMode are intentional fault injection and matched by LogAssert; they are not unreported failures or omitted logs. No Player build, rendered image, physical mouse, audio-device or performance acceptance is claimed.
