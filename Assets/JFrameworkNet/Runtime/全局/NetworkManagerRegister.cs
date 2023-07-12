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
        /// 当客户端连接到服务器时，向连接的客户端发送改变场景的事件
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
        /// 当客户端在服务器准备就绪，向客户端发送生成物体的消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="event"></param>
        private static void OnServerReadyEvent(ClientEntity client, ReadyEvent @event)
        {
            ServerManager.SetClientReady(client);
            OnServerReady?.Invoke(client);
        }

        /// <summary>
        /// 客户端连接的事件
        /// </summary>
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

        /// <summary>
        /// 客户端断开连接的事件
        /// </summary>
        private void OnClientDisconnectEvent()
        {
            if (networkMode is NetworkMode.Server or NetworkMode.None) return;
            OnStopClient?.Invoke();
            ClientManager.StopClient();
            OnClientDisconnect?.Invoke();
            if (networkMode == NetworkMode.Server) return;
            sceneName = "";
        }

        /// <summary>
        /// 客户端未准备就绪的事件 (不能接收和发送消息)
        /// </summary>
        /// <param name="event"></param>
        private static void OnClientNotReadyEvent(NotReadyEvent @event)
        {
            ClientManager.isReady = false;
            OnClientNotReady?.Invoke();
        }

        /// <summary>
        /// 当服务器场景发生改变时 (所有客户端也会同步到和服务器一样的场景)
        /// </summary>
        /// <param name="event"></param>
        private void OnClientLoadSceneEvent(SceneEvent @event)
        {
            if (ClientManager.isConnect)
            {
                ClientLoadScene(@event.sceneName);
            }
        }
    }
}