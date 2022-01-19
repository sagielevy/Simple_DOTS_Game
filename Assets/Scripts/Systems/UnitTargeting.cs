using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace sandbox
{
    [AlwaysSynchronizeSystem]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class UnitTargeting : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem commandBufferSystem;

        private EntityQuery unitsWithNoTargetsQuery;

        protected override void OnCreate()
        {
            commandBufferSystem =
                World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

            unitsWithNoTargetsQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any = new ComponentType[] { ComponentType.ReadOnly<TeamATag>(), ComponentType.ReadOnly<TeamBTag>() },
                None = new ComponentType[] { ComponentType.ReadOnly<UnitHasTarget>() }
            });
        }

        protected override void OnStartRunning()
        {
            RequireForUpdate(unitsWithNoTargetsQuery);
        }

        protected override void OnUpdate()
        {
            var ecb1 = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var ecb2 = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            var teamAArray = GetEntityQuery(ComponentType.ReadOnly<TeamATag>()).ToEntityArray(Allocator.TempJob);
            var teamBArray = GetEntityQuery(ComponentType.ReadOnly<TeamBTag>()).ToEntityArray(Allocator.TempJob);
            var time = Time.ElapsedTime;
            
            var job1 = Entities
                .WithAll<TeamATag>()
                .WithNone<UnitHasTarget>()
                .WithReadOnly(teamBArray)
                .ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
            {
                var rand = GenerateRandom(time, entity.Index);
                var randomBUnit = teamBArray[rand.NextInt(0, teamBArray.Length)];
                ecb1.AddComponent(entityInQueryIndex, entity, new UnitHasTarget { target = randomBUnit });
            }).ScheduleParallel(Dependency);

            var job2 = Entities
                .WithAll<TeamBTag>()
                .WithNone<UnitHasTarget>()
                .WithReadOnly(teamAArray)
                .ForEach((Entity entity, int entityInQueryIndex) =>
            {
                var rand = GenerateRandom(time, entity.Index);
                var randomAUnit = teamAArray[rand.NextInt(0, teamAArray.Length)];
                ecb2.AddComponent(entityInQueryIndex, entity, new UnitHasTarget { target = randomAUnit });
            }).ScheduleParallel(Dependency);

            job1.Complete();
            job2.Complete();

            teamAArray.Dispose();
            teamBArray.Dispose();
        }

        private static Random GenerateRandom(double time, int index)
        {
            var rand = new Random();
            rand.InitState((uint)((math.sin(time * 353) * 0.5 + 1) * 100 * (index + 1)));
            return rand;
        }
    }
}
