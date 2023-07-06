namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        private void RegisterServerEvent()
        {
            NetworkServer.OnConnected = OnServerConnectInternal;
            NetworkServer.OnDisconnected = OnServerDisconnectInternal;
            NetworkEvent.RegisterMessage<ReadyMessage>(OnServerReadyInternal);
        }

        private void RegisterClientEvent()
        {
            NetworkClient.OnConnected = OnClientConnectInternal;
            NetworkClient.OnDisconnected = OnClientDisconnectInternal;
            NetworkEvent.RegisterMessage<NotReadyMessage>(OnClientNotReadyInternal);
            NetworkEvent.RegisterMessage<SceneMessage>(OnClientLoadSceneInternal, false);
        }

        private void OnServerConnectInternal(ClientConnection client)
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

        private void OnServerDisconnectInternal(ClientConnection client)
        {
            OnServerDisconnect?.Invoke(client);
        }

        private static void OnServerReadyInternal(ClientConnection client, ReadyMessage message)
        {
            NetworkServer.SetClientReady(client);
            OnServerReady?.Invoke(client);
        }

        private void OnClientConnectInternal()
        {
            NetworkClient.server.isAuthority = true;
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
            NetworkClient.RuntimeInitializeOnLoad();
            if (networkMode == NetworkMode.Server) return;
            sceneName = "";
        }

        private static void OnClientNotReadyInternal(NotReadyMessage message)
        {
            NetworkClient.isReady = false;
            OnClientNotReady?.Invoke();
        }

        private static void OnClientLoadSceneInternal(SceneMessage message)
        {
            if (NetworkClient.isConnect)
            {
                //ClientLoadScene(message.sceneName);
            }
        }
    }
}