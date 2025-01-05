using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Hex.Authoring
{
    public class TileAuthoring : MonoBehaviour
    {
        [SerializeField] private PerlinConfig _perlinConfig;

        private class Baker : Baker<TileAuthoring>
        {
            public override void Bake(TileAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);
                AddComponent(entity, new LocalTransform { Position = authoring.transform.position });
                AddComponent(entity,
                    new PerlinConfig
                    {
                        TimeScale = authoring._perlinConfig.TimeScale,
                        Zoom = authoring._perlinConfig.Zoom
                    });
            }
        }
    }
}