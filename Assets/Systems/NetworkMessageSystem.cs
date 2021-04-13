using UnityEngine;
public class NetworkMessageSystem : ISystem
{
    public string Name
    {
        get
        {
            return GetType().Name;
        }
    }

    // In charge of sending all messages pending sending
    public void UpdateSystem()
    {
        if (ECSManager.Instance.RunningFastForward) return;

        bool messagingInfoFound = ComponentsManager.Instance.TryGetComponent(new EntityComponent(0), out MessagingInfo messagingInfo);

        if (!messagingInfoFound)
        {
            messagingInfo = new MessagingInfo() { currentMessageId = 0 };
        }

        if (ECSManager.Instance.NetworkManager.isServer)
        {
            ComponentsManager.Instance.ForEach<ReplicationMessage>((entityID, msg) =>
            {
                msg.messageID = messagingInfo.currentMessageId++;
                ECSManager.Instance.NetworkManager.SendReplicationMessage(msg);
                msg.handled = true;
                ComponentsManager.Instance.SetComponent<ReplicationMessage>(entityID, msg);
            });
        }

        if (ECSManager.Instance.NetworkManager.isClient)
        {
            // TODO
            bool inputExist = ComponentsManager.Instance.TryGetComponent((uint)ECSManager.Instance.NetworkManager.LocalClientId, out UserInputComponent userInput);
            if (inputExist)
            {
                for (int i=0; i < userInput.pendingInputsMessages.Count; i++)
                {
                    ReplicationMessage msg = userInput.pendingInputsMessages[i];
                    ECSManager.Instance.NetworkManager.SendReplicationMessageToServer(msg);
                }
                userInput.pendingInputsMessages.Clear();
            }
        }

        ComponentsManager.Instance.SetComponent<MessagingInfo>(new EntityComponent(0), messagingInfo);
    }
}