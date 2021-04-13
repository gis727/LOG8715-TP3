using System.Collections.Generic;
public class MessageBuffer : IComponent
{
    public List<ReplicationMessage> buffer;
    public MessageBuffer()
    {
        this.buffer = new List<ReplicationMessage>();
    }
}