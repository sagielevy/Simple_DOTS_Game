using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using SphereCollider = Unity.Physics.SphereCollider;
using BoxCollider = Unity.Physics.BoxCollider;

namespace sandbox
{
    public class UnitSpawner : MonoBehaviour
    {
        public const float hitDamage = 10;
        public const float healthToSizeRatio = 0.05f;

        public float maxHealth = 200;
        public float minHealth = 5;
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
            var health = minHealth + Mathf.Pow(UnityEngine.Random.value, 6) * (maxHealth - minHealth);
            var scale = health * healthToSizeRatio;

            var randomOffset = UnityEngine.Random.insideUnitCircle * spawningRadius;
            var initialPosition = transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);

            var collider = manager.GetComponentData<PhysicsCollider>(entity);

            if (collider.Value.Value.Type == ColliderType.Sphere)
            {
                unsafe
                {
                    SphereCollider* colliderPtr = (SphereCollider*)collider.ColliderPtr;

                    var sphereCollider = SphereCollider.Create(new SphereGeometry
                    {
                        Center = float3.zero,
                        Radius = scale
                    }, colliderPtr->Filter, colliderPtr->Material);
                    collider.Value = sphereCollider;

                    manager.SetComponentData(entity, collider);
                }
            }
            else
            {
                unsafe
                {
                    BoxCollider* colliderPtr = (BoxCollider*)collider.ColliderPtr;

                    var boxCollider = BoxCollider.Create(new BoxGeometry
                    {
                        Center = float3.zero,
                        BevelRadius = 0.05f * scale,
                        Orientation = quaternion.identity,
                        Size = new float3(1) * scale
                    }, colliderPtr->Filter, colliderPtr->Material);
                    collider.Value = boxCollider;
                    manager.SetComponentData(entity, collider);
                }
            }

            manager.AddComponentData(entity, velocity);
            manager.AddComponentData(entity, new Scale { Value = scale });
            manager.SetComponentData(entity, new HealthData { health = health });
            manager.SetComponentData(entity, new Translation { Value = new float3(initialPosition) });
        }

        private void OnDestroy()
        {
            blobAssetStore.Dispose();
        }
    }
}