using Unity.Entities;

namespace sandbox
{
    [GenerateAuthoringComponent]
    public struct SpeedData : IComponentData
    {
        public float speed;
    }
}