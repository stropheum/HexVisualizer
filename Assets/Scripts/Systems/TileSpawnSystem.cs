using Hex.Authoring;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Hex.Systems
{
    public partial struct TileSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TileSpawnerConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((var config, var spawnFlag, Entity entity) in
                     SystemAPI.Query<RefRO<TileSpawnerConfig>, RefRW<TileSpawnFlag>>().WithEntityAccess())
            {
                if (spawnFlag.ValueRO.Spawned) { continue; }

                Entity prefab = config.ValueRO.Prefab;
                var tilePositions = SystemAPI.GetBuffer<TilePosition>(entity);

                foreach (TilePosition tilePosition in tilePositions)
                {
                    Entity tileEntity = state.EntityManager.Instantiate(prefab);
                    state.EntityManager.SetComponentData(tileEntity,
                        new LocalTransform()
                        {
                            Position = tilePosition.Position,
                            Rotation = Quaternion.identity,
                            Scale = 1.0f
                        });
                }

                spawnFlag.ValueRW.Spawned = true;
            }
        }
    }
}