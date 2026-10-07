"""Rebuilds every generated art asset. Run tools/art/fetch_sources.sh first (needs Pillow: pip install -r requirements.txt).
Then in Unity: tools/unity-batch.sh exec AuraKnight.Editor.ArtPipeline.GenerateAll"""
import gen_backgrounds
import gen_bosses
import gen_enemies
import gen_leo
import gen_tilesets

if __name__ == "__main__":
    for module in (gen_leo, gen_enemies, gen_bosses, gen_tilesets, gen_backgrounds):
        module.build()
        print("built", module.__name__)
