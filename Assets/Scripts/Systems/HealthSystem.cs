using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace sandbox
{
    [AlwaysSynchronizeSystem]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public class HealthSystem : SystemBase
    {
        private BeginFixedStepSimulationEntityCommandBufferSystem bufferSystem;
        private BuildPhysicsWorld buildPhysicsWorld;
        private StepPhysicsWorld stepPhysicsWorld;

        protected override void OnCreate()
        {
            bufferSystem = World.GetOrCreateSystem<BeginFixedStepSimulationEntityCommandBufferSystem>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        }

        protected override void OnUpdate()
        {
            var triggerJob = new TriggerJob
            {
                entitiesWithHealth = GetComponentDataFromEntity<HealthData>(true),
                teamAEntities = GetComponentDataFromEntity<TeamATag>(true),
                teamBEntities = GetComponentDataFromEntity<TeamBTag>(true),
                deletedEntities = GetComponentDataFromEntity<DeleteTag>(true),
                colliders = GetComponentDataFromEntity<PhysicsCollider>(true),
                commandBuffer = bufferSystem.CreateCommandBuffer()
            };

            var triggerJobHandle = triggerJob.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, Dependency);
            triggerJobHandle.Complete();
        }

        private struct TriggerJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentDataFromEntity<HealthData> entitiesWithHealth;
            [ReadOnly] public ComponentDataFromEntity<TeamATag> teamAEntities;
            [ReadOnly] public ComponentDataFromEntity<TeamBTag> teamBEntities;
            [ReadOnly] public ComponentDataFromEntity<DeleteTag> deletedEntities;
            [ReadOnly] public ComponentDataFromEntity<PhysicsCollider> colliders;

            public EntityCommandBuffer commandBuffer;

            public void Execute(CollisionEvent collisionEvent)
            {
                // Only a single direction of the collision will pass (A, B) OR (B, A)
                if (TestEntityTrigger(collisionEvent.EntityA, collisionEvent.EntityB))
                {
                    var orgHealthA = entitiesWithHealth[collisionEvent.EntityA].health;
                    var orgHealthB = entitiesWithHealth[collisionEvent.EntityB].health;
                    var damage = math.min(orgHealthA, orgHealthB);

                    var newHealthA = orgHealthA - damage;
                    var newHealthB = orgHealthB - damage;

                    if (newHealthA > 0)
                    {
                        var newScaleA = UnitSpawner.healthToSizeRatio * newHealthA;
                        commandBuffer.SetComponent(collisionEvent.EntityA, new HealthData { health = newHealthA });
                        commandBuffer.SetComponent(collisionEvent.EntityA, new Scale { Value = newScaleA });
                        commandBuffer.SetComponent(collisionEvent.EntityA, MakeCollider(colliders[collisionEvent.EntityA], newScaleA));
                    } else
                    {
                        commandBuffer.AddComponent(collisionEvent.EntityA, new DeleteTag());
                    }

                    if (newHealthB > 0)
                    {
                        var newScaleB = UnitSpawner.healthToSizeRatio * newHealthB;
                        commandBuffer.SetComponent(collisionEvent.EntityB, new HealthData { health = newHealthB });
                        commandBuffer.SetComponent(collisionEvent.EntityB, new Scale { Value = newScaleB });
                        commandBuffer.SetComponent(collisionEvent.EntityB, MakeCollider(colliders[collisionEvent.EntityB], newScaleB));
                    }
                    else
                    {
                        commandBuffer.AddComponent(collisionEvent.EntityB, new DeleteTag());
                    }
                }
            }

            private bool TestEntityTrigger(Entity entity1, Entity entity2)
            {
                return teamAEntities.HasComponent(entity1) &&
                    teamBEntities.HasComponent(entity2) &&
                    !deletedEntities.HasComponent(entity1) &&
                    !deletedEntities.HasComponent(entity2);
            }

            private PhysicsCollider MakeCollider(PhysicsCollider collider, float scale)
            {
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
                    //collider.Value.Value.Filter);

                        //return new PhysicsCollider { Value = sphereCollider };
                    }
                }
                else
                {
                    unsafe {
                        BoxCollider* colliderPtr = (BoxCollider*)collider.ColliderPtr;
                        var boxCollider = BoxCollider.Create(new BoxGeometry
                        {
                            Center = float3.zero,
                            BevelRadius = 0.05f * scale,
                            Orientation = quaternion.identity,
                            Size = new float3(1) * scale
                        }, colliderPtr->Filter, colliderPtr->Material);
                        collider.Value = boxCollider;
                    //collider.Value.Value.Filter);

                        //return new PhysicsCollider { Value = boxCollider };
                    }
                }

                return collider;
            }
        }
    }
}