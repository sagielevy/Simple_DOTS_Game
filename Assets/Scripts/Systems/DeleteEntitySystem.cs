using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;

namespace sandbox
{
    [AlwaysSynchronizeSystem]
    [UpdateAfter(typeof(HealthSystem))]
    public class DeleteEntitySystem : JobComponentSystem
    {
        EntityQuery deletedEntitiesGroupQuery;

        protected override void OnCreate()
        {
            deletedEntitiesGroupQuery = GetEntityQuery(ComponentType.ReadOnly<DeleteTag>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBuffer1 = new EntityCommandBuffer(Allocator.TempJob);
            var commandBuffer1Writer = commandBuffer1.AsParallelWriter();
            var commandBuffer2 = new EntityCommandBuffer(Allocator.TempJob);
            var commandBuffer2Writer = commandBuffer2.AsParallelWriter();

            var deletedEntities = deletedEntitiesGroupQuery.ToEntityArray(Allocator.TempJob);

            // No one must point to destroyed entities anymore
            var job1 = Entities
                .WithAll<UnitHasTarget>()
                .ForEach((Entity other, int entityInQueryIndex, in UnitHasTarget unitHasTarget) =>
            {
                if (deletedEntities.Contains(unitHasTarget.target))
                {
                    commandBuffer1Writer.RemoveComponent<UnitHasTarget>(entityInQueryIndex, other);
                }
            }).Schedule(inputDeps);

            var job2 = Entities
                .WithAll<DeleteTag>()
                .WithoutBurst()
                .ForEach((Entity entity, int entityInQueryIndex) =>
                {
                    GameManager.instance.EntitiesCount -= 1;
                    commandBuffer2Writer.DestroyEntity(entityInQueryIndex, entity);
                }).Schedule(job1);

            job1.Complete();
            job2.Complete();

            commandBuffer1.Playback(EntityManager);
            commandBuffer1.Dispose();
            commandBuffer2.Playback(EntityManager);
            commandBuffer2.Dispose();
            deletedEntities.Dispose();

            return job2;
        }
    }
}