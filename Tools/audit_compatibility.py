#!/usr/bin/env python3
"""Read-only source/package correspondence analysis. Does not assert migration success."""
import argparse, collections, hashlib, json, pathlib, re

GUID = re.compile(rb'(?m)^guid:\s*([0-9a-f]{32})')
INCLUDE = re.compile(r'#\s*include(?:_with_pragmas)?\s*["<]([^">]+)[">]')

def index(root):
    result={}
    for meta in root.rglob('*.meta'):
        match=GUID.search(meta.read_bytes())
        if match:result[match[1].decode()]=str(meta.relative_to(root))[:-5]
    return result

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--source',default='/workspace/upstream/FPSSample');ap.add_argument('--editor-data',default='/workspace/tools/Unity/Hub/Editor/6000.6.5f1/Editor/Data');a=ap.parse_args()
    project=pathlib.Path(__file__).resolve().parents[1];source=pathlib.Path(a.source)
    packages=pathlib.Path(a.editor_data)/'Resources/PackageManager/BuiltInPackages'
    stage=project/'SourceAssets~/FPSSample';audit=json.loads((project/'Reports/fpssample-source-audit.json').read_text())
    old=index(source);new=index(packages);new_by_path={v:k for k,v in new.items()}
    decisions=[]
    for path,info in audit['external_code_or_packages'].items():
        row={'source_path':path,'source_guid':info['guid'],'reference_count':len(info['referenced_by'])}
        if info['guid'] in new:
            row.update(classification='same-guid-modern-package',candidate='Packages/'+new[info['guid']],note='Identity correspondence only; serialized versions/API/visual fidelity still require upgrade validation.')
        elif path.startswith('Packages/') and path[9:] in new_by_path:
            row.update(classification='same-path-different-guid',candidate=path,candidate_guid=new_by_path[path[9:]],note='Do not replace GUID without component/material compatibility review.')
        elif path.startswith('Assets/Scripts/Game/'):
            row.update(classification='legacy-gameplay-boundary',note='Replace game behavior through FirstRep/Ejoy; retain referenced visual payload. No legacy ECS/network runtime import.')
        elif path.endswith('.cs'):
            row.update(classification='render-rig-vfx-or-tool-review',note='Do not blanket strip: inspect implementation and dependencies.')
        else:
            row.update(classification='legacy-package-resource',note='Resolve original payload and modern equivalent; no substitute material/texture.')
        decisions.append(row)
    unresolved=[]
    for guid,refs in audit['unresolved_guids'].items():
        row={'guid':guid,'references':refs,'classification':'absent-from-upstream-and-bundled-package-metadata'}
        if guid in new:row.update(classification='found-in-modern-package',candidate='Packages/'+new[guid])
        # Retain source context; a stale saved material property is not necessarily
        # an active shader input, but must not be dismissed without shader inspection.
        contexts=[]
        for rel in refs[:3]:
            for f in [stage/rel,stage/(rel+'.meta')]:
                if not f.exists():continue
                lines=f.read_text(errors='replace').splitlines()
                for i,line in enumerate(lines):
                    if guid in line:contexts.append({'file':str(f.relative_to(stage)),'line':i+1,'context':lines[max(0,i-2):i+2]})
        row['contexts']=contexts;unresolved.append(row)
    shader_files=[];includes=[];graph_refs=[]
    def walk_json(value,owner):
        if isinstance(value,dict):
            for k,v in value.items():
                if k in ('m_SerializedSubGraph','m_SerializedTexture','m_Source') and isinstance(v,str) and v:
                    graph_refs.append({'file':owner,'field':k,'value':v})
                walk_json(v,owner)
        elif isinstance(value,list):
            for v in value:walk_json(v,owner)
        elif isinstance(value,str) and value.lstrip().startswith(('{','[')):
            try:walk_json(json.loads(value),owner)
            except (ValueError,RecursionError):pass
    for rel in audit['files']:
        if pathlib.Path(rel).suffix.lower() not in {'.shader','.hlsl','.cginc','.shadergraph','.shadersubgraph'}:continue
        shader_files.append(rel);text=(stage/rel).read_text(errors='replace')
        for include in INCLUDE.findall(text):
            paths=[pathlib.Path(rel).parent/include,pathlib.Path(include),pathlib.Path('Assets/TextMesh Pro/Resources/Shaders')/include]
            found=next((str(p) for p in paths if (source/p).is_file()),None)
            builtin=pathlib.Path(a.editor_data)/'Resources/CGIncludes'/include
            builtin_found=builtin.is_file() and builtin.resolve().is_relative_to(pathlib.Path(a.editor_data).resolve())
            includes.append({'owner':rel,'include':include,'source_path':found,'staged':bool(found and (stage/found).exists()),
                            'editor_builtin_path':str(builtin) if builtin_found else None,
                            'note':'Found in installed Editor; shader compatibility still untested' if builtin_found else ('Unresolved source include' if not found else '')})
        if pathlib.Path(rel).suffix.lower() in {'.shadergraph','.shadersubgraph'}:
            try:walk_json(json.loads(text),rel)
            except ValueError:pass
    failures=[]
    for rel,record in audit['files'].items():
        p=stage/rel
        if not p.exists() or hashlib.file_digest(p.open('rb'),'sha256').hexdigest()!=record['sha256']:failures.append(rel)
        original_meta=source/(rel+'.meta');staged_meta=stage/(rel+'.meta')
        if original_meta.exists() and (not staged_meta.exists() or original_meta.read_bytes()!=staged_meta.read_bytes()):failures.append(rel+'.meta')
    result={'status':'STATIC AUDIT ONLY; NOT IMPORTED','source_commit':audit['source_commit'],'counts':dict(collections.Counter(d['classification'] for d in decisions)),
            'boundaries':decisions,'unresolved':unresolved,'shader_files':shader_files,'includes':includes,'serialized_shadergraph_references':graph_refs,'hash_or_meta_mismatches':failures}
    (project/'Reports/compatibility-audit.json').write_text(json.dumps(result,indent=2))
    print(json.dumps({'counts':result['counts'],'unresolved':dict(collections.Counter(d['classification'] for d in unresolved)),'shader_files':len(shader_files),'includes':includes,'graph_references':len(graph_refs),'integrity_failures':len(failures)},indent=2))
    if failures:raise SystemExit(1)

if __name__=='__main__':main()
