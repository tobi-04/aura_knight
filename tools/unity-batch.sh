#!/usr/bin/env bash
# Run Unity in batch mode against this project, serialized by a lock so
# parallel callers (agents, CI, teammates' scripts) never open the project twice.
#   tools/unity-batch.sh compile                 -> import + compile, report C# errors
#   tools/unity-batch.sh setup                   -> run ProjectSetup.Run
#   tools/unity-batch.sh test [EditMode|PlayMode] -> run tests, write Logs/test-results.xml (UNITY_TEST_FILTER=<name> narrows it)
#   tools/unity-batch.sh exec Namespace.Class.Method -> run a static editor method (asset/scene generators)
# The lock is a directory holding the owner's PID: a lock whose owner died is reclaimed automatically.
# Refuses to run while a Unity Editor has the project open (Temp/UnityLockfile held by another process).
set -uo pipefail

USAGE="usage: $0 compile | setup | test [EditMode|PlayMode] | exec <Namespace.Class.Method>"
if [ $# -lt 1 ]; then echo "$USAGE" >&2; exit 64; fi
case "$1" in
  compile|setup) ;;
  test) ;;
  exec) if [ $# -lt 2 ] || [ -z "$2" ]; then echo "exec needs a method name" >&2; echo "$USAGE" >&2; exit 64; fi ;;
  *) echo "unknown command '$1'" >&2; echo "$USAGE" >&2; exit 64 ;;
esac

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity}"
LOCK="$ROOT/.unity-batch.lock"
LOG="$ROOT/Logs/batch-$1${2:+-${2##*.}}.log"
mkdir -p "$ROOT/Logs"

pid_alive() { [ -n "${1:-}" ] && kill -0 "$1" 2>/dev/null; }

acquire_lock() {
  local waited=0 owner
  until mkdir "$LOCK" 2>/dev/null; do
    owner="$(cat "$LOCK/pid" 2>/dev/null || true)"
    if [ -n "$owner" ] && ! pid_alive "$owner"; then
      echo "reclaiming stale Unity lock (owner pid $owner is gone)"
      rm -rf "$LOCK"; continue
    fi
    # Owner not recorded yet: give it a moment, then treat a pid-less lock as stale (crash between mkdir and pid write).
    if [ -z "$owner" ] && [ "$waited" -ge 10 ]; then
      echo "reclaiming stale Unity lock (no owner recorded)"
      rm -rf "$LOCK"; waited=0; continue
    fi
    echo "waiting for Unity lock${owner:+ held by pid $owner}..."; sleep 5; waited=$((waited + 5))
  done
  echo $$ > "$LOCK/pid"
}

editor_has_project_open() {
  local lockfile="$ROOT/Temp/UnityLockfile"
  [ -e "$lockfile" ] || return 1
  if command -v lsof >/dev/null 2>&1; then
    [ -n "$(lsof -t "$lockfile" 2>/dev/null)" ]
  else
    pgrep -f "Unity.*-projectPath $ROOT" >/dev/null 2>&1 || pgrep -x Unity >/dev/null 2>&1
  fi
}

acquire_lock
trap 'rm -rf "$LOCK"' EXIT

if editor_has_project_open; then
  echo "A Unity Editor has this project open (Temp/UnityLockfile is held). Close it, then run again." >&2
  exit 75
fi

case "$1" in
  compile) "$UNITY" -batchmode -nographics -quit -projectPath "$ROOT" -logFile "$LOG" ;;
  setup)   "$UNITY" -batchmode -nographics -quit -projectPath "$ROOT" -executeMethod AuraKnight.Editor.ProjectSetup.Run -logFile "$LOG" ;;
  test)    "$UNITY" -batchmode -nographics -projectPath "$ROOT" -runTests -testPlatform "${2:-EditMode}" ${UNITY_TEST_FILTER:+-testFilter "$UNITY_TEST_FILTER"} -testResults "$ROOT/Logs/test-results.xml" -logFile "$LOG" ;;
  exec)    "$UNITY" -batchmode -nographics -quit -projectPath "$ROOT" -executeMethod "$2" -logFile "$LOG" ;;
esac
code=$?
errors=$(grep -E "error CS[0-9]+" "$LOG" | sort -u)
grep -E "^(Exception|.*Exception:)" "$LOG" | grep -v "Licensing" | head -5
if [ -n "$errors" ]; then echo "$errors"; echo "COMPILE ERRORS (see $LOG)"; exit 1; fi
if [ "$1" = test ] && [ -f "$ROOT/Logs/test-results.xml" ]; then
  grep -o '<test-run [^>]*' "$ROOT/Logs/test-results.xml" | grep -oE '(total|passed|failed|skipped)="[0-9]+"'
fi
echo "exit=$code (log: $LOG)"; exit $code
