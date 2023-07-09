using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        private void RegisterServerEvent()
        {
            NetworkServer.OnConnected = OnServerConnectEvent;
            NetworkServer.OnDisconnected = OnServerDisconnectEvent;
            NetworkServer.RegisterEvent<ReadyEvent>(OnServerReadyEvent);
            Debug.Log("NetworkManager --> RegisterServerEvent");
        }

        private void RegisterClientEvent()
        {
            NetworkClient.OnConnected = OnClientConnectEvent;
            NetworkClient.OnDisconnected = OnClientDisconnectEvent;
            NetworkClient.RegisterEvent<NotReadyEvent>(OnClientNotReadyEvent);
            NetworkClient.RegisterEvent<SceneEvent>(OnClientLoadSceneEvent, false);
            Debug.Log("NetworkManager --> RegisterClientEvent");
        }

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

        private void OnServerDisconnectEvent(ClientEntity client)
        {
            OnServerDisconnect?.Invoke(client);
        }

        private static void OnServerReadyEvent(ClientEntity client, ReadyEvent @event)
        {
            NetworkServer.SetClientReady(client);
            OnServerReady?.Invoke(client);
        }

        private void OnClientConnectEvent()
        {
            NetworkClient.connection.isAuthority = true;
            Debug.Log("NetworkManager --> SetAuthority");
            if (!NetworkClient.isReady)
            {
                NetworkClient.Ready();
            }

            OnClientConnect?.Invoke();
        }

        private void OnClientDisconnectEvent()
        {
            if (networkMode is NetworkMode.Server or NetworkMode.None) return;
            networkMode = networkMode == NetworkMode.Host ? NetworkMode.Server : NetworkMode.None;
            OnClientDisconnect?.Invoke();
            OnStopClient?.Invoke();
            NetworkClient.StopClient();
            if (networkMode == NetworkMode.Server) return;
            sceneName = "";
        }

        private static void OnClientNotReadyEvent(NotReadyEvent @event)
        {
            NetworkClient.isReady = false;
            OnClientNotReady?.Invoke();
        }

        private void OnClientLoadSceneEvent(SceneEvent @event)
        {
            if (NetworkClient.isConnect)
            {
                ClientLoadScene(@event.sceneName);
            }
        }
    }
}