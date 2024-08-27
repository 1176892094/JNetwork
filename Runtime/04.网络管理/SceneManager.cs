// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  01:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using UnityEngine;
using JFramework;
using JFramework.Event;
using GlobalSceneManager = JFramework.SceneManager;

namespace JFramework.Net
{
    public class SceneManager : Component<NetworkManager>
    {
        /// <summary>
        /// 服务器场景
        /// </summary>
        private string sceneName;

        /// <summary>
        /// 服务器加载场景
        /// </summary>
        public void Load(string sceneName)
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
                client.isReady = false;
                client.Send(new ReadyMessage(false));
            }

            if (NetworkManager.Server.isActive)
            {
                this.sceneName = sceneName;
                NetworkManager.Server.isLoadScene = true;
                foreach (var client in NetworkManager.Server.clients.Values)
                {
                    client.Send(new SceneMessage(sceneName));
                }

                GlobalSceneManager.Load(sceneName, OnLoadComplete);
            }

            EventManager.Invoke(new OnServerChangeScene(sceneName));
        }

        /// <summary>
        /// 客户端加载场景
        /// </summary>
        internal void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("客户端不能加载空场景！");
                return;
            }

            if (!NetworkManager.Server.isActive)
            {
                this.sceneName = sceneName;
                NetworkManager.Client.isLoadScene = true;
                GlobalSceneManager.Load(sceneName, OnLoadComplete);
            }

            EventManager.Invoke(new OnClientChangeScene(sceneName));
        }

        /// <summary>
        /// 场景加载完成
        /// </summary>
        private void OnLoadComplete()
        {
            switch (NetworkManager.Mode)
            {
                case EntryMode.Host:
                    OnServerComplete();
                    OnClientComplete();
                    break;
                case EntryMode.Server:
                    OnServerComplete();
                    break;
                case EntryMode.Client:
                    OnClientComplete();
                    break;
            }
        }

        /// <summary>
        /// 服务器端场景加载完成
        /// </summary>
        private void OnServerComplete()
        {
            NetworkManager.Server.isLoadScene = false;
            NetworkManager.Server.SpawnObjects();
            EventManager.Invoke(new OnServerSceneChanged(sceneName));
        }

        /// <summary>
        /// 客户端场景加载完成
        /// </summary>
        private void OnClientComplete()
        {
            NetworkManager.Client.isLoadScene = false;
            if (NetworkManager.Client.isConnected)
            {
                if (!NetworkManager.Client.isReady)
                {
                    NetworkManager.Client.Ready();
                }

                EventManager.Invoke(new OnClientSceneChanged(GlobalSceneManager.name));
            }
        }
    }
}