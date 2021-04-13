﻿using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;

public class CustomNetworkManager : NetworkingManager
{
    public void Awake()
    {
        OnClientConnectedCallback += OnClientConnected;
        OnServerStarted += OnStartServer;
    }
    
    public void OnClientConnected(ulong clientId)
    {
        if (isServer)
        {
            bool spawnFound = ComponentsManager.Instance.TryGetComponent(new EntityComponent(0), out SpawnInfo spawnInfo);

            if (!spawnFound)
            {
                spawnInfo = new SpawnInfo(false);
            }
            spawnInfo.playersToSpawn.Add((uint)clientId);
            ComponentsManager.Instance.SetComponent<SpawnInfo>(new EntityComponent(0), spawnInfo);
        }
        else
        {
            RegisterClientNetworkHandlers();
        }
    }

    public void OnStartServer()
    {
        RegisterServerNetworkHandlers();
    }

    public void SendReplicationMessage(ReplicationMessage msg)
    {
        using (PooledBitStream stream = PooledBitStream.Get())
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteInt32(msg.messageID);
                writer.WriteInt32(msg.timeCreated);
                writer.WriteUInt32(msg.entityId);
                writer.WriteInt16((byte)msg.shape);
                writer.WriteVector2(msg.pos);
                writer.WriteVector2(msg.speed);
                writer.WriteDouble(msg.size);
                CustomMessagingManager.SendNamedMessage("Replication", null, stream, "customChannel");
            }
        }
    }

    public void SendReplicationMessageToServer(ReplicationMessage msg)
    {
        using (PooledBitStream stream = PooledBitStream.Get())
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteInt32(msg.messageID);
                writer.WriteInt32(msg.timeCreated);
                writer.WriteUInt32(msg.entityId);
                writer.WriteInt16((byte)msg.shape);
                writer.WriteVector2(msg.pos);
                writer.WriteVector2(msg.speed);
                writer.WriteDouble(msg.size);
                writer.WriteUInt32(msg.inputA);
                writer.WriteUInt32(msg.inputW);
                writer.WriteUInt32(msg.inputS);
                writer.WriteUInt32(msg.inputD);
                CustomMessagingManager.SendNamedMessage("Replication", this.ServerClientId, stream, "customChannel");
            }
        }
    }


    private void HandleReplicationMessage(ulong clientId, Stream stream)
    {
        ReplicationMessage replicationMessage = new ReplicationMessage();
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            replicationMessage.messageID = reader.ReadInt32();
            replicationMessage.timeCreated = reader.ReadInt32();
            replicationMessage.entityId = reader.ReadUInt32();
            replicationMessage.shape = (Config.Shape)reader.ReadInt16();
            replicationMessage.pos = reader.ReadVector2();
            replicationMessage.speed = reader.ReadVector2();
            replicationMessage.size = (float)reader.ReadDouble();
            replicationMessage.handled = false;
            ComponentsManager.Instance.SetComponent<ReplicationMessage>(replicationMessage.entityId, replicationMessage);
            if (!ComponentsManager.Instance.EntityContains<EntityComponent>(replicationMessage.entityId))
            {
                bool spawnFound = ComponentsManager.Instance.TryGetComponent(new EntityComponent(0), out SpawnInfo spawnInfo);

                if (!spawnFound)
                {
                    spawnInfo = new SpawnInfo(false);
                }
                spawnInfo.replicatedEntitiesToSpawn.Add(replicationMessage);
                ComponentsManager.Instance.SetComponent<SpawnInfo>(new EntityComponent(0), spawnInfo);
            }
            MessagingInfo messagingInfo = ComponentsManager.Instance.GetComponent<MessagingInfo>(new EntityComponent(0));
            messagingInfo.currentMessageId = replicationMessage.messageID;
            if (messagingInfo.localMessageId < 0) messagingInfo.localMessageId = replicationMessage.messageID;
            ComponentsManager.Instance.SetComponent<MessagingInfo>(new EntityComponent(0), messagingInfo);
        }
    }

    public void RegisterClientNetworkHandlers()
    {
        CustomMessagingManager.RegisterNamedMessageHandler("Replication", HandleReplicationMessage);
    }

    private void HandleServerReplicationMessage(ulong clientId, Stream stream)
    {
        ReplicationMessage msg = new ReplicationMessage();
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            msg.messageID = reader.ReadInt32();
            msg.timeCreated = reader.ReadInt32();
            msg.entityId = reader.ReadUInt32();
            msg.shape = (Config.Shape)reader.ReadInt16();
            msg.pos = reader.ReadVector2();
            msg.speed = reader.ReadVector2();
            msg.size = (float)reader.ReadDouble();
            msg.inputA = reader.ReadUInt32();
            msg.inputW = reader.ReadUInt32();
            msg.inputS = reader.ReadUInt32();
            msg.inputD = reader.ReadUInt32();
            var userInputComponent = ComponentsManager.Instance.GetComponent<UserInputComponent>(msg.entityId);
            userInputComponent.pendingInputsMessages.Add(msg);
            ComponentsManager.Instance.SetComponent<UserInputComponent>(msg.entityId, userInputComponent);
        }
    }
    public void RegisterServerNetworkHandlers()
    {
        // TODO
        CustomMessagingManager.RegisterNamedMessageHandler("Replication", HandleServerReplicationMessage);
    }



    public new bool isServer { get { return GetConnectionStatus() == ConnectionStatus.isServer; } }
    public new bool isClient { get { return GetConnectionStatus() == ConnectionStatus.isClient; } }

    public enum ConnectionStatus
    {
        isClient,
        isServer,
        notConnected
    }

    public ConnectionStatus GetConnectionStatus()
    {
        if (IsConnectedClient)
        {
            return ConnectionStatus.isClient;
        }
        else if (IsServer && IsListening)
        {
            return ConnectionStatus.isServer;
        }
        else
        {
            return ConnectionStatus.notConnected;
        }
    }
}
