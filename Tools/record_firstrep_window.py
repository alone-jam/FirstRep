#!/usr/bin/env python3
"""Capture an actual FirstRep X11 window and inject real OS inputs.

Requires libX11, libXtst, Pillow and ffmpeg. Use xwininfo to identify the fresh
Player window (640x360 by default; specify --width/--height to match); this script does not launch a Player or manufacture game frames.
"""
import argparse,ctypes as C,json,os,subprocess,time
from pathlib import Path
from PIL import Image
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('window',type=lambda value:int(value,0))
parser.add_argument('action',choices=['shot','focus','resume','close-console','record'])
parser.add_argument('--duration',type=float,default=32)
parser.add_argument('--input-time-scale',type=float,default=1)
parser.add_argument('--width',type=int,default=640)
parser.add_argument('--height',type=int,default=360)
parser.add_argument('--hold',type=float,default=.5)
parser.add_argument('--output',type=Path,default=Path(__file__).resolve().parents[1]/'Reports/GifCapture')
args=parser.parse_args();args.output.mkdir(parents=True,exist_ok=True)
X=C.CDLL('libX11.so.6');T=C.CDLL('libXtst.so.6')
class XImage(C.Structure):
 _fields_=[('width',C.c_int),('height',C.c_int),('xoffset',C.c_int),('format',C.c_int),('data',C.c_void_p),('byte_order',C.c_int),('bitmap_unit',C.c_int),('bitmap_bit_order',C.c_int),('bitmap_pad',C.c_int),('depth',C.c_int),('bytes_per_line',C.c_int),('bits_per_pixel',C.c_int)]
X.XOpenDisplay.argtypes=[C.c_char_p];X.XOpenDisplay.restype=C.c_void_p
X.XGetImage.argtypes=[C.c_void_p,C.c_ulong,C.c_int,C.c_int,C.c_uint,C.c_uint,C.c_ulong,C.c_int];X.XGetImage.restype=C.POINTER(XImage)
X.XDestroyImage.argtypes=[C.POINTER(XImage)]
X.XSetInputFocus.argtypes=[C.c_void_p,C.c_ulong,C.c_int,C.c_ulong];X.XFlush.argtypes=[C.c_void_p]
X.XWarpPointer.argtypes=[C.c_void_p,C.c_ulong,C.c_ulong,C.c_int,C.c_int,C.c_uint,C.c_uint,C.c_int,C.c_int]
X.XStringToKeysym.argtypes=[C.c_char_p];X.XStringToKeysym.restype=C.c_ulong
X.XKeysymToKeycode.argtypes=[C.c_void_p,C.c_ulong];X.XKeysymToKeycode.restype=C.c_uint
T.XTestFakeKeyEvent.argtypes=[C.c_void_p,C.c_uint,C.c_int,C.c_ulong]
T.XTestFakeButtonEvent.argtypes=[C.c_void_p,C.c_uint,C.c_int,C.c_ulong]
T.XTestFakeRelativeMotionEvent.argtypes=[C.c_void_p,C.c_int,C.c_int,C.c_ulong]
d=X.XOpenDisplay(os.environ.get('DISPLAY',':99').encode());assert d,'Cannot open X11 display'
window=args.window

def frame():
 p=X.XGetImage(d,window,0,0,args.width,args.height,C.c_ulong(-1).value,2);assert p,'Cannot capture Player window'
 try:
  v=p.contents;assert v.bits_per_pixel==32 and v.byte_order==0
  raw=C.string_at(v.data,v.bytes_per_line*v.height);stride=v.bytes_per_line
  if stride!=args.width*4:raw=b''.join(raw[y*stride:y*stride+args.width*4] for y in range(args.height))
  return raw
 finally:X.XDestroyImage(p)

def shot(name):Image.frombytes('RGB',(args.width,args.height),frame(),'raw','BGRX').save(args.output/name)
def key(name,down):
 code=X.XKeysymToKeycode(d,X.XStringToKeysym(name.encode()));assert code
 T.XTestFakeKeyEvent(d,code,int(down),0);X.XFlush(d)
def button(n,down):T.XTestFakeButtonEvent(d,n,int(down),0);X.XFlush(d)
def click(x,y):
 X.XWarpPointer(d,0,window,0,0,0,0,int(x*args.width/640),int(y*args.height/360));X.XFlush(d);time.sleep(.2)
 button(1,True);time.sleep(args.hold);button(1,False);time.sleep(1)
def motion(x,y):T.XTestFakeRelativeMotionEvent(d,x,y,0);X.XFlush(d)
if args.action=='shot':shot('fresh-player.png');raise SystemExit()
X.XSetInputFocus(d,window,2,0);X.XFlush(d);time.sleep(.5)
if args.action=='focus':shot('fresh-focused.png');raise SystemExit()
if args.action=='resume':click(320,168);shot('fresh-resumed.png');raise SystemExit()
if args.action=='close-console':
 key('Escape',True);time.sleep(args.hold);key('Escape',False);time.sleep(1)
 click(355,335);click(320,168);shot('fresh-console-closed.png');raise SystemExit()
actions=[
 (3,lambda:button(3,True),'aim'),
 (5,lambda:button(1,True),'fire'),
 (8,lambda:button(1,False),'stop firing'),
 (9,lambda:button(3,False),'stop aiming'),
 (11,lambda:key('r',True),'reload'),
 (13,lambda:key('r',False),'release reload'),
 (17,lambda:key('w',True),'walk forward'),
 (19,lambda:key('w',False),'stop walking'),
 (22,lambda:motion(35,0),'look right'),
 (26,lambda:key('a',True),'strafe left'),
 (29,lambda:key('a',False),'stop strafing')]
actions=[(at*args.input_time_scale,action,label) for at,action,label in actions]
encoder=subprocess.Popen(['ffmpeg','-y','-loglevel','error','-f','rawvideo','-pixel_format','bgr0','-video_size',f'{args.width}x{args.height}','-framerate','8','-i','pipe:0','-c:v','ffv1',str(args.output/'fresh-runtime.mkv')],stdin=subprocess.PIPE)
start=time.monotonic();count=0;events=[]
try:
 while time.monotonic()-start<args.duration:
  elapsed=time.monotonic()-start
  while actions and elapsed>=actions[0][0]:
   _,action,label=actions.pop(0);action();events.append({'seconds':elapsed,'action':label})
  encoder.stdin.write(frame());count+=1;deadline=start+count/8
  if deadline>time.monotonic():time.sleep(deadline-time.monotonic())
finally:
 for name in ['w','a','r']:key(name,False)
 for n in [1,3]:button(n,False)
 encoder.stdin.close();encoder.wait()
report={'frames':count,'capture_seconds':time.monotonic()-start,'window':window,'size':[args.width,args.height],'fps':8,'input_events':events,'encoder_exit':encoder.returncode}
(args.output/'fresh-recording.json').write_text(json.dumps(report,indent=2)+'\n');shot('fresh-after.png');print(json.dumps(report))
raise SystemExit(encoder.returncode)
