using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;

namespace sandbox
{
    // Should always run last as it removes entities.
    [AlwaysSynchronizeSystem]
    [UpdateAfter(typeof(MovementSystem))]
    [UpdateAfter(typeof(HealthSystem))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public class DeleteEntitySystem : SystemBase
    {
        EntityQuery deletedEntitiesQuery;
        EndFixedStepSimulationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            deletedEntitiesQuery = GetEntityQuery(ComponentType.ReadOnly<DeleteTag>());
            commandBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        }

        protected override void OnStartRunning()
        {
            RequireForUpdate(deletedEntitiesQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer1 = commandBufferSystem.CreateCommandBuffer();
            var commandBuffer1Writer = commandBuffer1.AsParallelWriter();

            commandBuffer1.DestroyEntitiesForEntityQuery(deletedEntitiesQuery);

            var deletedEntities = deletedEntitiesQuery.ToEntityArray(Allocator.TempJob);
            GameManager.instance.EntitiesCount -= deletedEntities.Length;

            // No one must point to destroyed entities anymore
            var job1 = Entities
                .WithReadOnly(deletedEntities)
                .ForEach((Entity other, int entityInQueryIndex, in UnitHasTarget unitHasTarget) =>
            {
                if (deletedEntities.Contains(unitHasTarget.target))
                {
                    commandBuffer1Writer.RemoveComponent<UnitHasTarget>(entityInQueryIndex, other);
                }
            }).ScheduleParallel(Dependency);

            job1.Complete();
            deletedEntities.Dispose();
        }
    }
}