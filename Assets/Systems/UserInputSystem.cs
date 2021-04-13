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
        
        if (ECSManager.Instance.NetworkManager.isClient)
        {
            DoClientStuff();
        }
        else if (ECSManager.Instance.NetworkManager.isServer)
        {
            DoServerStuff();
        }
    }

    void DoClientStuff()
    {
        bool inputDetected = false;
        int speed = 4;
        uint clientId = (uint)ECSManager.Instance.NetworkManager.LocalClientId;
        bool messagingInfoFound = ComponentsManager.Instance.TryGetComponent(new EntityComponent(0), out MessagingInfo messagingInfo);
        if (messagingInfoFound) messagingInfo.localMessageId++;
        int currentTime = Utils.SystemTime;
        if (ComponentsManager.Instance.TryGetComponent(clientId, out ShapeComponent shapeComponent))
        {
            ReplicationMessage msg = new ReplicationMessage();
            msg.entityId = clientId;
            msg.timeCreated = currentTime;
            msg.messageID = messagingInfoFound? messagingInfo.localMessageId: 0;
            msg.entityId = clientId;
            msg.shape = shapeComponent.shape;
            msg.pos = shapeComponent.pos;
            msg.speed = shapeComponent.speed;
            msg.inputA = 0;
            msg.inputW = 0;
            msg.inputS = 0;
            msg.inputD = 0;
            shapeComponent.speed = Vector2.zero;
            
            if (Input.GetKey(KeyCode.A))
            {
                shapeComponent.speed = Vector2.left * speed;
                msg.inputA = 1;
                inputDetected = true;
            }
            else if (Input.GetKey(KeyCode.W))
            {
                shapeComponent.speed = Vector2.up * speed;
                msg.inputW = 1;
                inputDetected = true;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                shapeComponent.speed = Vector2.down * speed;
                msg.inputS = 1;
                inputDetected = true;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                shapeComponent.speed = Vector2.right * speed;
                msg.inputD = 1;
                inputDetected = true;
            }
            if (ECSManager.Instance.Config.enableInputPrediction) ComponentsManager.Instance.SetComponent<ShapeComponent>(clientId, shapeComponent);

            // save the world state in history
            ComponentsManager.Instance.ForEach<ShapeComponent, UserInputComponent>((entityID, entityShape, entityUserInput) =>
            {
                ReplicationMessage entityMsg = new ReplicationMessage();
                entityMsg.messageID = messagingInfoFound? messagingInfo.localMessageId: 0;
                entityMsg.timeCreated = currentTime;
                entityMsg.pos = entityShape.pos;
                entityMsg.size = entityShape.size;
                entityMsg.speed = entityShape.speed;
                entityMsg.shape = entityShape.shape;
                entityUserInput.inputHistory.Add(entityMsg);

                if (entityID == clientId && inputDetected)
                {
                    entityUserInput.pendingInputsMessages.Add(msg);
                }

                ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, entityUserInput);
            });
            
            // clear old messages
            ComponentsManager.Instance.ForEach<UserInputComponent>((entityID,entityUserInput) =>
            {
                entityUserInput.inputHistory.RemoveAll(x => (currentTime - x.timeCreated) > 5000);
                ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, entityUserInput);
            });
        }
    }

    void DoServerStuff()
    {
        int speed = 4;
        ComponentsManager.Instance.ForEach<UserInputComponent>((entityID, userInput) =>
        {
            bool shapeSpawned = ComponentsManager.Instance.TryGetComponent(entityID, out ShapeComponent shapeComponent);
            bool isPlayer = ComponentsManager.Instance.EntityContains<PlayerComponent>(entityID);
            if (shapeSpawned && isPlayer)
            {
                shapeComponent.speed = Vector2.zero;
                for (int i=0; i < userInput.pendingInputsMessages.Count; i++)
                {   
                    var msg = userInput.pendingInputsMessages[i];
                    if (msg.inputA == 1)
                    {
                        shapeComponent.speed = Vector2.left * speed;
                    }
                    else if (msg.inputW == 1)
                    {
                        shapeComponent.speed = Vector2.up * speed;
                    }
                    else if (msg.inputS == 1)
                    {
                        shapeComponent.speed = Vector2.down * speed;
                    }
                    else if (msg.inputD == 1)
                    {
                        shapeComponent.speed = Vector2.right * speed;
                    }

                    userInput.fastForwardInputsMessages = new List<ReplicationMessage>{msg};
                    ComponentsManager.Instance.SetComponent<ShapeComponent>(entityID, shapeComponent);
                    ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, userInput);
                    ECSManager.Instance.FastForward(1);
                }
                userInput.pendingInputsMessages.Clear();
                ComponentsManager.Instance.SetComponent<ShapeComponent>(entityID, shapeComponent);
            }
        });
    }
}