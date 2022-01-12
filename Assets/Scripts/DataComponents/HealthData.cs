using System;
using Unity.Entities;

namespace sandbox
{
    [GenerateAuthoringComponent]
    public struct HealthData : IComponentData
    {
        public float health;
    }
}
