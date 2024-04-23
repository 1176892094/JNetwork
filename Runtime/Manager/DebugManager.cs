using UnityEngine;

namespace JFramework.Net
{
    internal class DebugManager : Component<NetworkManager>
    {
        private Rect windowRect;
        private static float screenWidth => Screen.width;
        private static float screenHeight => Screen.height;
        private static float windowScale => screenWidth / 1920f + screenHeight / 1080f;
        private static Transport transport => NetworkManager.Transport;
        private static ClientManager client => NetworkManager.Client;
        private static ServerManager server => NetworkManager.Server;

        private void Awake()
        {
            windowRect.position = new Vector2(screenWidth / windowScale - 200, 0);
            windowRect.size = new Vector2(200f, 75f);
        }

        public void OnUpdate()
        {
            var matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(windowScale, windowScale, 1f));
            windowRect = GUI.Window(1, windowRect, Window, "网络调试器");
            GUI.matrix = matrix;
        }

        private void Window(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000f, 20f));
            if (!client.isAuthority && !server.isActive)
            {
                StartButton();
            }
            else
            {
                StatsButton();
            }

            if (client.isAuthority && !client.isReady)
            {
                if (GUILayout.Button("准备"))
                {
                    client.Ready();
                }
            }

            StopButton();
        }

        private static void StartButton()
        {
            if (!client.isActive)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("服务器"))
                {
                    NetworkManager.Instance.StartServer();
                }

                if (GUILayout.Button("客户端"))
                {
                    NetworkManager.Instance.StartClient();
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("主机"))
                {
                    NetworkManager.Instance.StartHost();
                }

                transport.address = GUILayout.TextField(transport.address, GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label($"连接至 {transport.address}:{transport.port}...");
                if (GUILayout.Button("停止连接"))
                {
                    NetworkManager.Instance.StopClient();
                }
            }
        }

        private static void StopButton()
        {
            if (server.isActive && client.isAuthority)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("停止主机"))
                {
                    NetworkManager.Instance.StopHost();
                }

                if (GUILayout.Button("停止客户端"))
                {
                    NetworkManager.Instance.StopClient();
                }

                GUILayout.EndHorizontal();
            }
            else if (client.isAuthority)
            {
                if (GUILayout.Button("停止客户端"))
                {
                    NetworkManager.Instance.StopClient();
                }
            }
            else if (server.isActive)
            {
                if (GUILayout.Button("停止服务器"))
                {
                    NetworkManager.Instance.StopServer();
                }
            }
        }

        private static void StatsButton()
        {
            if (server.isActive && client.isActive)
            {
                GUILayout.Label($"<b>主机: {transport.address}:{transport.port}</b>", "Box");
            }
            else if (server.isActive)
            {
                GUILayout.Label($"<b>服务器: {transport.address}:{transport.port}</b>", "Box");
            }
            else if (client.isAuthority)
            {
                GUILayout.Label($"<b>客户端: {transport.address}:{transport.port}</b>", "Box");
            }
        }
    }
}