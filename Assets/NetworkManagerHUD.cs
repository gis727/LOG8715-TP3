using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class NetworkManagerHUD : MonoBehaviour
{
    public CustomNetworkManager manager;
    [SerializeField] public int offsetX;
    [SerializeField] public int offsetY;

    void Awake()
    {
        manager = GetComponent<CustomNetworkManager>();
    }

    void Update()
    {

        if (!manager.IsListening && !manager.IsConnectedClient)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                manager.StartServer();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                manager.StartClient();
            }
        }
    }

    void OnGUI()
    {

        int xpos = 10 + offsetX;
        int ypos = 40 + offsetY;
        int spacing = 24;

        if (!manager.IsListening && !manager.IsConnectedClient)
        {
            if (GUI.Button(new Rect(xpos, ypos, 200, 20), "LAN Client(C)"))
            {
                manager.StartClient();
            }
            
            ypos += spacing;

            if (GUI.Button(new Rect(xpos, ypos, 200, 20), "LAN Server(S)"))
            {
                manager.StartServer();
            }
            ypos += spacing;
        }
        else
        {
            if (manager.IsServer && manager.IsListening)
            {
                GUI.Label(new Rect(xpos, ypos, 300, 20), "Server");
                ypos += spacing;
            }
            if (manager.IsClient)
            {
                if (manager.IsConnectedClient)
                {
                    GUI.Label(new Rect(xpos, ypos, 300, 20), "Client: connected");
                    ypos += spacing;
                }
                else
                {
                    GUI.Label(new Rect(xpos, ypos, 300, 20), "Client: connecting");
                    ypos += spacing;
                }
            }
        }

        if (manager.IsListening || manager.IsConnectedClient)
        {
            if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Quit (X)"))
            {
                if (manager.IsClient)
                {
                    manager.StopClient();
                }
                else if (manager.IsServer)
                {
                    manager.StopServer();
                }
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}

