#!/usr/bin/env python3
"""Run the real Editor in the caller's authenticated context with a disk reserve.

This does not authenticate or change credential storage. Launch from the retained
PTY; restarting it with an unrelated exec command changes the Linux keyring.
"""
import os, pathlib, shutil, subprocess, sys, time

editor = '/workspace/tools/Unity/Hub/Editor/6000.6.5f1/Editor/Unity'
project = pathlib.Path(__file__).resolve().parents[1]
reserve = 1024**3
if not (project / 'Temp').exists() and not (project / 'Temp').is_symlink():
    (project / 'Temp').mkdir()
storage_paths = [project, (project / 'Temp').resolve(), (project / 'Builds').resolve(), (project / 'Library/PlayerDataCache').resolve()]
for path in storage_paths:
    if not path.exists() or shutil.disk_usage(path).free < reserve:
        sys.exit('Insufficient disk reserve before launch: ' + str(path))
process = subprocess.Popen([editor, *sys.argv[1:]], cwd=project)
stopped_for_disk = False
while process.poll() is None:
    exhausted = next((path for path in storage_paths if shutil.disk_usage(path).free < reserve), None)
    if exhausted is not None:
        print('Stopping this Editor import: free disk fell below the 1 GiB reserve at ' + str(exhausted) + '. Original art quality will not be reduced.', flush=True)
        process.terminate(); stopped_for_disk = True
        try: process.wait(timeout=30)
        except subprocess.TimeoutExpired: process.kill(); process.wait()
        break
    time.sleep(0.5)
print('Unity exit:', process.returncode, 'disk-reserve-stop:', stopped_for_disk, flush=True)
sys.exit(75 if stopped_for_disk else process.returncode)
