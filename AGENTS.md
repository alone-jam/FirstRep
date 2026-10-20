# FirstRep execution rules

Before installation, import, tests, builds, cleanup or delivery, read
[storage and recovery rules](Docs/STORAGE-AND-RECOVERY.md) and run
`python3 Tools/check_environment.py --verify-remote`.

- Never infer platform retention from a directory name or filesystem type. The
  official scope and unconfirmed details are recorded in that document.
- Commit/push source, original assets with their `.meta`, lockfiles and reviewed
  summaries to the existing authorized repository/branch. LFS pointers alone do
  not prove asset payload recovery; verify LFS objects separately.
- Cache is regenerable, but this does not authorize deleting it. Preserve existing
  files and historical evidence; obtain the user's approval before deletion.
- Raw test evidence and Player/capture output need a verified, authorized backup.
  A local `Reports` or `Deliverables` directory is staging, not proof of backup.
  No new upload destination, environment switch or publication is implicit.
- Keep credentials, license files and keyrings out of Git, reports, ordinary
  backups and published environment files. Do not inspect their contents.
- Do not install or accept new terms/login prompts without applicable user
  authorization. FirstRep uses the pinned fork; fix there and submit upstream PRs
  without waiting for upstream approval before local adoption.
- Historical official-pin tests/Player/GIF do not validate a new fork revision.
  Missing Editor, LFS payloads, licensing or storage must be reported explicitly.
