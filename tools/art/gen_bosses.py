"""Builds the four boss sheets under Art/Bosses/<Boss>/ (GDD 7.4). Clips: Idle, Move, Attack1-3, Hurt, Death (+Exposed for RootTree)."""
import sys
import pixel_kit as pk
import gen_boss_tree_spider as a
import gen_boss_machine_lord as b


def build():
    return {"RootTree": a.build_tree(), "GiantStoneSpider": a.build_spider(),
            "RogueMachine": b.build_machine(), "Malakor": b.build_malakor()}


if __name__ == "__main__":
    rows = build()
    if len(sys.argv) > 1:
        for name, anims in rows.items():
            pk.preview([[f for row in anims for f in row]], f"{sys.argv[1]}_{name}.png", zoom=2)
