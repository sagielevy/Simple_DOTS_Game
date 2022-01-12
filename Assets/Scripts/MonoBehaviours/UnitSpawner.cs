using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace sandbox
{
    public class UnitSpawner : MonoBehaviour
    {
        public const float hitDamage = 10;
        public const float healthToSizeRatio = 0.05f;

        public float maxHealth = 200;
        public float spawningRadius = 5;
        public int spawnCount;

        public GameObject unitPrefab;

        private Entity unitEntityPrefab;
        private EntityManager manager;
        private BlobAssetStore blobAssetStore;

        private void Start()
        {
            manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            blobAssetStore = new BlobAssetStore();

            var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
            unitEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(unitPrefab, settings);
        }

        private void Update()
        {
            for (int i = 0; i < spawnCount; i++)
            {
                if (GameManager.instance.EntitiesCount < GameManager.instance.maxExistingEntitiesCount)
                {
                    GameManager.instance.EntitiesCount += 1;
                    SpawnNewUnit();
                }
            }
        }

        private void SpawnNewUnit()
        {
            var entity = manager.Instantiate(unitEntityPrefab);

            var velocity = new PhysicsVelocity()
            {
                Linear = float3.zero,
                Angular = float3.zero
            };

            // Non-uniform distribtution of health per unit.
            var health = Mathf.Pow(UnityEngine.Random.value, 6) * maxHealth;

            var randomOffset = UnityEngine.Random.insideUnitCircle * spawningRadius;
            var initialPosition = transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);

            manager.AddComponentData(entity, velocity);
            manager.AddComponentData(entity, new Scale { Value = health * healthToSizeRatio });
            manager.SetComponentData(entity, new HealthData { health = health });
            manager.SetComponentData(entity, new Translation { Value = new float3(initialPosition) });
        }

        private void OnDestroy()
        {
            blobAssetStore.Dispose();
        }
    }
}