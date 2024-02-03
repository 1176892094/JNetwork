using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        public class SceneManager : Component<NetworkManager>
        {
            /// <summary>
            /// 服务器场景
            /// </summary>
            [SerializeField] internal string sceneName;

            /// <summary>
            /// 服务器加载场景
            /// </summary>
            public async void LoadScene(string newSceneName)
            {
                if (string.IsNullOrWhiteSpace(newSceneName))
                {
                    Debug.LogError("服务器不能加载空场景！");
                    return;
                }

                if (Server.isLoadScene && newSceneName == sceneName)
                {
                    Debug.LogError($"服务器已经在加载 {newSceneName} 场景");
                    return;
                }

                foreach (var client in Server.clients.Values)
                {
                    Server.SetReady(client, false);
                }

                OnServerLoadScene?.Invoke(newSceneName);
                sceneName = newSceneName;
                Server.isLoadScene = true;
                if (Server.isActive)
                {
                    using var writer = NetworkWriter.Pop();
                    NetworkMessage.WriteMessage(writer, new SceneMessage(newSceneName));
                    foreach (var client in Server.clients.Values)
                    {
                        client.Send(writer.ToArraySegment());
                    }
                }

                await GlobalManager.Scene.LoadScene(newSceneName);
                OnLoadComplete();
            }

            /// <summary>
            /// 客户端加载场景
            /// </summary>
            internal async void ClientLoadScene(string newSceneName)
            {
                if (string.IsNullOrWhiteSpace(newSceneName))
                {
                    Debug.LogError("客户端不能加载空场景！");
                    return;
                }

                Debug.Log("客户端器开始加载场景");
                OnClientLoadScene?.Invoke(newSceneName);
                if (Server.isActive) return; //Host不做处理
                sceneName = newSceneName;
                Client.isLoadScene = true;
                await GlobalManager.Scene.LoadScene(newSceneName);
                OnLoadComplete();
            }


            /// <summary>
            /// 场景加载完成
            /// </summary>
            private void OnLoadComplete()
            {
                switch (Instance.mode)
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
                Server.isLoadScene = false;
                Server.SpawnObjects();
                OnServerSceneChanged?.Invoke(sceneName);
            }

            /// <summary>
            /// 客户端场景加载完成
            /// </summary>
            private void OnClientSceneLoadCompleted()
            {
                Client.isLoadScene = false;
                if (!Client.isAuthority) return;
                Debug.Log("客户端加载场景完成");
                if (!Client.isReady)
                {
                    Client.Ready();
                }

                OnClientSceneChanged?.Invoke(GlobalManager.Scene.ToString());
            }
        }
    }
}