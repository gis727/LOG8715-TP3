/// <summary>
/// Clear components that shouldn't exist by the end of the frame
/// </summary>
public class ClearEndOfFrameComponentsSystem : ISystem
{
    public string Name
    {
        get 
        {
            return GetType().Name;
        }
    }

    public void UpdateSystem()
    {
        ComponentsManager.Instance.ClearComponents<ReplicationMessage>();
        ComponentsManager.Instance.ClearComponents<CollisionEventComponent>();
    }
}

