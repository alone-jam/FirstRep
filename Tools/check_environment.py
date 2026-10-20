#!/usr/bin/env python3
"""Read-only FirstRep preflight. Does not launch Unity, hydrate LFS or inspect secrets.

Exit 0: requested checks completed (read readiness in JSON).
Exit 2: --require-ready and prerequisites missing. Exit 3: consistency/read failure.
No directory is classified as persistent from its name or mount type.
"""
import argparse
import datetime
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess


def command(args, cwd):
    return subprocess.check_output(args, cwd=cwd, text=True, stderr=subprocess.PIPE,
                                   timeout=30).strip()


def inspect(verify_remote=False):
    root = Path(__file__).resolve().parents[1]
    config = json.loads((root / "Tools/unity-environment.json").read_text())
    version = (root / "ProjectSettings/ProjectVersion.txt").read_text()
    manifest = json.loads((root / "Packages/manifest.json").read_text())
    lock = json.loads((root / "Packages/packages-lock.json").read_text())["dependencies"]["com.ejoy.framework"]
    provenance = json.loads((root / "Docs/UpstreamVersions.json").read_text())["EjoyFramework"]
    guard = (root / "Assets/FirstRep/Editor/FrameworkDependencyCheck.cs").read_text()
    consistent = (manifest["dependencies"]["com.ejoy.framework"] == lock["version"] == provenance["url"]
                  and lock["hash"] == provenance["commit"] and lock["source"] == "git"
                  and provenance["commit"] in guard
                  and provenance["url"].split("#")[0] in guard
                  and f'm_EditorVersion: {config["editorVersion"]}\n' in version
                  and f'({config["editorRevision"]})' in version)
    editor = Path(os.environ.get(config["editorOverrideVariable"], config["editorExecutable"]))
    available = editor.is_file() and os.access(editor, os.X_OK)
    blockers = []
    if not consistent:
        blockers.append("Manifest, lock, guard, provenance or Editor version is inconsistent")
    if not available:
        blockers.append("Configured Editor executable is missing or not executable")
    # git-lfs reports pointers and local object presence without fetching or checking out.
    lfs_data = json.loads(command(["git", "lfs", "ls-files", "--json"], root))
    files = lfs_data["files"] if isinstance(lfs_data, dict) else lfs_data
    missing = [f for f in files if not f["checkout"] or not f["downloaded"]]
    if missing:
        blockers.append(f'{len(missing)} LFS files lack checked-out payload or a local LFS object')
    head = command(["git", "rev-parse", "HEAD"], root)
    remote = None
    if verify_remote:
        line = command(["git", "ls-remote", "origin", "refs/heads/work"], root)
        remote = line.split()[0] if line else None
        if remote != head:
            blockers.append("Local HEAD differs from remote work; unpublished work needs review")
    observations = {}
    for path in [root, Path("/tmp")]:
        observations[str(path)] = json.loads(command(
            ["findmnt", "--json", "-T", str(path), "-o", "TARGET,SOURCE,FSTYPE,OPTIONS"], root))
    return {
        "checkedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "host": socket.gethostname(), "project": str(root), "head": head,
        "remoteWorkHead": remote, "remoteChecked": verify_remote,
        "configurationConsistent": consistent,
        "editor": {"path": str(editor), "expectedVersion": config["editorVersion"],
                   "expectedRevision": config["editorRevision"], "executablePresent": available,
                   "binaryVersionExecuted": False, "licenseChecked": False},
        "unityCliOnPath": shutil.which("unity"),
        "lfs": {"files": len(files), "checkedOut": sum(f["checkout"] for f in files),
                "localObjectsPresent": sum(f["downloaded"] for f in files),
                "expectedPayloadBytes": sum(f["size"] for f in files),
                "payloadHashesVerified": False, "remotePayloadsVerified": False},
        "mountObservationsOnly": observations,
        "verifiedPersistentDirectory": config["verifiedPersistentDirectory"],
        "configuredExternalArtifactBackup": config["configuredExternalArtifactBackup"],
        "prerequisitesPresent": not blockers, "blockers": blockers,
        "boundary": "Presence is not licensing, Unity test success, payload integrity or retention proof. No file was changed."
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify-remote", action="store_true", help="Read remote work SHA; does not fetch")
    parser.add_argument("--require-ready", action="store_true", help="Exit 2 when Editor/LFS/config prerequisites are absent")
    args = parser.parse_args()
    try:
        result = inspect(args.verify_remote)
    except (OSError, ValueError, KeyError, subprocess.SubprocessError) as error:
        # Do not print captured stderr, arbitrary subprocess output or environment variables.
        print(json.dumps({"checkCompleted": False, "errorType": type(error).__name__,
                          "boundary": "No installation, login, download or cleanup attempted"}))
        raise SystemExit(3)
    print(json.dumps(result, ensure_ascii=False, indent=2))
    raise SystemExit(3 if not result["configurationConsistent"] else
                     2 if args.require_ready and not result["prerequisitesPresent"] else 0)
