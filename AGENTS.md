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
  authorization.
- Fix Framework defects and missing general-purpose capabilities in
  `alone-jam/EjoyFramework`, add regression coverage, push the fix and submit an
  upstream PR. FirstRep uses the verified fork revision without waiting for
  upstream approval. Do not duplicate general Framework infrastructure in this
  project to work around a defect or missing capability; retain game-specific
  rules and Unity adapters here. Pin the full tested commit SHA in the manifest,
  lockfile, dependency guard and provenance, retaining PR/commit mappings.
- Historical official-pin tests/Player/GIF do not validate a new fork revision.
  Missing Editor, LFS payloads, licensing or storage must be reported explicitly.
