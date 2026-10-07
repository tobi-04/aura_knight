---
name: unity-batch-gotchas
description: tools/unity-batch.sh quirks in the Aura Knight repo that hide failures (exit code, 120 s tool limit, shared logs)
metadata:
  type: feedback
---

`tools/unity-batch.sh exec ...` prints `exit=0` even when the batch aborted on an exception (generator threw, validator failed). After every `exec` of a generator, grep the log (`Logs/batch-exec-<Method>.log`) for `Aborting batchmode`, `Step '.*' failed` or `[RegenerateAll] Done`. Piping the script through `tail` also hides it.

**Why:** a failed `LevelGenerator.GenerateAll` (validator problem) silently left old prefabs in place and the PlayMode run that followed passed against stale assets.

**How to apply:** run generators, then check the log line, then run tests. Run full PlayMode (about 3 minutes) with `run_in_background` (tool calls time out at 120 s) and wait for the notification; do not edit `.cs` files while a batch is running. `Logs/test-results.xml` is overwritten by each run, so read it right after the run. A narrow run: `UNITY_TEST_FILTER=<namespace or full name> tools/unity-batch.sh test PlayMode`.
