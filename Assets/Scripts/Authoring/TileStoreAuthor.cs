using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Authoring
{
    [InternalBufferCapacity(128)]
    public struct TilePosition : IBufferElementData
    {
        public float3 Position;
    }

    public class TileStoreAuthor : MonoBehaviour
    {
        [Serializable]
        public struct TileSetupInfo
        {
            public TileAuthoring TilePrefab;
            public Vector2Int HexTileSize;
        }

        private const float PointToPointLength = 1.5f;
        private const float HalfP2PLength = 0.75f;
        private const float TileOffsetY = 0.435f;

        [field: SerializeField] public TileSetupInfo SetupInfo { get; private set; }

        public class Baker : Baker<TileStoreAuthor>
        {
            public override void Bake(TileStoreAuthor authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                var tilePositions = AddBuffer<TilePosition>(entity);

                Vector2Int hexTileSize = authoring.SetupInfo.HexTileSize;
                Vector2 origin = new Vector2(hexTileSize.x * PointToPointLength, hexTileSize.y * TileOffsetY) / -2f;

                for (var i = 0; i < hexTileSize.y; i++)
                for (var j = 0; j < hexTileSize.x; j++)
                {
                    float x = j * PointToPointLength + HalfP2PLength * (i % 2);
                    float y = i * TileOffsetY;
                    tilePositions.Add(new TilePosition
                    {
                        Position = new Vector3(origin.x + x, 0, origin.y + y)
                    });
                }

                AddComponent(entity,
                    new TileSpawnerConfig
                    {
                        Prefab = GetEntity(authoring.SetupInfo.TilePrefab,
                            TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace)
                    });
                AddComponent(entity,
                    new TileSpawnFlag
                    {
                        Spawned = false
                    });
            }
        }
    }
}