public class DisplayShapePositionSystem : ISystem
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
        if (ECSManager.Instance.RunningFastForward) return;
        
        ComponentsManager.Instance.ForEach<ShapeComponent>((entityID, shapeComponent) => {
            ECSManager.Instance.UpdateShapePosition(entityID, shapeComponent.pos);
        });
    }
}