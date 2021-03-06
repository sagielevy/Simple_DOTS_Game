using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace sandbox
{
    [AlwaysSynchronizeSystem]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public class MovementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            var translations = GetComponentDataFromEntity<Translation>(true);

            Entities
                .WithReadOnly(translations)
                .ForEach((ref PhysicsVelocity vel, in UnitHasTarget unitHasTarget, in SpeedData speedData, in Translation translation) =>
            {
                var targetPos = translations[unitHasTarget.target];
                var direction = math.normalize(targetPos.Value - translation.Value);

                var newVel = direction * speedData.speed * deltaTime;

                vel.Linear.xz += newVel.xz;
            }).ScheduleParallel();
        }
    }
}
