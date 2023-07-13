using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        /// <summary>
        /// 当客户端连接到服务器时，向连接的客户端发送改变场景的事件
        /// </summary>
        /// <param name="client">连接的客户端</param>
        internal static void OnServerConnectEvent(ClientEntity client)
        {
            client.isAuthority = true;
            if (!string.IsNullOrEmpty(serverScene))
            {
                var message = new SceneEvent()
                {
                    sceneName = serverScene
                };
                client.Send(message);
            }

            OnServerConnect?.Invoke(client);
        }

        /// <summary>
        /// 当客户端从服务器断开时触发
        /// </summary>
        /// <param name="client">断开的客户端</param>
        internal static void OnServerDisconnectEvent(ClientEntity client)
        {
            OnServerDisconnect?.Invoke(client);
        }

        /// <summary>
        /// 当客户端在服务器准备就绪，向客户端发送生成物体的消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="event"></param>
        internal static void OnServerReadyEvent(ClientEntity client, ReadyEvent @event)
        {
            ServerManager.SetClientReady(client);
            OnServerReady?.Invoke(client);
        }

        /// <summary>
        /// 客户端连接的事件
        /// </summary>
        internal static void OnClientConnectEvent()
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
        internal static void OnClientDisconnectEvent()
        {
            OnClientDisconnect?.Invoke();
        }

        /// <summary>
        /// 客户端未准备就绪的事件 (不能接收和发送消息)
        /// </summary>
        /// <param name="event"></param>
        internal static void OnClientNotReadyEvent(NotReadyEvent @event)
        {
            ClientManager.isReady = false;
            OnClientNotReady?.Invoke();
        }

        /// <summary>
        /// 当服务器场景发生改变时 (所有客户端也会同步到和服务器一样的场景)
        /// </summary>
        /// <param name="event"></param>
        internal static void OnClientLoadSceneEvent(SceneEvent @event)
        {
            if (ClientManager.isAuthority)
            {
                ClientLoadScene(@event.sceneName);
            }
        }
    }
}