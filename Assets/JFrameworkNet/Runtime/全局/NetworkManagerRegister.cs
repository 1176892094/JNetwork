using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        /// <summary>
        /// 注册服务器事件
        /// </summary>
        private void RegisterServerEvent()
        {
            Debug.Log("注册服务器事件");
            ServerManager.OnConnected -= OnServerConnectEvent;
            ServerManager.OnConnected += OnServerConnectEvent;
            ServerManager.OnDisconnected -= OnServerDisconnectEvent;
            ServerManager.OnDisconnected += OnServerDisconnectEvent;
            ServerManager.RegisterEvent<ReadyEvent>(OnServerReadyEvent);
        }

        /// <summary>
        /// 注册客户端事件
        /// </summary>
        private void RegisterClientEvent()
        {
            Debug.Log("注册客户端事件");
            ClientManager.OnConnected -= OnClientConnectEvent;
            ClientManager.OnConnected += OnClientConnectEvent;
            ClientManager.OnDisconnected -= OnClientDisconnectEvent;
            ClientManager.OnDisconnected += OnClientDisconnectEvent;
            ClientManager.RegisterEvent<NotReadyEvent>(OnClientNotReadyEvent);
            ClientManager.RegisterEvent<SceneEvent>(OnClientLoadSceneEvent, false);
            setting.RegisterPrefab();
        }

        /// <summary>
        /// 当客户端连接到服务器时触发
        /// </summary>
        /// <param name="client">连接的客户端</param>
        private void OnServerConnectEvent(ClientEntity client)
        {
            client.isAuthority = true;
            if (!string.IsNullOrEmpty(sceneName))
            {
                var message = new SceneEvent()
                {
                    sceneName = sceneName
                };
                client.Send(message);
            }

            OnServerConnect?.Invoke(client);
        }

        /// <summary>
        /// 当客户端从服务器断开时触发
        /// </summary>
        /// <param name="client">断开的客户端</param>
        private void OnServerDisconnectEvent(ClientEntity client)
        {
            OnServerDisconnect?.Invoke(client);
        }

        /// <summary>
        /// 当
        /// </summary>
        /// <param name="client"></param>
        /// <param name="event"></param>
        private static void OnServerReadyEvent(ClientEntity client, ReadyEvent @event)
        {
            ServerManager.SetClientReady(client);
            OnServerReady?.Invoke(client);
        }

        private void OnClientConnectEvent()
        {
            ClientManager.connection.isAuthority = true;
            Debug.Log("设置身份验证成功。");
            if (!ClientManager.isReady)
            {
                ClientManager.Ready();
            }

            OnClientConnect?.Invoke();
        }

        private void OnClientDisconnectEvent()
        {
            if (networkMode is NetworkMode.Server or NetworkMode.None) return;
            OnClientDisconnect?.Invoke();
            networkMode = networkMode == NetworkMode.Host ? NetworkMode.Server : NetworkMode.None;
            ClientManager.StopClient();
            OnStopClient?.Invoke();
            if (networkMode == NetworkMode.Server) return;
            sceneName = "";
        }

        private static void OnClientNotReadyEvent(NotReadyEvent @event)
        {
            ClientManager.isReady = false;
            OnClientNotReady?.Invoke();
        }

        private void OnClientLoadSceneEvent(SceneEvent @event)
        {
            if (ClientManager.isConnect)
            {
                ClientLoadScene(@event.sceneName);
            }
        }
    }
}