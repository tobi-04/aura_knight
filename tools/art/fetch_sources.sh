#!/usr/bin/env bash
# Downloads the third-party CC0 packs the generators build on into tools/art/.cache (git-ignored, not shipped).
# Each pack's License.txt is checked for "CC0" before it is accepted. Usage: tools/art/fetch_sources.sh
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
CACHE="${AURA_ART_CACHE:-$ROOT/tools/art/.cache}"
mkdir -p "$CACHE"

fetch_kenney() { # slug
  local slug="$1" dest="$CACHE/$1"
  if [ -f "$dest/License.txt" ]; then echo "$slug: cached"; return; fi
  local url
  url="$(curl -fsSL "https://kenney.nl/assets/$slug" | grep -o -E "https://kenney.nl/media/pages/assets/$slug/[^\"]+\.zip" | sort -u | head -1)"
  [ -n "$url" ] || { echo "$slug: download link not found" >&2; exit 1; }
  curl -fsSL "$url" -o "$CACHE/$slug.zip"
  mkdir -p "$dest" && unzip -q -o "$CACHE/$slug.zip" -d "$dest"
  grep -q "CC0" "$dest/License.txt" || { echo "$slug: License.txt is not CC0, refusing to use it" >&2; rm -rf "$dest"; exit 1; }
  echo "$slug: ok ($url)"
}

fetch_kenney tiny-dungeon
