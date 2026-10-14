#!/usr/bin/env python3
import pathlib,json,struct,re,subprocess,os
root=pathlib.Path(__file__).resolve().parents[1];stage=root/'SourceAssets~/FPSSample';repo='/workspace/upstream/FPSSample'
def geometry_names(path):
 data=path.read_bytes();names=[]
 if not data.startswith(b'Kaydara FBX Binary'):
  return {'format':'ascii','geometry':[[None,n.decode(), 'Mesh'] for n in re.findall(rb'Model:\s*\d+,\s*"([^"]+)"',data)]}
 version=struct.unpack_from('<I',data,23)[0];fmt='<QQQ' if version>=7500 else '<III';header=25 if version>=7500 else 13
 def walk(pos,end):
  while pos+header<=end:
   finish,nprops,plen=struct.unpack_from(fmt,data,pos);namelen=data[pos+header-1]
   if not finish:return
   name=data[pos+header:pos+header+namelen].decode(errors='replace');p=pos+header+namelen;values=[]
   for _ in range(nprops):
    t=chr(data[p]);p+=1
    sizes={'Y':2,'C':1,'I':4,'F':4,'D':8,'L':8}
    if t in sizes:
     value=struct.unpack_from({'Y':'<h','C':'<?','I':'<i','F':'<f','D':'<d','L':'<q'}[t],data,p)[0];p+=sizes[t]
    elif t in 'SR':
     size=struct.unpack_from('<I',data,p)[0];p+=4;value=data[p:p+size].decode(errors='replace') if t=='S' else None;p+=size
    elif t in 'fdlibc':
     count,encoding,size=struct.unpack_from('<III',data,p);p+=12+size;value=None
    else:raise ValueError(t)
    values.append(value)
   if name=='Model' and values[-1]=='Mesh':names.append(values[:3])
   walk(pos+header+namelen+plen,finish);pos=finish
 walk(27,len(data));return {'format':'binary','version':version,'geometry':names}
rows=[]
for x in json.loads((root/'Reports/imported-mesh-clip-reference-audit.json').read_text())['unresolvedReferences']:
 txt=(stage/x['file']).read_text();before=txt[:sum(len(s)+1 for s in txt.splitlines()[:x['line']-1])];block=re.split(r'(?m)^--- ',before)[-1];typ=re.search(r'\n(\w+):',block);x['component']=typ[1] if typ else None;x['enabled']=re.search(r'm_Enabled: (\d+)',block)[1] if re.search(r'm_Enabled: (\d+)',block) else None
 x['originalFbxGeometry']=geometry_names(stage/x['model']);x['exactOriginalMeshNamePresent']=any(x['originalName'] in str(n[1]).split('\x00')[0].split('::') for n in x['originalFbxGeometry']['geometry'])
 x['sourceHistory']=subprocess.check_output(['git','-C',repo,'log','--all','--format=%H','--',x['model']],env={**os.environ,'GIT_NO_LAZY_FETCH':'1'}).decode().splitlines();rows.append(x)
result={'scope':'Direct original FBX binary mesh Model records and serialized prefab component review, no mutation or guessed remapping','rows':rows};(root/'Reports/ceiling-reference-review.json').write_text(json.dumps(result,indent=2)+'\n');print('reviewed',len(rows),'components',set(x['component'] for x in rows),'exactNamePresent',sum(x['exactOriginalMeshNamePresent'] for x in rows));print('histories',set(tuple(x['sourceHistory']) for x in rows));print(rows[0]['originalFbxGeometry'])
