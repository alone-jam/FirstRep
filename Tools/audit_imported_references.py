#!/usr/bin/env python3
import json,pathlib,re,collections,yaml
root=pathlib.Path(__file__).resolve().parents[1];source=root/'SourceAssets~/FPSSample'
rows=json.loads((root/'Reports/unity-importer-settings.json').read_text())['assets'];models={}
for a in rows:
 if a['importer']!='ModelImporter':continue
 m=yaml.safe_load((source/(a['path']+'.meta')).read_text());models[m['guid']]={'path':a['path'],'ids':{int(s.split('|',1)[0]) for s in a['objectIds']},'names':m['ModelImporter'].get('fileIDToRecycleName',{})}
refs=[];checked=0
for p in source.rglob('*'):
 if not p.is_file() or p.suffix.lower() not in ('.prefab','.unity','.asset','.controller','.overridecontroller','.anim','.mat'):continue
 text=p.read_text(errors='replace')
 for match in re.finditer(r'\{fileID: (\d+), guid: ([0-9a-f]{32}), type: \d+\}',text):
  ident=int(match[1]);guid=match[2]
  if guid not in models or ident//100000 not in (43,74):continue
  checked+=1;m=models[guid]
  if ident not in m['ids']:refs.append({'file':str(p.relative_to(source)),'line':text.count('\n',0,match.start())+1,'guid':guid,'fileID':ident,'model':m['path'],'originalName':m['names'].get(ident)})
report={'scope':'Actual referenced Mesh/AnimationClip local IDs checked against real Unity imported objects; excludes other object types and appearance.','checkedReferences':checked,'unresolvedReferences':refs}
(root/'Reports/imported-mesh-clip-reference-audit.json').write_text(json.dumps(report,indent=2)+'\n');print('checked',checked,'unresolved',len(refs));print(refs[:5])
