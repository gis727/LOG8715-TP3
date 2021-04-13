using System.Collections.Generic;
using UnityEngine;


public class ReplicationSystem : ISystem
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
        
        if (ECSManager.Instance.NetworkManager.isServer && !ECSManager.Instance.RunningFastForward)
        {
            UpdateSystemServer();
        }
        else if (ECSManager.Instance.NetworkManager.isClient)
        {
            if (ECSManager.Instance.RunningFastForward) UpdateClientInFastForward();
            else UpdateSystemClient();
        }
    }

    public static void UpdateSystemServer()
    {
        // creates messages from current state

        ComponentsManager.Instance.ForEach<ShapeComponent>((entityID, shapeComponent) => {
            ReplicationMessage msg = new ReplicationMessage() {
                messageID = 0,
                timeCreated = Utils.SystemTime,
                entityId = entityID.id,
                shape = shapeComponent.shape,
                pos = shapeComponent.pos,
                speed = shapeComponent.speed,
                size = shapeComponent.size
            };
            ComponentsManager.Instance.SetComponent<ReplicationMessage>(entityID, msg);
        });
    }
    public static void UpdateSystemClient()
    {
        MessageBuffer msgBuffer;
        if (!ComponentsManager.Instance.TryGetComponent<MessageBuffer>(new EntityComponent(0), out msgBuffer))
        {
            msgBuffer = new MessageBuffer();
            ComponentsManager.Instance.SetComponent<MessageBuffer>(new EntityComponent(0), msgBuffer);
        }
        
        // can receive only one replication message per entity for simplicity
        ComponentsManager.Instance.ForEach<ReplicationMessage>((entityID, msgReplication) => {
            if (msgReplication.handled) return;
            if (msgBuffer.buffer.Count < ECSManager.Instance.Config.allShapesToSpawn.Count) msgBuffer.buffer.Add(msgReplication);

            // Updating entity info from message's state
            var component = ComponentsManager.Instance.GetComponent<ShapeComponent>(msgReplication.entityId);

            if (component.shape != msgReplication.shape)
            {
                // needs to respawn entity to change its shape
                bool spawnFound = ComponentsManager.Instance.TryGetComponent(new EntityComponent(0), out SpawnInfo spawnInfo);

                if (!spawnFound)
                {
                    spawnInfo = new SpawnInfo(false);
                }
                spawnInfo.replicatedEntitiesToSpawn.Add(msgReplication);
                ComponentsManager.Instance.SetComponent<SpawnInfo>(new EntityComponent(0), spawnInfo);
            }
            else if (!ECSManager.Instance.Config.enablDeadReckoning)
            {
                component.pos = msgReplication.pos;
                component.speed = msgReplication.speed;
                component.size = msgReplication.size;
                ComponentsManager.Instance.SetComponent<ShapeComponent>(msgReplication.entityId, component);
            }
            
            msgReplication.handled = true;
            ComponentsManager.Instance.SetComponent<ReplicationMessage>(entityID, msgReplication);
        });

        // Wait until the buffer has all entities
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
        int maxOffset = 1; // this is how much offset we want to tolerate on each entity before actually reconciling
        int oldInputIndex = ClientIsUpToDateWithServer(msgBuffer, maxOffset);

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
