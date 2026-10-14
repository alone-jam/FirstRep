#!/usr/bin/env python3
"""Run the real Editor in the caller's authenticated context with a disk reserve.

This does not authenticate or change credential storage. Launch from the retained
PTY; restarting it with an unrelated exec command changes the Linux keyring.
"""
import os, pathlib, shutil, subprocess, sys, time

editor = '/workspace/tools/Unity/Hub/Editor/6000.6.5f1/Editor/Unity'
project = pathlib.Path(__file__).resolve().parents[1]
reserve = 1024**3

def free_bytes(path):
    # Unity removes/recreates Temp during startup and shutdown. Check its
    # nearest existing parent on the same filesystem instead of losing the
    # reserve monitor to a transient FileNotFoundError.
    existing = path
    while not existing.exists():
        if existing == existing.parent:
            raise FileNotFoundError(str(path))
        existing = existing.parent
    return shutil.disk_usage(existing).free
if not (project / 'Temp').exists() and not (project / 'Temp').is_symlink():
    (project / 'Temp').mkdir()
storage_paths = [project, (project / 'Temp').resolve(), (project / 'Builds').resolve(), (project / 'Library/PlayerDataCache').resolve()]
for path in storage_paths:
    if not path.exists() or free_bytes(path) < reserve:
        sys.exit('Insufficient disk reserve before launch: ' + str(path))
process = subprocess.Popen([editor, *sys.argv[1:]], cwd=project)
stopped_for_disk = False
while process.poll() is None:
    exhausted = next((path for path in storage_paths if free_bytes(path) < reserve), None)
    if exhausted is not None:
        print('Stopping this Editor import: free disk fell below the 1 GiB reserve at ' + str(exhausted) + '. Original art quality will not be reduced.', flush=True)
        process.terminate(); stopped_for_disk = True
        try: process.wait(timeout=30)
        except subprocess.TimeoutExpired: process.kill(); process.wait()
        break
    time.sleep(0.5)
print('Unity exit:', process.returncode, 'disk-reserve-stop:', stopped_for_disk, flush=True)
sys.exit(75 if stopped_for_disk else process.returncode)
