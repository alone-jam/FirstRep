#!/usr/bin/env python3
"""Preserve authentic source assets outside Unity import; never converts or substitutes art.

GUID closure is a static approximation: binary serialization and string-based loads
still require AssetDatabase.GetDependencies validation in a licensed Editor.
"""
import argparse, concurrent.futures, hashlib, json, pathlib, re, shutil, urllib.request, urllib.parse

REV = 'c8375e7cf29cd0bcc3764b0650028a99aecb7475'
ROOTS = [
    'Assets/Scenes/Level_01/Level_01_Main.unity',
    'Assets/Scenes/Level_01/Level_01_Background.unity',
    'Assets/Scenes/Level_01/Level_01_Gameplay.unity',
    'Assets/Prefabs/Characters/Terraformer/Terraformer_A_1P.prefab',
    'Assets/Prefabs/Characters/Robot/Robot_A_Client.prefab',
    'Assets/Prefabs/Weapons/TerraformerWeapon/Item_TerraformerWeapon_1P.prefab',
]
GUID = re.compile(rb'guid:\s*([0-9a-f]{32})')
TEXT_EXT = {'.unity','.prefab','.mat','.asset','.anim','.controller','.overrideController',
            '.playable','.mask','.vfx','.shadergraph','.shadersubgraph','.meta','.shader','.hlsl','.cginc'}

def preserve_metadata(src, dest, records):
    """Keep folder GUIDs and upstream license notices without importing legacy code."""
    extras = set()
    for name in records:
        for parent in pathlib.Path(name).parents:
            if str(parent) == '.': continue
            meta = pathlib.Path(str(parent) + '.meta')
            if (src/meta).is_file(): extras.add(meta)
    # Keep notice files in their original scopes. A plugin's license does not
    # relicense the other project assets.
    for path in src.rglob('*'):
        if '.git' in path.parts or not path.is_file() or path.suffix == '.meta': continue
        if 'license' in path.name.lower() or 'notice' in path.name.lower():
            extras.add(path.relative_to(src))
            meta = pathlib.Path(str(path) + '.meta')
            if meta.is_file(): extras.add(meta.relative_to(src))
    result = {}
    for rel in sorted(extras):
        data = (src/rel).read_bytes()
        if data.startswith(b'version https://git-lfs.github.com/spec/v1'):
            raise ValueError('Unresolved metadata/notice LFS pointer: ' + str(rel))
        out = dest/rel; out.parent.mkdir(parents=True, exist_ok=True)
        if out.exists() and out.read_bytes() != data:
            raise ValueError('Existing source metadata differs: ' + str(rel))
        if not out.exists(): out.write_bytes(data)
        result[rel.as_posix()] = hashlib.sha256(data).hexdigest()
    return result

def json_guids(value):
    if isinstance(value,dict):
        for key,item in value.items():
            if key == 'guid' and isinstance(item,str) and re.fullmatch('[0-9a-f]{32}',item):yield item.encode()
            yield from json_guids(item)
    elif isinstance(value,list):
        for item in value:yield from json_guids(item)
    elif isinstance(value,str) and value.lstrip().startswith(('{','[')):
        try:yield from json_guids(json.loads(value))
        except (ValueError,RecursionError):pass

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--source',required=True);ap.add_argument('--project',default=str(pathlib.Path(__file__).resolve().parents[1]));a=ap.parse_args()
    src=pathlib.Path(a.source);project=pathlib.Path(a.project);dest=project/'SourceAssets~/FPSSample'
    index={}
    for p in src.rglob('*.meta'):
        m=GUID.search(p.read_bytes())
        if m:index[m[1].decode()]=p.relative_to(src).as_posix()[:-5]
    records={};external={};missing={};errors={};opaque=[]
    pending=set(ROOTS)
    # Original matching animations are explicit roots: clips are also referenced
    # through legacy graph assets which are deliberately not executable here.
    pending.update(p.relative_to(src).as_posix() for p in (src/'Assets/Animation/Characters/Terraformer_1P/Clips').glob('*.fbx'))
    def fetch(rel):
        p=src/rel;out=dest/rel
        if not p.is_file():return rel,None,'source file absent'
        b=p.read_bytes();lfs=b.startswith(b'version https://git-lfs.github.com/spec/v1')
        expected=None
        if lfs:
            expected=re.search(rb'oid sha256:(\w+)',b)[1].decode();size=int(re.search(rb'size (\d+)',b)[1])
            if out.exists() and hashlib.file_digest(out.open('rb'),'sha256').hexdigest()==expected:
                return rel,out.read_bytes(),None
            if shutil.disk_usage(project).free-size < 4*1024**3: return rel,None,'reserve 4 GiB for import; disk space insufficient'
            # GitHub public LFS content endpoint; verify against committed LFS OID.
            url='https://media.githubusercontent.com/media/Unity-Technologies/FPSSample/'+REV+'/'+urllib.parse.quote(rel)
            try:
                with urllib.request.urlopen(url,timeout=90) as response:b=response.read()
                if len(b)!=size or hashlib.sha256(b).hexdigest()!=expected:raise ValueError('LFS hash/size mismatch')
            except Exception as e:return rel,None,str(e)
        out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(b)
        meta=src/(rel+'.meta')
        if meta.exists():shutil.copy2(meta,dest/(rel+'.meta'))
        return rel,b,None
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        while pending:
            batch=sorted(pending);pending=set()
            for rel,b,error in pool.map(fetch,batch):
                if error:errors[rel]=error;continue
                records[rel]={'sha256':hashlib.sha256(b).hexdigest(),'bytes':len(b)}
                meta=src/(rel+'.meta');data=meta.read_bytes() if meta.exists() else b''
                if pathlib.Path(rel).suffix.lower() in TEXT_EXT:
                    if b'\x00' in b[:4096]:opaque.append(rel)
                    else:data+=b
                refs=set(GUID.findall(data))
                if pathlib.Path(rel).suffix.lower() in {'.shadergraph','.shadersubgraph'}:
                    try:refs.update(json_guids(json.loads(b)))
                    except ValueError:pass
                if pathlib.Path(rel).suffix.lower() in {'.shader','.hlsl','.cginc'}:
                    for inc in re.findall(rb'#\s*include(?:_with_pragmas)?\s*"([^"]+)"',b):
                        include=inc.decode()
                        for dep in [pathlib.Path(rel).parent/include,pathlib.Path(include),pathlib.Path('Assets/TextMesh Pro/Resources/Shaders')/include]:
                            candidate=(src/dep).resolve()
                            if candidate.is_relative_to(src.resolve()) and candidate.is_file():
                                name=candidate.relative_to(src.resolve()).as_posix()
                                if name not in records and name not in errors:pending.add(name)
                                break
                for raw in refs:
                    g=raw.decode()
                    if g.startswith('0000000000000000'):continue
                    dep=index.get(g)
                    if not dep:missing.setdefault(g,[]).append(rel);continue
                    if not dep.startswith('Assets/') or pathlib.Path(dep).suffix.lower() in {'.cs','.dll','.so','.bundle'}:
                        external.setdefault(dep,{'guid':g,'referenced_by':[]})['referenced_by'].append(rel);continue
                    if dep not in records and dep not in errors:pending.add(dep)
            print(f'Staged {len(records)} files, {sum(x["bytes"] for x in records.values())/1024**3:.2f} GiB, next {len(pending)}, errors {len(errors)}',flush=True)
            report={'source_commit':REV,'roots':ROOTS,'files':records,'external_code_or_packages':external,'unresolved_guids':missing,'opaque_assets_requiring_editor_scan':opaque,'errors':errors,'status':'source-staged-not-migrated','unity_validation':'NOT RUN'}
            (project/'Reports/fpssample-source-audit.json').write_text(json.dumps(report,indent=2))
    extras = preserve_metadata(src, dest, records)
    (project/'Reports/source-metadata-audit.json').write_text(json.dumps(extras, indent=2))
    if errors:raise SystemExit(1)

if __name__=='__main__':main()
