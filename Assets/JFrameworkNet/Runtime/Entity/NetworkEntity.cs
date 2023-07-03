using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public class NetworkEntity : MonoBehaviour
    {
        private readonly Dictionary<int, ClientConnection> observers = new Dictionary<int, ClientConnection>();
        public uint netId;
        public uint sceneId;
        public bool isOwner;
        public bool isClient;
        public bool isServer;
        public ClientConnection client;
        public NetworkBehaviour[] objects;

        private static uint objectId;
        internal static uint ObjectId => objectId++;


        internal void AddObserver(ClientConnection client)
        {
            if (!observers.ContainsKey(client.clientId))
            {
                if (observers.Count == 0)
                {
                    foreach (var obj in objects)
                    {
                        obj.ClearAllDirtyBits();
                    }
                }

                observers[client.clientId] = client;
               // client.AddObserver(this);
            }
        }

        public virtual void OnStartServer()
        {
        }
    }
}