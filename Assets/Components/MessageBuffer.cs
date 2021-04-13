using System.Collections.Generic;
using UnityEngine;
public class MessageBuffer : IComponent
{
    public List<ReplicationMessage> buffer;
    public MessageBuffer()
    {
        this.buffer = new List<ReplicationMessage>();
    }
}