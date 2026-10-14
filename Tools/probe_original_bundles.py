#!/usr/bin/env python3
"""Read UnityFS compression/container metadata with existing system liblz4.
No export, asset recreation, GUID rewriting, installation or access-control bypass.
"""
import pathlib,struct,io,ctypes,ctypes.util,json,collections,re,lzma
root=pathlib.Path(__file__).resolve().parents[1];lib=ctypes.CDLL(ctypes.util.find_library('lz4'));lib.LZ4_decompress_safe.argtypes=[ctypes.c_void_p,ctypes.c_void_p,ctypes.c_int,ctypes.c_int];lib.LZ4_decompress_safe.restype=ctypes.c_int
class R:
 def __init__(self,data,endian='>'):self.f=io.BytesIO(data);self.e=endian
 def read(self,n):return self.f.read(n)
 def val(self,s):return struct.unpack(self.e+s,self.read(struct.calcsize(s)))[0]
 def z(self):
  a=bytearray()
  while True:
   c=self.read(1)
   if c==b'\0':return a.decode(errors='replace')
   if not c:raise EOFError()
   a.extend(c)
 def align(self,n=4):self.f.seek((-self.f.tell())%n,1)
def dec(b,n,flags):
 mode=flags&63
 if mode==0:return b
 if mode in (2,3):
  out=ctypes.create_string_buffer(n);got=lib.LZ4_decompress_safe(b,out,len(b),n)
  if got!=n:raise ValueError('lz4 length')
  return out.raw
 raise ValueError('Unsupported compression '+str(mode))
def serial(data):
 r=R(data);metadataSize=r.val('I');fileSize=r.val('I');version=r.val('I');offset=r.val('I');endian=r.val('B');r.read(3)
 if version!=17:return {'serializedVersion':version,'scope':'version not parsed'}
 r.e='>' if endian else '<';uv=r.z();target=r.val('i');tree=r.val('?');count=r.val('i');types=[]
 for _ in range(count):
  cls=r.val('i');stripped=r.val('?');script=r.val('h')
  if cls==114:r.read(16)
  r.read(16)
  if tree:
   nodes=r.val('i');strings=r.val('i');r.read(nodes*24+strings)
  types.append(cls)
 objects=[];n=r.val('i')
 for _ in range(n):
  r.align();pid=r.val('q');start=r.val('I')+offset;size=r.val('I');ti=r.val('i');cls=types[ti];row={'pathID':pid,'classID':cls,'bytes':size};blob=data[start:start+size]
  if cls in (21,28,43,49,74,83,90,91,142,156) and len(blob)>4:
   length=struct.unpack_from(r.e+'i',blob)[0]
   if 0<=length<=min(len(blob)-4,4096):row['name']=blob[4:4+length].decode(errors='replace')
  objects.append(row)
 scripts=r.val('i')
 for _ in range(scripts):r.val('i');r.align();r.val('q')
 external=[]
 for _ in range(r.val('i')):
  empty=r.z();guid=r.read(16).hex();typ=r.val('i');path=r.z();external.append({'guidRawHex':guid,'type':typ,'path':path})
 return {'unityVersion':uv,'targetPlatform':target,'serializedVersion':version,'typeTreePresent':tree,'classes':dict(collections.Counter(types)),'objects':objects,'externals':external}
def probe(path):
 b=path.read_bytes();r=R(b);sig=r.z();ver=r.val('I');uv=r.z();rev=r.z();size=r.val('Q');ci=r.val('I');ui=r.val('I');flags=r.val('I');header=r.f.tell()
 if sig!='UnityFS':raise ValueError(sig)
 if ver>=7:r.align(16);header=r.f.tell()
 if flags&128:info=dec(b[-ci:],ui,flags);p=header
 else:info=dec(b[header:header+ci],ui,flags);p=header+ci
 ir=R(info);ir.read(16);blocks=[]
 for _ in range(ir.val('I')):blocks.append((ir.val('I'),ir.val('I'),ir.val('H')))
 nodes=[]
 for _ in range(ir.val('I')):nodes.append({'offset':ir.val('Q'),'size':ir.val('Q'),'flags':ir.val('I'),'name':ir.z()})
 if flags&512:p=(p+15)//16*16
 chunks=[]
 for u,c,f in blocks:chunks.append(dec(b[p:p+c],u,f));p+=c
 data=b''.join(chunks);results=[]
 for node in nodes:
  if not node['name'].endswith(('.resS','.resource')):
   results.append({'name':node['name'],'serialized':serial(data[node['offset']:node['offset']+node['size']])})
 return {'bundle':str(path.relative_to(root)),'signature':sig,'formatVersion':ver,'unityVersion':uv,'revision':rev,'nodes':nodes,'serializedFiles':results,'scope':'Runtime object names/types/local pathIDs and external references only; not original editor GUID/meta reconstruction.'}
if __name__=='__main__':
 rows=[]
 for name in ['52dd235007d50bc4598d3f524e699f8a','aa4dcfa5e3fda1742a3fcb1a62bb6e8d']:
  p=root/'SourceAssets~/OfficialReleaseMetadata/AssetBundles/bundledresources/client_assets'/name;rows.append(probe(p))
 (root/'Reports/official-bundle-runtime-probe.json').write_text(json.dumps(rows,indent=2)+'\n')
 for row in rows:
  print(row['bundle'],row['revision'])
  for f in row['serializedFiles']:
   d=f['serialized'];print('file',f['name'],'version',d.get('serializedVersion'),'objects',len(d.get('objects',[])),'externals',d.get('externals'));print('animations',[o.get('name') for o in d.get('objects',[]) if o['classID']==74]);print('avatars',[o.get('name') for o in d.get('objects',[]) if o['classID']==90])
