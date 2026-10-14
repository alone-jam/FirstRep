#!/usr/bin/env python3
"""Read small official public ZIP entries by HTTP Range; verify ZIP CRC."""
import pathlib,json,urllib.request,struct,zlib,concurrent.futures,hashlib
root=pathlib.Path(__file__).resolve().parents[1];index=json.loads((root/'Reports/official-build-zip-index.json').read_text());url=index['url'];dest=root/'SourceAssets~/OfficialReleaseMetadata';dest.mkdir(exist_ok=True)
def get(start,length):
 with urllib.request.urlopen(urllib.request.Request(url,headers={'Range':f'bytes={start}-{start+length-1}'}),timeout=30) as r:
  if r.status!=206:raise RuntimeError('Expected bounded HTTP Range')
  return r.read(length)
def entry(e):
 h=get(e['offset'],30);v=struct.unpack('<4s5H3I2H',h)
 if v[0]!=b'PK\x03\x04' or v[2]&1:raise RuntimeError('Unsupported/encrypted entry')
 raw=get(e['offset']+30+v[-2]+v[-1],e['compressed']);data=zlib.decompress(raw,-15) if v[3]==8 else raw
 if len(data)!=e['uncompressed'] or zlib.crc32(data)!=v[6]:raise RuntimeError('ZIP integrity mismatch '+e['name'])
 p=dest/e['name'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
 return {'name':e['name'],'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'crcVerified':True}
if __name__=='__main__':
 selected=[e for e in index['entries'] if e['name'].endswith('.manifest') or ('license' in e['name'].lower() or 'notice' in e['name'].lower())]
 with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:rows=list(pool.map(entry,selected))
 (root/'Reports/official-release-manifests.json').write_text(json.dumps({'url':url,'entries':rows},indent=2)+'\n');print('Verified metadata entries',len(rows),'bytes',sum(e['bytes'] for e in rows))
