#!/usr/bin/env python3
"""Run the recorded local CLI without changing shell profiles or logging proxy traffic.

Authentication stays in Unity's dedicated state; do not redirect login/status
output into Git or artifact backups. This wrapper never performs login itself.
"""
import json
import os
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
config = json.loads((root / "Tools/unity-environment.json").read_text())
binary = Path(config["cliExecutable"])
if not binary.is_file():
    sys.exit("Recorded Unity CLI is unavailable; follow Docs/STORAGE-AND-RECOVERY.md")
env = os.environ.copy()
env.update(config["cliStateEnvironment"])
os.execve(str(binary), [str(binary), "--no-log-proxy", *sys.argv[1:]], env)
