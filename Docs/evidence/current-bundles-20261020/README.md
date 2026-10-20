# Current-source AssetBundle build evidence

Four explicitly sanitized log copies, not originals. See manifest.json for original/copy hashes and redaction scope. Original logs, memory/disk samples, generated serialization copies and new bundles remain local; no verified external artifact backup exists. Decompress with `gzip -dc <file.gz>`.

Source ad9f75a996befa57ebf7b0bbfab79f7af25eae38, fork c36cc4fff162994ec7f05f47a2f30cc0ec4e414c, Unity 6000.6.5f1. Actual AssetBundle build exits 0 with 6 bundles and 14 assets, including MainMenu and four scene entries. The production prepared-resource validation also exits 0. Logs retain legacy Timeline missing-script and shader-support warnings; success is not visual or audio acceptance.

Player build was paused before launch because the capacity estimate exceeds free scratch space with the 1 GiB reserve. No current Player or headless smoke result exists. Launcher/provenance guards reject missing/current-unproven and historical Players. Release upload remains paused.
