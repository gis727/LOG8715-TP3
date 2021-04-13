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
        if (ECSManager.Instance.RunningFastForward) return;

        if (ECSManager.Instance.NetworkManager.isServer)
        {
            UpdateSystemServer();
        }
        else if (ECSManager.Instance.NetworkManager.isClient)
        {
            UpdateSystemClient();
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
    }
}