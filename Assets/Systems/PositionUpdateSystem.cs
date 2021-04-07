using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionUpdateSystem : ISystem {
    public string Name
    {
        get
        {
            return this.GetType().Name;
        }
    }

    public void UpdateSystem()
    {
        UpdateSystem(Time.deltaTime);
    }

    public void UpdateSystem(float deltaTime)
    {
        ComponentsManager.Instance.ForEach<ShapeComponent>((entityID, shapeComponent) => {
            shapeComponent.pos = GetNewPosition(shapeComponent.pos, shapeComponent.speed, deltaTime);
            ComponentsManager.Instance.SetComponent<ShapeComponent>(entityID, shapeComponent);
        });
}

    public static Vector2 GetNewPosition(Vector2 position, Vector2 speed)
    {
        return GetNewPosition(position, speed, Time.deltaTime);
    }

    public static Vector2 GetNewPosition(Vector2 position, Vector2 speed, float deltaTime)
    {
        var newPosition = position + speed * deltaTime;
        return newPosition;
    }
}

