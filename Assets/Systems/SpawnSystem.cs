using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : ISystem
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

        if (ECSManager.Instance.NetworkManager.isServer)
        {
            //Only server spawns shapes from config
            bool spawnFound = ComponentsManager.Instance.TryGetComponent(new EntityComponent(0), out SpawnInfo spawnInfo);

            if (!spawnFound || !spawnInfo.spawnDone)
            {
                uint currentID = 1;
                float minSpeed = 2.0f;
                float maxSpeed = 5.0f;
                foreach (var entity in ECSManager.Instance.Config.allShapesToSpawn)
                {
                    float xDir = UnityEngine.Random.value > 0.5 ? 1 : -1;
                    float yDir = UnityEngine.Random.value > 0.5 ? 1 : -1;
                    SpawnEntity(currentID++, new Vector2(xDir * UnityEngine.Random.Range(minSpeed, maxSpeed), yDir * UnityEngine.Random.Range(minSpeed, maxSpeed)), entity);
                }
                if (!spawnFound)
                {
                    spawnInfo = new SpawnInfo(true);
                }
                ComponentsManager.Instance.SetComponent<SpawnInfo>(new EntityComponent(0), spawnInfo);
            }
            for (int i = 0; i < spawnInfo.playersToSpawn.Count; i++)
            {
                uint playerId = spawnInfo.playersToSpawn[i];
                bool shapeSpawned = ComponentsManager.Instance.TryGetComponent(playerId, out ShapeComponent shapeComponent);
                if (shapeSpawned)
                {
                    SpawnPlayerEntity(playerId, shapeComponent);
                    spawnInfo.playersToSpawn.RemoveAt(i);
                    i--;
                }
            }
            ComponentsManager.Instance.SetComponent<SpawnInfo>(new EntityComponent(0), spawnInfo);
        }
        else if (ECSManager.Instance.NetworkManager.isClient)
        {
            uint clientId = (uint)ECSManager.Instance.NetworkManager.LocalClientId;
            bool playerSpawned = ComponentsManager.Instance.TryGetComponent(clientId, out PlayerComponent playerComponent);
            if (!playerSpawned)
            {
                bool shapeSpawned = ComponentsManager.Instance.TryGetComponent(clientId, out ShapeComponent shapeComponent);
                if (shapeSpawned)
                {
                    SpawnPlayerEntity(clientId, shapeComponent);
                }
            }

            bool spawnFound = ComponentsManager.Instance.TryGetComponent(new EntityComponent(0), out SpawnInfo spawnInfo);

            if (spawnFound)
            {
                foreach (var msgReplication in spawnInfo.replicatedEntitiesToSpawn)
                {
                    var entityConfig = new Config.ShapeConfig();
                    entityConfig.shape = msgReplication.shape;
                    entityConfig.size = msgReplication.size;
                    entityConfig.initialPos = msgReplication.pos;
                    SpawnEntity(msgReplication.entityId, msgReplication.speed, entityConfig);
                }
                spawnInfo.replicatedEntitiesToSpawn.Clear();

                ComponentsManager.Instance.SetComponent<SpawnInfo>(new EntityComponent(0), spawnInfo);
            }
        }
    }

    public static void SpawnEntity(uint entityId, Vector2 speed, Config.ShapeConfig entityConfig)
    {
        ECSManager.Instance.CreateShape(entityId, entityConfig);
        ShapeComponent shapeData = new ShapeComponent();
        shapeData.pos = entityConfig.initialPos;
        shapeData.speed = speed;
        shapeData.shape = entityConfig.shape;
        shapeData.size = entityConfig.size;
        ComponentsManager.Instance.SetComponent<ShapeComponent>(entityId, shapeData);
        ComponentsManager.Instance.SetComponent<EntityComponent>(entityId, new EntityComponent(entityId));
    }

    public void SpawnPlayerEntity(uint playerId, ShapeComponent shapeData)
    {
        var entityConfig = new Config.ShapeConfig();
        entityConfig.shape = Config.Shape.Circle;
        entityConfig.size = shapeData.size;
        entityConfig.initialPos = shapeData.pos;
        SpawnEntity(playerId, new Vector2(), entityConfig);
        PlayerComponent playerComponent = new PlayerComponent() { playerId = playerId };
        ComponentsManager.Instance.SetComponent<PlayerComponent>(playerId, playerComponent);
    }
}
