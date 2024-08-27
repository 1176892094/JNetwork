// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-04  23:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using UnityEngine;

namespace JFramework.Net
{
    internal class DebugManager : Component<NetworkManager>
    {
        private static Rect windowRect;
        private static float windowScale => Screen.width / 2560f + Screen.height / 1440f;

        private void Awake()
        {
            windowRect.position = new Vector2((Screen.width - 200 * windowScale) / windowScale, 0);
            windowRect.size = new Vector2(200f, 75f);
        }

        public static void Update()
        {
            var matrix = GUI.matrix;
            var skin = GUI.skin;
            var textField = skin.textField;
            GUI.matrix = Matrix4x4.Scale(new Vector3(windowScale, windowScale, 1f));
            windowRect = GUI.Window(1, windowRect, Window, "网络调试器");
            skin.textField = textField;
            GUI.skin = skin;
            GUI.matrix = matrix;
        }

        private static void Window(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000f, 20f));
            if (!NetworkManager.Client.isConnected && !NetworkManager.Server.isActive)
            {
                if (!NetworkManager.Client.isActive)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Server"))
                    {
                        NetworkManager.StartServer();
                    }

                    if (GUILayout.Button("Client"))
                    {
                        NetworkManager.StartClient();
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Host"))
                    {
                        NetworkManager.StartHost();
                    }

                    var address = NetworkManager.Transport.address;
                    NetworkManager.Transport.address = GUILayout.TextField(address, new[] { GUILayout.Width(86.5f), GUILayout.Height(22) });
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label($"<b>Connecting...</b>", "Box");
                    if (GUILayout.Button("Stop Client"))
                    {
                        NetworkManager.StopClient();
                    }
                }
            }
            else
            {
                if (NetworkManager.Server.isActive || NetworkManager.Client.isActive)
                {
                    GUILayout.Label($"<b>{NetworkManager.Transport.address} : {NetworkManager.Transport.port}</b>", "Box");
                }
            }

            if (NetworkManager.Client.isConnected && !NetworkManager.Client.isReady)
            {
                if (GUILayout.Button("Ready"))
                {
                    NetworkManager.Client.Ready();
                }
            }

            if (NetworkManager.Server.isActive && NetworkManager.Client.isConnected)
            {
                if (GUILayout.Button("Stop Host"))
                {
                    NetworkManager.StopHost();
                }
            }
            else if (NetworkManager.Client.isConnected)
            {
                if (GUILayout.Button("Stop Client"))
                {
                    NetworkManager.StopClient();
                }
            }
            else if (NetworkManager.Server.isActive)
            {
                if (GUILayout.Button("Stop Server"))
                {
                    NetworkManager.StopServer();
                }
            }
        }
    }
}