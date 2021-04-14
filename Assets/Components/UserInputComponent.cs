using System.Collections.Generic;
public struct UserInputComponent : IComponent
{
    public List<ReplicationMessage> inputHistory;
    public List<ReplicationMessage> pendingInputsMessages;
    public List<ReplicationMessage> fastForwardInputsMessages;
    public List<ReplicationMessage> pendingServerMessages;
    int lastSentTime;

    public UserInputComponent(int lastSentTime)
    {
        this.lastSentTime = lastSentTime;
        this.inputHistory = new List<ReplicationMessage>();
        this.pendingInputsMessages = new List<ReplicationMessage>();
        this.fastForwardInputsMessages = new List<ReplicationMessage>();
        this.pendingServerMessages = new List<ReplicationMessage>();
    }
}