using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace sandbox
{
    [AlwaysSynchronizeSystem]
    public class UnitTargeting : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb1 = new EntityCommandBuffer(Allocator.TempJob);
            var ecb1Writer = ecb1.AsParallelWriter();

            var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            var ecb2Writer = ecb2.AsParallelWriter();

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
                ecb1Writer.AddComponent(entityInQueryIndex, entity, new UnitHasTarget { target = randomBUnit });
            }).ScheduleParallel(Dependency);

            var job2 = Entities
                .WithAll<TeamBTag>()
                .WithNone<UnitHasTarget>()
                .WithReadOnly(teamAArray)
                .ForEach((Entity entity, int entityInQueryIndex) =>
            {
                var rand = GenerateRandom(time, entity.Index);
                var randomAUnit = teamAArray[rand.NextInt(0, teamAArray.Length)];
                ecb2Writer.AddComponent(entityInQueryIndex, entity, new UnitHasTarget { target = randomAUnit });
            }).ScheduleParallel(Dependency);

            job1.Complete();
            job2.Complete();

            ecb1.Playback(EntityManager);
            ecb1.Dispose();
            ecb2.Playback(EntityManager);
            ecb2.Dispose();
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
