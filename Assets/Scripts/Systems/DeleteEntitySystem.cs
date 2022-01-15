using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;

namespace sandbox
{
    // Should always run last as it removes entities.
    [AlwaysSynchronizeSystem]
    [UpdateAfter(typeof(HealthSystem))]
    public class DeleteEntitySystem : SystemBase
    {
        EntityQuery deletedEntitiesGroupQuery;
        EndSimulationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            deletedEntitiesGroupQuery = GetEntityQuery(ComponentType.ReadOnly<DeleteTag>());
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer1 = commandBufferSystem.CreateCommandBuffer();
            var commandBuffer1Writer = commandBuffer1.AsParallelWriter();

            commandBuffer1.DestroyEntitiesForEntityQuery(deletedEntitiesGroupQuery);

            var deletedEntities = deletedEntitiesGroupQuery.ToEntityArray(Allocator.TempJob);
            GameManager.instance.EntitiesCount -= deletedEntities.Length;

            // No one must point to destroyed entities anymore
            var job1 = Entities
                .WithAll<UnitHasTarget>()
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