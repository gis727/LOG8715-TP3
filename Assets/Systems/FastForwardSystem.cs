using UnityEngine;

public class FastForwardSystem : ISystem
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
        if (ECSManager.Instance.RunningFastForward && ECSManager.Instance.NetworkManager.isClient)
        {
            UpdateClientInFastForward();
        }
    }
    
    private void UpdateClientInFastForward()
    {
        uint clientId = (uint)ECSManager.Instance.NetworkManager.LocalClientId;
        var inputs = ComponentsManager.Instance.GetComponent<UserInputComponent>(clientId);

        if (inputs.fastForwardInputsMessages.Count > 0)
        {
            var shape = ComponentsManager.Instance.GetComponent<ShapeComponent>(clientId);
            shape.speed = Vector2.zero;

            // Pop input
            ReplicationMessage currentInput = inputs.fastForwardInputsMessages[0];
            inputs.fastForwardInputsMessages.RemoveAt(0);

            // Apply input
            Utils.GetUserInput(ref currentInput, ref shape, false);
            ComponentsManager.Instance.SetComponent<ShapeComponent>(clientId, shape);
            ComponentsManager.Instance.SetComponent<UserInputComponent>(clientId, inputs);
            
            // Update history
            ComponentsManager.Instance.ForEach<ShapeComponent, UserInputComponent>((entityID, entityShape, entityUserInput) =>
            {
                int historicalInputIndex = entityUserInput.inputHistory.FindIndex(x => x.timeCreated == currentInput.timeCreated);
                if (historicalInputIndex >= 0) 
                {
                    ReplicationMessage msg = entityUserInput.inputHistory[historicalInputIndex];
                    msg.pos = currentInput.pos;
                    ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, entityUserInput);
                }
            });
        }
    }
}