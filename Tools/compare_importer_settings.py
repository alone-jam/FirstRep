#!/usr/bin/env python3
"""Compare original importer fields with actual Unity serialization; report only."""
import json,re,pathlib,yaml,math,collections
root=pathlib.Path(__file__).resolve().parents[1]
def key(k): return re.sub('[^a-z0-9]','',re.sub(r'^m_','',str(k)).lower())
def walk(old,new,path='',out=None):
 if out is None:out=[]
 if isinstance(old,dict) and isinstance(new,dict):
  nk={key(k):v for k,v in new.items()}
  for k,v in old.items():
   if k in ('serializedVersion','fileIDToRecycleName','externalObjects'):continue
   name=path+'/'+str(k)
   if key(k) not in nk:out.append({'field':name,'status':'not-directly-mapped'})
   else:walk(v,nk[key(k)],name,out)
 elif isinstance(old,list) and isinstance(new,list):
  if len(old)!=len(new):out.append({'field':path,'status':'list-length','original':len(old),'actual':len(new)})
  for i,(a,b) in enumerate(zip(old,new)):walk(a,b,path+'/'+str(i),out)
 elif old!=new and not (isinstance(old,(int,float)) and isinstance(new,(int,float)) and math.isclose(old,new,abs_tol=1e-6)):
  out.append({'field':path,'status':'different','original':old,'actual':new})
 return out
rows=[]
for asset in json.loads((root/'Reports/unity-importer-settings.json').read_text())['assets']:
 p=root/'SourceAssets~/FPSSample'/(asset['path']+'.meta');o=yaml.safe_load(p.read_text());n=json.loads(asset['serializedSettings']);typ=asset['importer'];old=o.get(typ,{})
 diffs=walk(old,n)
 # Only compare actual mesh/clip IDs: transform/root naming remaps require separate Unity reference validation.
 oldids=old.get('fileIDToRecycleName',{}); actual={int(x.split('|',1)[0]):x.split('|',2)[1:] for x in asset['objectIds']};missing=[]
 for ident,name in oldids.items():
  if int(ident)//100000 in (43,74) and int(ident) not in actual:missing.append({'id':ident,'name':name})
 rows.append({'path':asset['path'],'importer':typ,'differences':diffs,'missingOriginalMeshClipIds':missing})
result={'scope':'Mechanical comparison; renamed/unmapped fields and default sentinels require semantic review. Not a visual fidelity pass.','assets':rows,'assetsWithDifferences':sum(bool(a['differences']) for a in rows),'missingMeshClipIds':sum(len(a['missingOriginalMeshClipIds']) for a in rows),'differenceFields':dict(collections.Counter(x['field'] for a in rows for x in a['differences']))}
(root/'Reports/importer-settings-comparison.json').write_text(json.dumps(result,indent=2)+'\n');print({k:v for k,v in result.items() if k not in ('assets','differenceFields')});print(collections.Counter(result['differenceFields']).most_common(30))
