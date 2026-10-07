# Enemy art

Variants: ThornBug, MushroomHopper, Bat, StoneSpider, PatrolRobot, ScrapZapper, NightKnight, Ghost.
Each folder holds `<Variant>.png` (sliced sheet), `<Variant>.sheet.json`, `Clips/<Variant>_<Clip>.anim` and `<Variant>.overrideController`.

Animator contract (`_Base/EnemyBase.controller`, the override controller swaps in the variant clips):

| Parameter | Type | Effect |
|-----------|------|--------|
| `Moving` | bool | Idle <-> Move |
| `Attack` | trigger | any state -> Attack, returns to Idle when the clip ends |
| `Hurt` | trigger | any state -> Hurt, returns to Idle |
| `Dead` | bool | any state -> Death (final frame holds) |

Clips (same names in every sheet): `Idle` (loop), `Move` (loop), `Attack`, `Hurt`, `Death` (one-shot). ScrapZapper never moves, its `Move` equals `Idle`.
Put the Animator on the child that holds the SpriteRenderer (clips animate `SpriteRenderer.sprite` at the Animator's own path) and use `Mat_SpriteLit`.
Sheet cells: 48x48 px (NightKnight 64x64), centre pivot, facing right.
