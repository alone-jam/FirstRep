#!/usr/bin/env python3
"""Read-only SHA256/size verification of checked-out Git LFS assets and optional cache.

Prints a JSON summary. No download, checkout, deletion or credential access.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
import datetime
import hashlib
import json
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]


def git(*args):
    return subprocess.check_output(["git", *args], cwd=root, text=True, stderr=subprocess.PIPE)


def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            value.update(block)
    return path.stat().st_size, value.hexdigest()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--include-local-cache", action="store_true")
    args = parser.parse_args()
    files = json.loads(git("lfs", "ls-files", "--json"))["files"]
    media = next(line.split("=", 1)[1] for line in git("lfs", "env").splitlines()
                 if line.startswith("LocalMediaDir="))

    def check(item):
        expected = item["size"], item["oid"]
        path = root / item["name"]
        if item["oid_type"] != "sha256" or not path.resolve().is_relative_to(root):
            return {"path": item["name"], "error": "Invalid LFS metadata or asset outside project"}
        try:
            if digest(path) != expected:
                return {"path": item["name"], "error": "Working payload size/hash mismatch"}
            if args.include_local_cache:
                oid = item["oid"]
                cached = Path(media) / oid[:2] / oid[2:4] / oid
                if digest(cached) != expected:
                    return {"path": item["name"], "error": "Cached object size/hash mismatch"}
        except OSError as error:
            return {"path": item["name"], "error": type(error).__name__}
        return None

    with ThreadPoolExecutor(max_workers=2) as pool:
        failures = [error for error in pool.map(check, files) if error]
    manifest = [{k: f[k] for k in ["name", "size", "oid"]} for f in sorted(files, key=lambda f: f["name"])]
    result = {"checkedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
              "sourceCommit": git("rev-parse", "HEAD").strip(),
              "files": len(files), "payloadBytes": sum(f["size"] for f in files),
              "workingPayloadsVerified": not failures,
              "localCacheVerified": not failures if args.include_local_cache else None,
              "manifestSha256": hashlib.sha256(json.dumps(manifest, sort_keys=True).encode()).hexdigest(),
              "failures": failures,
              "boundary": "Local integrity at this time; not a platform retention or independent artifact backup guarantee"}
    print(json.dumps(result, ensure_ascii=False, indent=2))
    raise SystemExit(1 if failures else 0)
