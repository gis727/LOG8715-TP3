using System.Collections.Generic;
using UnityEngine;

public class BounceBackSystem : ISystem
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
        ComponentsManager.Instance.ForEach<CollisionEventComponent, ShapeComponent>((entity, collisionEventComponent, shapeComponent) =>
        {
            Vector2 speed = shapeComponent.speed;

            shapeComponent.speed = -shapeComponent.speed;
            ComponentsManager.Instance.SetComponent<ShapeComponent>(entity, shapeComponent);
        });
    }
}