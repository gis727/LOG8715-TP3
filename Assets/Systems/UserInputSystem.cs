using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInputSystem : ISystem
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
        
        if      (ECSManager.Instance.NetworkManager.isClient) UpdateClientSystem();
        else if (ECSManager.Instance.NetworkManager.isServer) UpdateServerSystem();
    }

    void UpdateClientSystem()
    {
        uint clientId = (uint)ECSManager.Instance.NetworkManager.LocalClientId;
        int currentTime = Utils.SystemTime;

        if (ComponentsManager.Instance.TryGetComponent(clientId, out ShapeComponent shapeComponent))
        {
            // Build a new message
            ReplicationMessage msg = new ReplicationMessage();
            msg.entityId = clientId;
            msg.timeCreated = currentTime;
            msg.entityId = clientId;
            msg.shape = shapeComponent.shape;
            msg.pos = shapeComponent.pos;
            msg.speed = shapeComponent.speed;
            msg.inputA = 0;
            msg.inputW = 0;
            msg.inputS = 0;
            msg.inputD = 0;

            // Get user inputs
            bool inputDetected = Utils.GetUserInput(ref msg, ref shapeComponent, true);

            // Save the inputs if required
            if (ECSManager.Instance.Config.enableInputPrediction) ComponentsManager.Instance.SetComponent<ShapeComponent>(clientId, shapeComponent);

            
            ComponentsManager.Instance.ForEach<ShapeComponent, UserInputComponent>((entityID, entityShape, entityUserInput) =>
            {
                ReplicationMessage entityMsg = new ReplicationMessage();
                entityMsg.timeCreated = currentTime;
                entityMsg.pos = entityShape.pos;
                entityMsg.size = entityShape.size;
                entityMsg.speed = entityShape.speed;
                entityMsg.shape = entityShape.shape;

                if (entityID == clientId && inputDetected)
                {
                    if (ECSManager.Instance.Config.enableInputPrediction)
                    {
                        entityMsg.inputA = msg.inputA;
                        entityMsg.inputW = msg.inputW;
                        entityMsg.inputS = msg.inputS;
                        entityMsg.inputD = msg.inputD;
                    }

                    // Put the new input message in the queue to server
                    entityUserInput.pendingInputsMessages.Add(msg);
                }

                // save in history
                entityUserInput.inputHistory.Add(entityMsg);

                ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, entityUserInput);
            });
            
            // Clear old messages
            ClearOldInputs(currentTime);
        }
    }

    void UpdateServerSystem()
    {
        ComponentsManager.Instance.ForEach<UserInputComponent>((entityID, userInput) =>
        {
            bool shapeSpawned = ComponentsManager.Instance.TryGetComponent(entityID, out ShapeComponent shapeComp);
            bool isPlayer = ComponentsManager.Instance.EntityContains<PlayerComponent>(entityID);
            if (shapeSpawned && isPlayer)
            {
                for (int i = 0; i < userInput.pendingInputsMessages.Count; i++)
                {   
                    ShapeComponent shapeComponent = ComponentsManager.Instance.GetComponent<ShapeComponent>(entityID);
                    
                    // Get the user input coming from the client
                    ReplicationMessage msg = userInput.pendingInputsMessages[i];
                    Utils.GetUserInput(ref msg, ref shapeComponent, false);

                    ComponentsManager.Instance.SetComponent<ShapeComponent>(entityID, shapeComponent);

                    // Prepare fast forward:
                    // Set a replication message to let the world know that this entity is concerned by fast forward
                    userInput.fastForwardInputsMessages = new List<ReplicationMessage>{new ReplicationMessage()};
                    ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, userInput);
                    
                    ECSManager.Instance.FastForward(1);

                    // Reset speed to 0
                    ShapeComponent newShapeComponent = ComponentsManager.Instance.GetComponent<ShapeComponent>(entityID);
                    newShapeComponent.speed = Vector2.zero;
                    ComponentsManager.Instance.SetComponent<ShapeComponent>(entityID, newShapeComponent);
                }
                userInput.pendingInputsMessages.Clear();
                ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, userInput);
            }
        });
    }


    // Clears inputs older than 'maxAge' from the input history
    private void ClearOldInputs(int currentTime)
    {
        int maxAge = 2000;
        ComponentsManager.Instance.ForEach<UserInputComponent>((entityID,entityUserInput) =>
        {
            entityUserInput.inputHistory.RemoveAll(x => (currentTime - x.timeCreated) > maxAge);
            ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, entityUserInput);
        });
    }
}