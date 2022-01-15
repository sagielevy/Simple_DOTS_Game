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
    public class HealthSystem : SystemBase
    {
        private BeginInitializationEntityCommandBufferSystem bufferSystem;
        private BuildPhysicsWorld buildPhysicsWorld;
        private StepPhysicsWorld stepPhysicsWorld;

        protected override void OnCreate()
        {
            bufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        }

        protected override void OnUpdate()
        {
            var triggerJob = new TriggerJob
            {
                entitiesWithHealth = GetComponentDataFromEntity<HealthData>(),
                teamAEntities = GetComponentDataFromEntity<TeamATag>(),
                teamBEntities = GetComponentDataFromEntity<TeamBTag>(),
                deletedEntities = GetComponentDataFromEntity<DeleteTag>(),
                commandBuffer = bufferSystem.CreateCommandBuffer()
            };

            var triggerJobHandle = triggerJob.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, Dependency);
            bufferSystem.AddJobHandleForProducer(triggerJobHandle);
            triggerJobHandle.Complete();
        }

        private struct TriggerJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentDataFromEntity<HealthData> entitiesWithHealth;
            [ReadOnly] public ComponentDataFromEntity<TeamATag> teamAEntities;
            [ReadOnly] public ComponentDataFromEntity<TeamBTag> teamBEntities;
            [ReadOnly] public ComponentDataFromEntity<DeleteTag> deletedEntities;

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
                        commandBuffer.SetComponent(collisionEvent.EntityA, new HealthData { health = newHealthA });
                        commandBuffer.SetComponent(collisionEvent.EntityA, new Scale { Value = UnitSpawner.healthToSizeRatio * newHealthA });
                    } else
                    {
                        commandBuffer.AddComponent(collisionEvent.EntityA, new DeleteTag());
                    }

                    if (newHealthB > 0)
                    {
                        commandBuffer.SetComponent(collisionEvent.EntityB, new HealthData { health = newHealthB });
                        commandBuffer.SetComponent(collisionEvent.EntityB, new Scale { Value = UnitSpawner.healthToSizeRatio * newHealthB });
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
        }
    }
}