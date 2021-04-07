public struct CollisionEventComponent : IComponent
{
    readonly public uint entityID;
    readonly public uint otherEntityID;

    public CollisionEventComponent(uint id1, uint id2)
    {
        entityID = id1;
        otherEntityID = id2;
    }
}