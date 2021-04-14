using UnityEngine;


public class ReconciliationSystem : ISystem
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

        if (ECSManager.Instance.NetworkManager.isClient)
        {
            UpdateSystemClient();
        }
    }

    public static void UpdateSystemClient()
    {
        MessageBuffer msgBuffer = ComponentsManager.Instance.GetComponent<MessageBuffer>(new EntityComponent(0));
        
        // Wait until the buffer has all entities before launching reconciliation
        bool bufferIsFull = msgBuffer.buffer.Count >= ECSManager.Instance.Config.allShapesToSpawn.Count;
        if (bufferIsFull)
        {
            msgBuffer.buffer.Sort((x,y) => (int)(x.timeCreated - y.timeCreated));
            if (ECSManager.Instance.Config.enablDeadReckoning) LaunchReconciliation(msgBuffer);
            msgBuffer.buffer.Clear();
        }
        ComponentsManager.Instance.SetComponent<MessageBuffer>(new EntityComponent(0), msgBuffer);
    }

    private static void LaunchReconciliation(MessageBuffer msgBuffer)
    {
        int MAX_OFFSET = 1; // this is how much offset we want to tolerate on each entity before actually reconciling
        int oldInputIndex = ClientIsUpToDateWithServer(msgBuffer, MAX_OFFSET);

        if (oldInputIndex < 0) return; // We are up to date with the server

        // Accept server dictatorship
        foreach (ReplicationMessage msg in msgBuffer.buffer)
        {
            var component = ComponentsManager.Instance.GetComponent<ShapeComponent>(msg.entityId);
            component.pos = msg.pos;
            component.speed = msg.speed;
            component.size = msg.size;
            ComponentsManager.Instance.SetComponent<ShapeComponent>(msg.entityId, component);
        }

        // Setup fast forward
        uint clientId = (uint)ECSManager.Instance.NetworkManager.LocalClientId;
        var userInputs= ComponentsManager.Instance.GetComponent<UserInputComponent>(clientId);
        int inputsLength = userInputs.inputHistory.Count - oldInputIndex;
        userInputs.fastForwardInputsMessages = userInputs.inputHistory.GetRange(oldInputIndex, inputsLength);
        ComponentsManager.Instance.SetComponent<UserInputComponent>(clientId, userInputs);
        
        // Run fast forward (simulate inputs)
        var userInputComponent = ComponentsManager.Instance.GetComponent<UserInputComponent>(clientId);
        ECSManager.Instance.FastForward(userInputComponent.fastForwardInputsMessages.Count);
    }

    private static int ClientIsUpToDateWithServer(MessageBuffer msgBuffer, int maxOffset)
    {
        // Compute buffer time and network lag
        int bufferTime = msgBuffer.buffer[msgBuffer.buffer.Count-1].timeCreated;
        float networkLag = Utils.SystemTime - bufferTime + Time.deltaTime;

        bool upToDateWithServer = true;
        int oldInputIndex = -1;

        for(int i = 0; i < msgBuffer.buffer.Count; i++)
        {
            ReplicationMessage msg = msgBuffer.buffer[i];
            var userInput = ComponentsManager.Instance.GetComponent<UserInputComponent>(msg.entityId);

            oldInputIndex = userInput.inputHistory.FindIndex(x => x.timeCreated >= (bufferTime - networkLag));
            if (oldInputIndex >= 0)
            {
                float offset = (userInput.inputHistory[oldInputIndex].pos - msg.pos).magnitude;
                upToDateWithServer = upToDateWithServer && offset <= maxOffset;
            }
        }
        return upToDateWithServer? -1: oldInputIndex;
    }
}