# Boss art

Bosses: RootTree (96x96), GiantStoneSpider (128x96), RogueMachine (128x128), Malakor (96x128). Layout as in `Enemies/README.md`.
Animator contract (`_Base/BossBase.controller`, per boss `<Boss>.overrideController`):

| Parameter | Type | Effect |
|-----------|------|--------|
| `Moving` | bool | Idle <-> Move |
| `Attack1`, `Attack2`, `Attack3` | trigger | any state -> that attack, returns to Idle |
| `Hurt` | trigger | any state -> Hurt |
| `Dead` | bool | any state -> Death |
| `Exposed` | bool | Idle <-> Exposed (weak-point pose; real clip only on RootTree, other bosses map it to Idle) |

Attack mapping (GDD 7.4): RootTree 1 roots, 2 seeds, 3 branch sweep. GiantStoneSpider 1 ceiling drop, 2 web spit, 3 summon.
RogueMachine 1 pistons, 2 laser (beam drawn in the clip), 3 steam. Malakor 1 slash, 2 teleport thrust (dither fade), 3 darkness aura.
Phase 2 / 3: speed up `Animator.speed` and tint through `_Color`; no extra clips.
