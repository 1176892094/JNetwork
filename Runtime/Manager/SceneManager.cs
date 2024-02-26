using System;
using UnityEngine;

namespace JFramework.Net
{
    public class SceneManager : ScriptableObject
    {
        /// <summary>
        /// 服务器场景
        /// </summary>
        [SerializeField] internal string sceneName;

        /// <summary>
        /// 客户端加载场景的事件
        /// </summary>
        public event Action<string> OnClientChangeScene;

        /// <summary>
        /// 服务器加载场景的事件
        /// </summary>
        public event Action<string> OnServerChangeScene;

        /// <summary>
        /// 客户端加载场景完成的事件
        /// </summary>
        public event Action<string> OnClientSceneChanged;

        /// <summary>
        /// 服务器加载场景完成的事件
        /// </summary>
        public event Action<string> OnServerSceneChanged;

        /// <summary>
        /// 服务器加载场景
        /// </summary>
        public async void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("服务器不能加载空场景！");
                return;
            }

            if (NetworkManager.Server.isLoadScene && this.sceneName == sceneName)
            {
                Debug.LogError($"服务器已经在加载 {sceneName} 场景");
                return;
            }

            foreach (var client in NetworkManager.Server.clients.Values)
            {
                NetworkManager.Server.SetReady(client, false);
            }

            OnServerChangeScene?.Invoke(sceneName);
            if (!NetworkManager.Server.isActive) return;
            this.sceneName = sceneName;
            NetworkManager.Server.isLoadScene = true;
            
            using var writer = NetworkWriter.Pop();
            NetworkMessage.WriteMessage(writer, new SceneMessage(sceneName));
            foreach (var client in NetworkManager.Server.clients.Values)
            {
                client.Send(writer.ToArraySegment());
            }

            await GlobalManager.Scene.Load(sceneName);
            OnLoadComplete();
        }

        /// <summary>
        /// 客户端加载场景
        /// </summary>
        internal async void ClientLoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("客户端不能加载空场景！");
                return;
            }

            OnClientChangeScene?.Invoke(sceneName);

            if (NetworkManager.Server.isActive) return; //主机不做处理
            this.sceneName = sceneName;
            NetworkManager.Client.isLoadScene = true;
            await GlobalManager.Scene.Load(sceneName);
            OnLoadComplete();
        }


        /// <summary>
        /// 场景加载完成
        /// </summary>
        private void OnLoadComplete()
        {
            switch (NetworkManager.Instance.mode)
            {
                case NetworkMode.Host:
                    OnServerSceneLoadCompleted();
                    OnClientSceneLoadCompleted();
                    break;
                case NetworkMode.Server:
                    OnServerSceneLoadCompleted();
                    break;
                case NetworkMode.Client:
                    OnClientSceneLoadCompleted();
                    break;
            }
        }

        /// <summary>
        /// 服务器端场景加载完成
        /// </summary>
        private void OnServerSceneLoadCompleted()
        {
            NetworkManager.Server.isLoadScene = false;
            NetworkManager.Server.SpawnObjects();
            OnServerSceneChanged?.Invoke(sceneName);
        }

        /// <summary>
        /// 客户端场景加载完成
        /// </summary>
        private void OnClientSceneLoadCompleted()
        {
            NetworkManager.Client.isLoadScene = false;
            if (!NetworkManager.Client.isAuthority) return;
            if (!NetworkManager.Client.isReady)
            {
                NetworkManager.Client.Ready();
            }

            OnClientSceneChanged?.Invoke(GlobalManager.Scene.ToString());
        }

        /// <summary>
        /// 当对象被销毁
        /// </summary>
        internal void Reset()
        {
            sceneName = "";
            OnClientChangeScene = null;
            OnServerChangeScene = null;
            OnClientSceneChanged = null;
            OnServerSceneChanged = null;
        }
    }
}