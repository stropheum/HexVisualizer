using System;
using Unity.Entities;

namespace Hex
{
    [Serializable]
    public struct PerlinConfig : IComponentData
    {
        public float Zoom;
        public float TimeScale;
    }

    public struct TileSpawnerConfig : IComponentData
    {
        public Entity Prefab;
    }

    public struct TileSpawnFlag : IComponentData
    {
        public bool Spawned;
    }
}