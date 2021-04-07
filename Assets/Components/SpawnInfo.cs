using System.Collections.Generic;

public struct SpawnInfo : IComponent
{
    public bool spawnDone;
    public List<uint> playersToSpawn;
    public List<ReplicationMessage> replicatedEntitiesToSpawn;

    public SpawnInfo(bool spawnDone)
    {
        this.spawnDone = spawnDone;
        this.playersToSpawn = new List<uint>();
        this.replicatedEntitiesToSpawn = new List<ReplicationMessage>();
    }
}