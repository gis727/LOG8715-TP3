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
        ComponentsManager.Instance.ForEach<ShapeComponent, UserInputComponent>((entityID, shapeComponent, userInput) => {
            if (ECSManager.Instance.NetworkManager.IsServer)
            {
                if ((ECSManager.Instance.RunningFastForward && userInput.fastForwardInputsMessages.Count > 0) || !ECSManager.Instance.RunningFastForward)
                {
                    shapeComponent.pos = GetNewPosition(shapeComponent.pos, shapeComponent.speed, deltaTime);
                    ComponentsManager.Instance.SetComponent<ShapeComponent>(entityID, shapeComponent);
                    userInput.fastForwardInputsMessages.Clear();
                }
            }
            else
            {
                shapeComponent.pos = GetNewPosition(shapeComponent.pos, shapeComponent.speed, deltaTime);
                ComponentsManager.Instance.SetComponent<ShapeComponent>(entityID, shapeComponent);
            }
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

