#!/usr/bin/env python3
"""Read-only gate: a preserved Player must not silently stand in for current source."""
import argparse
import hashlib
import json
import pathlib
import re
import subprocess
import sys


def verify_player(project, player):
    project, player = pathlib.Path(project), pathlib.Path(player).resolve()
    manifest = player.parent / 'PLAYER-MANIFEST.json'
    if not player.is_file() or not manifest.is_file():
        raise ValueError('Current Player provenance is missing. Rebuild/package the current source and fork; preserved Players remain historical.')
    record = json.loads(manifest.read_text())
    fork = json.loads((project / 'Docs/UpstreamVersions.json').read_text())['EjoyFramework']['commit']
    unity = json.loads((project / 'Tools/unity-environment.json').read_text())['editorVersion']
    if record.get('frameworkCommit') != fork or record.get('unityVersion') != unity:
        raise ValueError('Player fork/Unity revision differs from the current project; rebuild required.')
    source = record.get('sourceCommit', '')
    if not re.fullmatch(r'[0-9a-f]{40}', source):
        raise ValueError('Player source commit is invalid.')
    diff = subprocess.run(['git', 'diff', '--quiet', source, '--', 'Assets', 'Packages', 'ProjectSettings'], cwd=project, capture_output=True)
    extra = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '--', 'Assets', 'Packages', 'ProjectSettings'], cwd=project)
    if diff.returncode or extra:
        raise ValueError('Player inputs differ from current Assets/Packages/ProjectSettings; rebuild required.')
    entries = record.get('files')
    if not isinstance(entries, list) or not entries or not any(e.get('path') == player.name for e in entries):
        raise ValueError('Player file/hash manifest is incomplete.')
    for entry in entries:
        relative = pathlib.PurePosixPath(entry['path'])
        if relative.is_absolute() or '..' in relative.parts:
            raise ValueError('Player manifest path leaves the Player directory.')
        path = player.parent.joinpath(*relative.parts)
        if not path.resolve().is_relative_to(player.parent):
            raise ValueError('Player manifest symlink leaves the Player directory.')
        if not path.is_file() or path.stat().st_size != entry['bytes']:
            raise ValueError('Player file missing/size differs: ' + str(relative))
        digest = hashlib.sha256()
        with path.open('rb') as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b''):
                digest.update(chunk)
        if digest.hexdigest() != entry['sha256']:
            raise ValueError('Player hash differs: ' + str(relative))
    return {'sourceCommit': source, 'frameworkCommit': fork, 'unityVersion': unity, 'filesVerified': len(entries)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--player', required=True)
    args = parser.parse_args()
    try:
        result = verify_player(pathlib.Path(__file__).resolve().parents[1], args.player)
    except (OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as error:
        print(str(error), file=sys.stderr)
        return 1
    print(json.dumps(result))
    return 0


if __name__ == '__main__':
    sys.exit(main())
