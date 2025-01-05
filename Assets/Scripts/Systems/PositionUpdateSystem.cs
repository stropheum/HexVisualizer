using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Hex.Systems
{
    public partial struct PositionUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((var transform, var perlinConfig, Entity entity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<PerlinConfig>>().WithEntityAccess())
            {
                float3 position = transform.ValueRO.Position;
                PerlinConfig configRO = perlinConfig.ValueRO;
                float delta = Mathf.PerlinNoise(
                    position.x * configRO.Zoom + Time.time * configRO.TimeScale,
                    position.z * configRO.Zoom + Time.time * configRO.TimeScale);
                transform.ValueRW.Position.y = delta;
            }
        }
    }
}