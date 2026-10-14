#!/usr/bin/env python3
"""Generate an Editor-readable original-art ledger; does not import/convert assets."""
import json
import pathlib
import re

project = pathlib.Path(__file__).resolve().parents[1]
audit = json.loads((project / 'Reports/fpssample-source-audit.json').read_text())
assert not audit['errors'], 'Resolve source download errors before preparing provenance.'
stage = project / 'SourceAssets~/FPSSample'
records = []
seen = set()
for path, info in sorted(audit['files'].items()):
    meta = stage / (path + '.meta')
    if not meta.exists():
        continue
    match = re.search(r'(?m)^guid:\s*([0-9a-f]{32})', meta.read_text())
    if not match:
        continue
    guid = match[1]
    if guid in seen:
        raise ValueError('Duplicate source GUID: ' + guid)
    seen.add(guid)
    records.append({'guid': guid, 'sourcePath': path, 'sha256': info['sha256']})
out = project / 'Assets/FirstRep/Editor/OriginalArtManifest.json'
out.write_text(json.dumps({'sourceCommit': audit['source_commit'], 'records': records}, indent=2) + '\n')
print(f'Prepared {len(records)} original-art provenance records. No assets imported or approved.')
