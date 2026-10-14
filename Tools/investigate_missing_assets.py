#!/usr/bin/env python3
"""Read-only GUID investigation: full public Git history, original shaders, Editor IDs.

Requires complete Git objects; intentionally disables automatic lazy fetch.
Never rewrites a serialized GUID or imports an old package into the project.
"""
import collections, hashlib, json, mmap, os, pathlib, re, subprocess

PROJECT = pathlib.Path(__file__).resolve().parents[1]
SOURCE = pathlib.Path('/workspace/upstream/FPSSample')
EDITOR = pathlib.Path('/workspace/tools/Unity/Hub/Editor/6000.6.5f1/Editor/Unity')
AUDIT = json.loads((PROJECT/'Reports/compatibility-audit.json').read_text())
STAGE = PROJECT/'SourceAssets~/FPSSample'
ENV = {**os.environ, 'GIT_NO_LAZY_FETCH': '1'}

def git(*args):
    return subprocess.check_output(['git', '-C', str(SOURCE), *args], env=ENV)

def main():
    index = {}
    for p in SOURCE.rglob('*.meta'):
        m = re.search(rb'(?m)^guid:\s*([0-9a-f]{32})', p.read_bytes())
        if m: index[m[1].decode()] = str(p.relative_to(SOURCE))[:-5]
    wanted = {x['guid'] for x in AUDIT['unresolved'] if x['classification'].startswith('absent')}
    objects = git('rev-list', '--objects', '--all', '--missing=print').decode().splitlines()
    missing = [o for o in objects if o.startswith('?')]
    if missing: raise RuntimeError('Incomplete history: fetch original Git objects before classifying missing GUIDs.')
    metas = {s.split(' ', 1)[0]: s.split(' ', 1)[1] for s in objects if ' ' in s and s.endswith('.meta')}
    process = subprocess.Popen(['git', '-C', str(SOURCE), 'cat-file', '--batch'], stdin=subprocess.PIPE, stdout=subprocess.PIPE, env=ENV)
    historic = collections.defaultdict(list)
    for oid, path in metas.items():
        process.stdin.write((oid+'\n').encode()); process.stdin.flush()
        header = process.stdout.readline().decode().split()
        if len(header) != 3: raise RuntimeError('Missing metadata blob: '+oid)
        data = process.stdout.read(int(header[2])); process.stdout.read(1)
        match = re.search(rb'(?m)^guid:\s*([0-9a-f]{32})', data)
        if match and match[1].decode() in wanted:
            historic[match[1].decode()].append({'path': path, 'git_blob': oid})
    process.stdin.close(); process.wait()
    resolved = json.loads((PROJECT/'Reports/recovered-original-assets.json').read_text())
    recovered = {r['guid']: r for r in resolved}
    rows = []
    with EDITOR.open('rb') as f, mmap.mmap(f.fileno(), 0, access=mmap.ACCESS_READ) as editor:
        for old in AUDIT['unresolved']:
            if old['guid'] not in wanted: continue
            guid = old['guid']; row = dict(old)
            row['history_matches'] = historic[guid]
            row['editor_binary_guid_offset'] = editor.find(guid.encode())
            if guid in recovered:
                row.update(classification='historical-original-recovered', recovery=recovered[guid], next_step='Resolve recovered prefab dependencies in the original revision; compare original impact behavior before including in migrated content.')
            elif guid == '5bf10cdea1344482e91a4f2b58506b77':
                row.update(classification='identified-legacy-entities-script', identity='GameObjectEntity', evidence='Exact original registry package 0.0.12-preview.24 archive checksum verified; matching original .meta and source preserved.', next_step='Old ECS behavior boundary, not missing art. Do not install legacy Entities in the new project; review affected objects before conversion.')
            elif row['editor_binary_guid_offset'] >= 0:
                row.update(classification='engine-known-legacy-script-reference', next_step='GUID exists in the actual Editor binary. Inspect each fileID/type and Unity upgrade output; do not replace one assembly GUID with a single modern script GUID.')
                row['likely_identity'] = {'6a10b2909283487f913b00d94cd3faf5':'Old Timeline assembly (inferred from serialized Timeline structures)', 'f70555f144d8491a825f0804e09c671c':'Old UGUI assembly (inferred from serialized Image fields)'}.get(guid)
            else:
                row.update(classification='not-supplied-in-public-source-history', next_step='No matching .meta in any fetched public revision or current packages. Do not invent a replacement. Validate whether this exact reference is consumed; if consumed, original author/source recovery is required.')
            usage = []
            for rel in sorted(set(old['references'])):
                if not rel.lower().endswith('.mat'): continue
                text = (STAGE/rel).read_text(errors='replace')
                shader_match = re.search(r'm_Shader:.*guid:\s*([0-9a-f]{32})', text)
                shader_path = index.get(shader_match[1]) if shader_match else None
                shader = (SOURCE/shader_path).read_text(errors='replace') if shader_path and (SOURCE/shader_path).is_file() else ''
                lines = text.splitlines()
                for i, line in enumerate(lines):
                    if guid not in line: continue
                    prop = next((re.search(r'^\s+-\s+(_\w+):', lines[j]) for j in range(i-1, max(-1,i-5), -1) if re.search(r'^\s+-\s+(_\w+):', lines[j])), None)
                    name = prop[1] if prop else None
                    declared = bool(name and re.search(r'(?m)^\s*(?:\[[^\]]*\]\s*)*'+re.escape(name)+r'\s*\(', shader)) if shader else None
                    usage.append({'material': rel, 'line': i+1, 'property': name, 'source_shader': shader_path, 'property_declared_in_shader_file': declared,
                                  'note':'Absence is evidence of a stale saved property, NOT permission to delete it or proof that scripts never swap shaders.'})
            row['material_usage'] = usage
            if usage and all(u['property_declared_in_shader_file'] is False for u in usage):
                row['impact_classification'] = 'all-observed-material-uses-are-undeclared-properties-in-original-shader'
            elif usage: row['impact_classification'] = 'at-least-one-declared-or-unresolved-shader-property'
            rows.append(row)
    result = {'scope':'44 initially unlocated GUIDs; no automatic reference edits', 'source_commit': git('rev-parse','HEAD').decode().strip(),
              'public_commits_scanned': int(git('rev-list','--count','--all')), 'historical_meta_blobs_scanned':len(metas), 'missing_git_objects':len(missing),
              'editor_sha256':hashlib.file_digest(EDITOR.open('rb'),'sha256').hexdigest(),
              'classification_counts':dict(collections.Counter(r['classification'] for r in rows)),
              'material_impact_counts':dict(collections.Counter(r.get('impact_classification','non-material-reference') for r in rows)), 'items':rows}
    (PROJECT/'Reports/missing-guid-investigation.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='items'},indent=2))

if __name__ == '__main__': main()
