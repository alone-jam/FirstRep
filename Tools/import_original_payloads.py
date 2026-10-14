#!/usr/bin/env python3
"""Stage unchanged binary art for real Unity import without duplicating 12 GiB.

Only importer inputs (textures, FBX, audio) are linked, with read-only payloads.
Metadata is copied independently so Unity upgrades cannot change source metas.
Materials/scenes/controllers/graphs are NOT automatically migrated by this step.
"""
import hashlib, json, os, pathlib, shutil

project = pathlib.Path(__file__).resolve().parents[1]
stage = project/'SourceAssets~/FPSSample'
audit = json.loads((project/'Reports/fpssample-source-audit.json').read_text())
extensions = {'.tif','.tiff','.fbx','.tga','.psd','.exr','.png','.wav','.ogg','.mp3','.aif','.aiff','.hdr','.jpg','.jpeg'}
records = []
for rel, info in sorted(audit['files'].items()):
    if pathlib.Path(rel).suffix.lower() not in extensions: continue
    source = stage/rel; dest = project/rel
    actual = hashlib.file_digest(source.open('rb'),'sha256').hexdigest()
    if actual != info['sha256']: raise ValueError('Source payload changed: '+rel)
    dest.parent.mkdir(parents=True,exist_ok=True)
    if dest.exists():
        if not os.path.samefile(source,dest): raise ValueError('Destination already has independent content: '+rel)
    else: os.link(source,dest)
    original_mode = source.stat().st_mode & 0o777
    source.chmod(original_mode & ~0o222)
    smeta = pathlib.Path(str(source)+'.meta'); dmeta = pathlib.Path(str(dest)+'.meta')
    if smeta.exists() and not dmeta.exists(): shutil.copy2(smeta,dmeta)
    for parent in pathlib.Path(rel).parents:
        if str(parent) in ('.','Assets'): continue
        meta = pathlib.Path(str(parent)+'.meta'); source_meta = stage/meta; dest_meta = project/meta
        if source_meta.exists() and not dest_meta.exists(): shutil.copy2(source_meta,dest_meta)
    records.append({'path':rel,'sha256':actual,'bytes':info['bytes'],'method':'read-only hardlink; independent metadata','original_mode':oct(original_mode)})
(project/'Reports/original-payload-import-inputs.json').write_text(json.dumps({'stage':'binary importer inputs only; not approved migrated content','records':records},indent=2)+'\n')
print('Prepared',len(records),'unchanged original binary inputs; bytes',sum(r['bytes'] for r in records),flush=True)
