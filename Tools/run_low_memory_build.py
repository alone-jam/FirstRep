#!/usr/bin/env python3
"""Build a Player from previously prepared, integrity-checked bundles with low concurrency.

Linux/cgroup environment diagnostic; logs process names/RSS and disk space, never credentials.
Generate bundles from the current assets before using this explicit staged entry point.
"""
import subprocess,time,json,pathlib,os,sys,argparse
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument("--stage",choices=["bundles","player","scripts"],default="player")
parser.add_argument("--log-prefix",default="official-a5e1ec6-build-low-memory")
options=parser.parse_args()
if pathlib.Path(options.log_prefix).name!=options.log_prefix:
 parser.error("log prefix must be a filename, without directories")
method="FirstRep.Fps.Editor.SliceWorkbench."+{"bundles":"BuildFrameworkResources","player":"BuildLinuxFromPreparedResources","scripts":"BuildLinuxScriptsOnly"}[options.stage]
root=pathlib.Path(__file__).resolve().parents[1];report=root/('Reports/'+options.log_prefix+'-samples.jsonl')
report.parent.mkdir(parents=True,exist_ok=True)
args=['python3','Tools/run_unity_checked.py','-batchmode','-nographics','-quit','-job-worker-count','1','-diag-debug-shader-compiler','-refreshImportMode','InProcess','-desiredWorkerCount','1','-standbyWorkerCount','0','-projectPath',str(root),'-executeMethod',method,'-logFile',str(root/('Reports/'+options.log_prefix+'.log'))]
env=os.environ.copy();env.update(FIRSTREP_BUILD_TEMP='/tmp/firstrep-intermediates/Temp-active',UPM_CACHE_ROOT='/workspace/tools/state/upm',XDG_CONFIG_HOME='/workspace/tools/state/config',XDG_CACHE_HOME='/workspace/tools/state/cache',XDG_DATA_HOME='/workspace/tools/state/data')
with (root/('Reports/'+options.log_prefix+'-launcher.log')).open('w') as log,report.open('w') as f:
 p=subprocess.Popen(args,cwd=root,env=env,stdout=log,stderr=subprocess.STDOUT)
 peak=0
 while p.poll() is None:
  stat=dict((line.split()[0],int(line.split()[1])) for line in pathlib.Path('/sys/fs/cgroup/memory.stat').read_text().splitlines())
  sample={'time':time.time(),'memory_stat':{key:stat[key] for key in ['anon','file','shmem','file_mapped','file_dirty']},'memory_current':int(pathlib.Path('/sys/fs/cgroup/memory.current').read_text()),'processes':[],'disk':{}}
  peak=max(peak,sample['memory_current'])
  for d in pathlib.Path('/proc').iterdir():
   if not d.name.isdigit():continue
   try:
    name=(d/'comm').read_text().strip()
    if not (name.startswith('Unity') or name in ['dotnet','FirstRep.x86_64','AssetImportWork','MainThread']):continue
    status=(d/'status').read_text();rss=next((int(x.split()[1])*1024 for x in status.splitlines() if x.startswith('VmRSS:')),0)
    if rss:sample['processes'].append({'pid':int(d.name),'name':name,'rss_bytes':rss})
   except OSError:pass
  for path in ['/workspace','/tmp']:
   st=os.statvfs(path);sample['disk'][path]=st.f_bavail*st.f_frsize
  f.write(json.dumps(sample)+'\n');f.flush();time.sleep(2)
 print(json.dumps({'exit_code':p.returncode,'peak_cgroup_bytes':peak,'samples':str(report)}),flush=True)

sys.exit(p.returncode)
