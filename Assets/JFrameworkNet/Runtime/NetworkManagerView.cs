using JFramework.Net;
using UnityEngine;

namespace JFramework.Editor
{
    [DisallowMultipleComponent]
    public class NetworkManagerView : MonoBehaviour
    {
        private NetworkManager manager;

        public int offsetX;
        public int offsetY;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - offsetX, 40 + offsetY, 250, 9999));

            if (!NetworkClient.isConnect && !NetworkServer.isActive)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            if (NetworkClient.isConnect && !NetworkClient.isReady)
            {
                if (GUILayout.Button("Client Ready"))
                {
                    NetworkClient.Ready();
                }
            }

            StopButtons();

            GUILayout.EndArea();
        }

        void StartButtons()
        {
            if (!NetworkClient.isActive)
            {
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    if (GUILayout.Button("Start Host"))
                    {
                        manager.StartHost();
                    }
                }

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Start Client"))
                {
                    manager.StartClient();
                }

                GUILayout.TextField(manager.address.ip);

                GUILayout.EndHorizontal();

                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    GUILayout.Box("(  WebGL cannot be server  )");
                }
                else
                {
                    if (GUILayout.Button("Start Server"))
                    {
                        manager.StartServer();
                    }
                }
            }
            else
            {
                GUILayout.Label($"Connecting to {manager.address.ip}..");
                if (GUILayout.Button("Cancel Connection Attempt"))
                {
                    manager.StopClient();
                }
            }
        }

        private void StatusLabels()
        {
            if (NetworkServer.isActive && NetworkClient.isActive)
            {
                GUILayout.Label($"<b>Host</b>: running via {Transport.current}");
            }
            else if (NetworkServer.isActive)
            {
                GUILayout.Label($"<b>Server</b>: running via {Transport.current}");
            }
            else if (NetworkClient.isConnect)
            {
                GUILayout.Label($"<b>Client</b>: connected to {manager.address.ip} via {Transport.current}");
            }
        }

        private void StopButtons()
        {
            if (NetworkServer.isActive && NetworkClient.isConnect)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Stop Host"))
                {
                    manager.StopHost();
                }

                if (GUILayout.Button("Stop Client"))
                {
                    manager.StopClient();
                }

                GUILayout.EndHorizontal();
            }
            else if (NetworkClient.isConnect)
            {
                if (GUILayout.Button("Stop Client"))
                {
                    manager.StopClient();
                }
            }
            else if (NetworkServer.isActive)
            {
                if (GUILayout.Button("Stop Server"))
                {
                    manager.StopServer();
                }
            }
        }
    }
}