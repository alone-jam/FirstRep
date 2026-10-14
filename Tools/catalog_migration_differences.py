#!/usr/bin/env python3
import pathlib,json,re,collections
r=pathlib.Path(__file__).resolve().parents[1];repo=pathlib.Path('/workspace/upstream/FPSSample');idx={}
for p in repo.rglob('*.meta'):
 m=re.search(r'(?m)^guid:\s*(\w+)',p.read_text(errors='replace'))
 if m:idx[m[1]]=str(p.relative_to(repo))[:-5]
known={}
for base in [r/'Assets',r/'Packages',r/'Library/PackageCache']:
 for p in base.rglob('*.cs.meta'):
  m=re.search(r'(?m)^guid:\s*(\w+)',p.read_text(errors='replace'))
  if m:known[m[1]]=str(p)
rows={}
for p in (r/'SourceAssets~/FPSSample/Assets').rglob('*'):
 if p.suffix not in ('.prefab','.unity'):continue
 for g in re.findall(r'm_Script:.*?guid: (\w+)',p.read_text(errors='replace')):
  if g in known:continue
  path=idx.get(g,'Unresolved original script/legacy assembly');row=rows.setdefault(g,{'guid':g,'originalScript':path,'references':[],'reason':'legacy component unsupported by the migrated Unity/Ejoy stack; original geometry and transforms retained unless separately listed'})
  row['references'].append(str(p.relative_to(r/'SourceAssets~/FPSSample')))
  if '/Rope' in path:row['reason']='Editor rope authoring component omitted; serialized original LineRenderer geometry retained.'
  elif 'ProjectLODLightmapManager' in path:row['reason']='Editor bake manager omitted; existing LOD lightmap projection applied via preserved ProjectLODLightmaps.'
  elif 'Anim' in path:row['reason']='Old gameplay animation graph not executed; selected original clips wired into local Unity Animator states, unused motions omitted.'
  elif 'PostProcessing' in path or 'postprocessing' in path:row['reason']='PostProcessing v2 component not executed; HDRP17 Volume compatibility requires visual calibration.'
  elif 'VFX' in path:row['reason']='Legacy VFX controller/binding omitted pending supported API conversion; graphs and native VisualEffect components retained where loadable.'
  elif 'Sound' in path or 'Audio' in path:row['reason']='Old game audio service binding omitted; selected original clips played through local gameplay, original ambient behavior may differ.'
(r/'Reports/unsupported-original-component-inventory.json').write_text(json.dumps(list(rows.values()),indent=2)+'\n');print('Recorded',len(rows),'unsupported script/assembly identities')
