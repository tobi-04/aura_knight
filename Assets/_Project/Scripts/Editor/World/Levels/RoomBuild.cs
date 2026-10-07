using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>The room prefab under construction: its source file and the containers each builder part fills.</summary>
    sealed class RoomBuild
    {
        public RoomFile File;
        public LevelRegion Region;
        public Transform Root, Grid, Collision, Hazards, Props, Enemies, Exits, Spawns;

        public static Vector3 Local(Vector2 position) => new Vector3(position.x, position.y, 0f);
    }
}
