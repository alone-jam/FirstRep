#!/usr/bin/env python3
"""Redirect Unity Temp after Editor startup, preserving startup files."""
import hashlib, json, os, pathlib, shutil, time

project = pathlib.Path(__file__).resolve().parents[1]
requested = os.environ.get('FIRSTREP_BUILD_TEMP')
if requested:
    destination = pathlib.Path(requested).resolve()
    if not destination.is_relative_to('/tmp/firstrep-intermediates'):
        raise RuntimeError('Build scratch must be inside /tmp/firstrep-intermediates')
    destination.mkdir(parents=True, exist_ok=True)
    source = project / 'Temp'
    if source.resolve() != destination:
        if source.is_symlink():
            raise RuntimeError('Unexpected Temp link; preserve it and inspect manually')
        files = [p for p in source.rglob('*') if p.is_file()]
        if sum(p.stat().st_size for p in files) > 1024**2:
            raise RuntimeError('Temp already contains build data; refusing live migration')
        shutil.copytree(source, destination, dirs_exist_ok=True)
        for p in files:
            if p.read_bytes() != (destination / p.relative_to(source)).read_bytes():
                raise RuntimeError('Startup file verification failed')
        backup = project / ('Temp-startup-preserved-' + str(time.time_ns()))
        source.rename(backup)
        source.symlink_to(destination, target_is_directory=True)
        report = {'startupFilesPreserved': str(backup), 'activeTemp': str(destination),
                  'verifiedFiles': len(files)}
        (project / 'Reports' / ('temp-redirect-' + str(time.time_ns()) + '.json')).write_text(json.dumps(report, indent=2))
    print('Verified build Temp: ' + str(source.resolve()), flush=True)
