using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        private void RegisterServerEvent()
        {
            NetworkServer.OnConnected = OnServerConnectInternal;
            NetworkServer.OnDisconnected = OnServerDisconnectInternal;
            NetworkServer.RegisterMessage<ReadyMessage>(OnServerReadyInternal);
            Debug.Log("NetworkManager --> RegisterServerEvent");
        }

        private void RegisterClientEvent()
        {
            NetworkClient.OnConnected = OnClientConnectInternal;
            NetworkClient.OnDisconnected = OnClientDisconnectInternal;
            NetworkClient.RegisterMessage<NotReadyMessage>(OnClientNotReadyInternal);
            NetworkClient.RegisterMessage<SceneMessage>(OnClientLoadSceneInternal, false);
            Debug.Log("NetworkManager --> RegisterClientEvent");
        }

        private void OnServerConnectInternal(ClientEntity client)
        {
            client.isAuthority = true;
            if (!string.IsNullOrEmpty(sceneName))
            {
                var message = new SceneMessage()
                {
                    sceneName = sceneName
                };
                client.Send(message);
            }

            OnServerConnect?.Invoke(client);
        }

        private void OnServerDisconnectInternal(ClientEntity client)
        {
            OnServerDisconnect?.Invoke(client);
        }

        private static void OnServerReadyInternal(ClientEntity client, ReadyMessage message)
        {
            NetworkServer.SetClientReady(client);
            OnServerReady?.Invoke(client);
        }

        private void OnClientConnectInternal()
        {
            NetworkClient.connection.isAuthority = true;
            Debug.Log("NetworkManager --> SetAuthority");
            if (!NetworkClient.isReady)
            {
                NetworkClient.Ready();
            }

            OnClientConnect?.Invoke();
        }

        private void OnClientDisconnectInternal()
        {
            if (networkMode is NetworkMode.Server or NetworkMode.None) return;
            networkMode = networkMode == NetworkMode.Host ? NetworkMode.Server : NetworkMode.None;
            OnClientDisconnect?.Invoke();
            OnStopClient?.Invoke();
            NetworkClient.StopClient();
            if (networkMode == NetworkMode.Server) return;
            sceneName = "";
        }

        private static void OnClientNotReadyInternal(NotReadyMessage message)
        {
            NetworkClient.isReady = false;
            OnClientNotReady?.Invoke();
        }

        private void OnClientLoadSceneInternal(SceneMessage message)
        {
            if (NetworkClient.isConnect)
            {
                ClientLoadScene(message.sceneName);
            }
        }
    }
}