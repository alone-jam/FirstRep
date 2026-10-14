#!/usr/bin/env python3
import json,pathlib,re,collections
root=pathlib.Path(__file__).resolve().parents[1];stage=root/'SourceAssets~/FPSSample';repo=pathlib.Path('/workspace/upstream/FPSSample');d=json.loads((root/'Reports/missing-guid-investigation.json').read_text());missing={x['guid']:x for x in d['items'] if x['classification']=='not-supplied-in-public-source-history'}
idx={}
for p in repo.rglob('*.meta'):
 m=re.search(r'(?m)^guid: ([0-9a-f]{32})',p.read_text(errors='replace'))
 if m:idx[m[1]]=str(p.relative_to(repo))[:-5]
cache={}
def reach(rel):
 seen=set();found=set();todo=[rel]
 while todo:
  p=todo.pop()
  if p in seen:continue
  seen.add(p);f=stage/p
  if not f.exists():f=repo/p
  if not f.is_file() or f.suffix.lower() not in ('.prefab','.mat','.asset','.controller','.unity','.vfx','.shadergraph'):continue
  if p not in cache:cache[p]=set(re.findall(r'guid:\s*([0-9a-f]{32})',f.read_text(errors='replace')))
  for g in cache[p]:
   if g in missing:found.add(g)
   elif g in idx:todo.append(idx[g])
 return sorted(found)
roots=json.loads((root/'Reports/fpssample-source-audit.json').read_text())['roots'];out={p:reach(p) for p in roots};candidates=[]
for p in stage.glob('Assets/Prefabs/Environment/Levels/Level_01/*Floor*.prefab'):
 rel=str(p.relative_to(stage));hit=reach(rel)
 if not hit:candidates.append(rel)
result={'scope':'Closure against 40 known missing GUIDs only; excludes importer-ID/script/render compatibility and is not a playable/visual validation.','roots':out,'floorPrefabsWithoutKnownMissingGuids':candidates};(root/'Reports/minimal-scope-source-review.json').write_text(json.dumps(result,indent=2)+'\n');print(out);print('floorCandidates',len(candidates),candidates[:5])
