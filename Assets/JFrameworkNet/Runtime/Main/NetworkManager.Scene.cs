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
                Debug.LogError("Server load scene empty scene name");
                return;
            }

            if (NetworkServer.isLoadScene && newSceneName == sceneName)
            {
                Debug.LogError($"Scene change is already in progress for {newSceneName}");
                return;
            }

            NetworkServer.SetClientNotReadyAll();
            OnServerLoadScene?.Invoke(newSceneName);
            sceneName = newSceneName;
            NetworkServer.isLoadScene = true;
            if (NetworkServer.isActive)
            {
                NetworkServer.Send(new SceneMessage
                {
                    sceneName = newSceneName
                });
            }

            await LoadSceneAsync(newSceneName);
        }
        
        /// <summary>
        /// 客户端加载场景
        /// </summary>
        internal async void ClientLoadScene(string newSceneName)
        {
            if (string.IsNullOrWhiteSpace(newSceneName))
            {
                Debug.LogError("Client load scene empty scene name");
                return;
            }

            OnClientLoadScene?.Invoke(newSceneName);
            if (NetworkServer.isActive) return; //Host不做处理
            sceneName = newSceneName;
            NetworkClient.isLoadScene = true;
            await LoadSceneAsync(newSceneName);
        }
        
        /// <summary>
        /// 异步加载场景
        /// </summary>
        private async Task LoadSceneAsync(string newSceneName)
        {
            await SceneManager.LoadSceneAsync(newSceneName);
            NetworkServer.isLoadScene = false;
            NetworkClient.isLoadScene = false;
            switch (networkMode)
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
            NetworkServer.SpawnObjects();
            OnServerSceneChanged?.Invoke(sceneName);
        }

        /// <summary>
        /// 客户端场景加载完成
        /// </summary>
        private void OnClientSceneLoadCompleted()
        {
            if (!NetworkClient.isConnect) return;
            if (NetworkClient.connection.isAuthority && !NetworkClient.isReady)
            {
                NetworkClient.Ready();
            }

            OnClientSceneChanged?.Invoke(SceneManager.localScene);
        }
    }
}