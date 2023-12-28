using System;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        /// <summary>
        /// 客户端加载场景的事件
        /// </summary>
        public static event Action<string> OnClientLoadScene;

        /// <summary>
        /// 服务器加载场景的事件
        /// </summary>
        public static event Action<string> OnServerLoadScene;

        /// <summary>
        /// 客户端加载场景完成的事件
        /// </summary>
        public static event Action<string> OnClientSceneChanged;

        /// <summary>
        /// 服务器加载场景完成的事件
        /// </summary>
        public static event Action<string> OnServerSceneChanged;

        /// <summary>
        /// 当接收Ping
        /// </summary>
        public static event Action<double> OnClientPingUpdate;
        
        /// <summary>
        /// 生成玩家预置体
        /// </summary>
        /// <param name="client"></param>
        private void SpawnPrefab(NetworkClient client)
        {
            if (client.isSpawn && playerPrefab != null)
            {
                Server.Spawn(Instantiate(playerPrefab), client);
                client.isSpawn = false;
            }
        }

        /// <summary>
        /// 客户端 Ping
        /// </summary>
        /// <param name="ping"></param>
        private void ClientPingUpdate(double ping)
        {
            OnClientPingUpdate?.Invoke(ping);
        }

        /// <summary>
        /// 运行初始化
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            Transport.ResetStatic();
            OnClientLoadScene = null;
            OnServerLoadScene = null;
            OnClientSceneChanged = null;
            OnServerSceneChanged = null;
            OnClientPingUpdate = null;
        }
    }
}