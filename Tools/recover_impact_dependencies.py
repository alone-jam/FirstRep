#!/usr/bin/env python3
import pathlib,re,json,urllib.request,urllib.parse,hashlib,shutil,os
r=pathlib.Path(__file__).resolve().parents[1];repo=pathlib.Path('/workspace/upstream/FPSSample');stage=r/'SourceAssets~/FPSSample';rev='c8375e7cf29cd0bcc3764b0650028a99aecb7475';idx={}
for p in repo.rglob('*.meta'):
 m=re.search(rb'(?m)^guid:\s*([a-f0-9]+)',p.read_bytes())
 if m:idx[m[1].decode()]=str(p.relative_to(repo))[:-5]
todo=['Assets/Materials/Effects/Impacts/Impact_Terraformer_Weapon_A/Impact_Terraformer_Weapon_A.mat'];seen=set();records={}
while todo:
 rel=todo.pop()
 if rel in seen:continue
 seen.add(rel);p=repo/rel
 if not p.exists() or not rel.startswith('Assets/') or p.suffix.lower() in ('.cs','.dll'):continue
 b=p.read_bytes();src=stage/rel
 if b.startswith(b'version https://git-lfs.github.com/spec/v1'):
  sha=re.search(rb'oid sha256:(\w+)',b)[1].decode()
  if src.exists():b=src.read_bytes()
  else:b=urllib.request.urlopen('https://media.githubusercontent.com/media/Unity-Technologies/FPSSample/'+rev+'/'+urllib.parse.quote(rel),timeout=60).read()
  assert hashlib.sha256(b).hexdigest()==sha
 src.parent.mkdir(parents=True,exist_ok=True)
 if not src.exists():src.write_bytes(b)
 meta=pathlib.Path(str(p)+'.meta');sm=pathlib.Path(str(src)+'.meta')
 if meta.exists() and not sm.exists():shutil.copy2(meta,sm)
 dest=r/rel;dest.parent.mkdir(parents=True,exist_ok=True)
 if not dest.exists():
  if p.suffix.lower() in ('.tif','.tga','.png','.exr','.fbx','.wav','.psd'):os.link(src,dest);src.chmod(src.stat().st_mode&~0o222)
  else:dest.write_bytes(b)
  if sm.exists():shutil.copy2(sm,str(dest)+'.meta')
 records[rel]={'sha256':hashlib.sha256(b).hexdigest(),'bytes':len(b)}
 if b'\0' not in b[:4096]:
  for guid in re.findall(rb'guid:\s*([a-f0-9]+)',b):
   if guid.decode() in idx:todo.append(idx[guid.decode()])
a=json.loads((r/'Reports/fpssample-source-audit.json').read_text());a['files'].update(records);(r/'Reports/fpssample-source-audit.json').write_text(json.dumps(a,indent=2)+'\n');(r/'Reports/impact-dependency-recovery.json').write_text(json.dumps(records,indent=2)+'\n');print('Verified',len(records),'original impact dependencies')
