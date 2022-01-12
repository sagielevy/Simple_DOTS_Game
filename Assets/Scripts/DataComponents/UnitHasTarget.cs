using Unity.Entities;

namespace sandbox
{
    // Added to Entity if has Target
    [GenerateAuthoringComponent]
    public struct UnitHasTarget : IComponentData
    {
        public Entity target;
    }
}