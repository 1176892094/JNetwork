using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        public class NetworkServer
        {
            private Dictionary<uint, NetworkEntity> spawnDict = new Dictionary<uint, NetworkEntity>();
            private Dictionary<int, ClientConnection> clientDict = new Dictionary<int, ClientConnection>();
            public int tickRate;
            public bool isActive;
            private bool initialized;
            private int maxConnection;
            private ClientConnection client;
            public Action<ClientConnection> OnConnected;
            public Action<ClientConnection> OnDisconnected;

            public NetworkServer(int maxConnection, bool isListen)
            {
                this.maxConnection = maxConnection;
                if (isListen)
                {
                    Transport.Instance.ServerStart();
                }

                isActive = true;
            }

            private void StartServer()
            {
                if (Transport.Instance == null)
                {
                    Debug.LogError("There was no active Transport!");
                    return;
                }

                if (initialized) return;
                clientDict.Clear();
            }

            public void Spawn(GameObject obj, NetworkConnection connection = null)
            {
                if (!isActive)
                {
                    Debug.LogError($"SpawnObject for {obj}, NetworkServer is not active", obj);
                    return;
                }

                if (!obj.TryGetComponent(out NetworkEntity entity))
                {
                    Debug.LogError($"SpawnObject {obj} has no NetworkIdentity.", obj);
                    return;
                }

                if (spawnDict.ContainsKey(entity.netId))
                {
                    Debug.LogWarning($"{entity} with netId = {entity.netId} was already spawned.", entity.gameObject);
                    return;
                }

                entity.client = (ClientConnection)connection;

                if (entity.client is { isLocal: true })
                {
                    entity.isOwner = true;
                }
            
                if (!entity.isServer && entity.netId == 0)
                {
                    entity.isClient = Client.isActive;
                    entity.isServer = true;
                    entity.netId = NetworkEntity.ObjectId;
                    spawnDict[entity.netId] = entity;
                    entity.OnStartServer();
                }

                AddReadToObservers(entity);
            }

            private void AddReadToObservers(NetworkEntity entity)
            {
                foreach (var connection in clientDict.Values.Where(client => client.isReady))
                {
                    entity.AddObserver(connection);
                }
          
                if (client is { isReady: true })
                {
                    entity.AddObserver(client);
                }
            }
            
            public void SpawnObjects()
            {
                if (!isActive) return;
                NetworkEntity[] entities = Resources.FindObjectsOfTypeAll<NetworkEntity>();
                foreach (var entity in entities)
                {
                    if (NetworkUtils.IsSceneObject(entity) && entity.netId == 0)
                    {
                        entity.gameObject.SetActive(true);
                        if (NetworkUtils.IsValidParent(entity))
                        {
                            Spawn(entity.gameObject, entity.client);
                        }
                    }
                }
            }


            public void EarlyUpdate()
            {
            }

            public void AfterUpdate()
            {
            }
        }
    }
}