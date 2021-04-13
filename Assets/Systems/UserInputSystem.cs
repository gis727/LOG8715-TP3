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

                if (entityID == clientId && inputDetected)
                {
                    if (ECSManager.Instance.Config.enableInputPrediction)
                    {
                        entityMsg.inputA = msg.inputA;
                        entityMsg.inputW = msg.inputW;
                        entityMsg.inputS = msg.inputS;
                        entityMsg.inputD = msg.inputD;
                    }
                    entityUserInput.pendingInputsMessages.Add(msg);
                }

                entityUserInput.inputHistory.Add(entityMsg);

                ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, entityUserInput);
            });
            
            // clear old messages
            ComponentsManager.Instance.ForEach<UserInputComponent>((entityID,entityUserInput) =>
            {
                entityUserInput.inputHistory.RemoveAll(x => (currentTime - x.timeCreated) > 2000);
                ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, entityUserInput);
            });
        }
    }
    void DoServerStuff()
    {
        int speed = 4;
        ComponentsManager.Instance.ForEach<UserInputComponent>((entityID, userInput) =>
        {
            bool shapeSpawned = ComponentsManager.Instance.TryGetComponent(entityID, out ShapeComponent shapeComp);
            bool isPlayer = ComponentsManager.Instance.EntityContains<PlayerComponent>(entityID);
            if (shapeSpawned && isPlayer)
            {
                for (int i=0; i < userInput.pendingInputsMessages.Count; i++)
                {   
                    ShapeComponent shapeComponent = ComponentsManager.Instance.GetComponent<ShapeComponent>(entityID);
                    shapeComponent.speed = Vector2.zero;
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
                    ComponentsManager.Instance.SetComponent<ShapeComponent>(entityID, shapeComponent);

                    // prepare fast forward
                    // set a replication message to let the world know that this entity is concerned by fast forward
                    userInput.fastForwardInputsMessages = new List<ReplicationMessage>{new ReplicationMessage()};
                    ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, userInput);
                    
                    // fast forward (simulate input)
                    ECSManager.Instance.FastForward(1);

                    // reset 0 speed
                    ShapeComponent newShapeComponent = ComponentsManager.Instance.GetComponent<ShapeComponent>(entityID);
                    newShapeComponent.speed = Vector2.zero;
                    ComponentsManager.Instance.SetComponent<ShapeComponent>(entityID, newShapeComponent);
                }
                userInput.pendingInputsMessages.Clear();
                ComponentsManager.Instance.SetComponent<UserInputComponent>(entityID, userInput);
            }
        });
    }
}