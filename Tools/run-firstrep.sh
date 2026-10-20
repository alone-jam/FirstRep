#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
player="${FIRSTREP_PLAYER:-$project_dir/Builds/Linux/FirstRep.x86_64}"
if [[ ! -x "$player" ]]; then
  echo "Linux player is not built yet: $player" >&2
  exit 1
fi
python3 "$project_dir/Tools/check_player_provenance.py" --player "$player"
if [[ "${FIRSTREP_SOFTWARE_RENDERING:-0}" == 1 ]]; then
  render_root="${FIRSTREP_RENDER_SUPPORT:-/workspace/tools/render-support/root}"
  if [[ ! -f "$render_root/usr/share/vulkan/icd.d/lvp_icd.json" || -z "${DISPLAY:-}" ]]; then
    echo "Software Vulkan ICD or DISPLAY is unavailable. Configure and verify graphics support before launching." >&2
    exit 1
  fi
  export VK_ICD_FILENAMES="$render_root/usr/share/vulkan/icd.d/lvp_icd.json"
  export LD_LIBRARY_PATH="$render_root/usr/lib/x86_64-linux-gnu${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
  export XDG_CONFIG_HOME="${XDG_CONFIG_HOME:-$project_dir/.runtime/config}"
  export XDG_CACHE_HOME="${XDG_CACHE_HOME:-$project_dir/.runtime/cache}"
  mkdir -p "$XDG_CONFIG_HOME" "$XDG_CACHE_HOME"
  set -- -force-vulkan -force-device-index 0 "$@"
fi
exec "$player" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 "$@"
