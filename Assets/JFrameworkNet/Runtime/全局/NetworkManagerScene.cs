using System.Threading.Tasks;
using JFramework.Core;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        /// <summary>
        /// 服务器加载场景
        /// </summary>
        public async void ServerLoadScene(string newSceneName)
        {
            if (string.IsNullOrWhiteSpace(newSceneName))
            {
                Debug.LogError("服务器不能加载空场景！");
                return;
            }

            if (ServerManager.isLoadScene && newSceneName == serverScene)
            {
                Debug.LogError($"服务器已经在加载 {newSceneName} 场景");
                return;
            }

            Debug.Log("服务器开始加载场景");
            ServerManager.SetClientNotReadyAll();
            OnServerLoadScene?.Invoke(newSceneName);
            serverScene = newSceneName;
            ServerManager.isLoadScene = true;
            if (ServerManager.isActive)
            {
                ServerManager.SendToAll(new SceneEvent
                {
                    sceneName = newSceneName
                });
            }

            await LoadSceneAsync(newSceneName);
        }
        
        /// <summary>
        /// 客户端加载场景
        /// </summary>
        private async void ClientLoadScene(string newSceneName)
        {
            if (string.IsNullOrWhiteSpace(newSceneName))
            {
                Debug.LogError("客户端不能加载空场景！");
                return;
            }
            Debug.Log("客户端器开始加载场景");
            OnClientLoadScene?.Invoke(newSceneName);
            if (ServerManager.isActive) return; //Host不做处理
            serverScene = newSceneName;
            ClientManager.isLoadScene = true;
            await LoadSceneAsync(newSceneName);
        }
        
        /// <summary>
        /// 异步加载场景
        /// </summary>
        private async Task LoadSceneAsync(string newSceneName)
        {
            await SceneManager.LoadSceneAsync(newSceneName);
            ServerManager.isLoadScene = false;
            ClientManager.isLoadScene = false;
            switch (mode)
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
            Debug.Log("服务器加载场景完成");
            ServerManager.SpawnObjects();
            OnServerSceneChanged?.Invoke(serverScene);
        }

        /// <summary>
        /// 客户端场景加载完成
        /// </summary>
        private void OnClientSceneLoadCompleted()
        {
            if (!ClientManager.isAuthority) return;
            Debug.Log("客户端加载场景完成");
            if (ClientManager.connection.isAuthority && !ClientManager.isReady)
            {
                ClientManager.Ready();
            }

            OnClientSceneChanged?.Invoke(SceneManager.scene);
        }
    }
}