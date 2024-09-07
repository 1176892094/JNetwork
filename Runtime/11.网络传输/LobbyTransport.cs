// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-08-27  16:08
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace JFramework.Net
{
    [DefaultExecutionOrder(1001)]
    public partial class LobbyTransport : Transport
    {
        public static LobbyTransport Instance;
        public Transport transport;
        public bool isPublic = true;
        public string roomName;
        public string roomData;
        public string serverId;
        public string serverKey = "Secret Key";

        private int playerId;
        private bool isClient;
        private bool isServer;
        private StateMode state = StateMode.Disconnect;
        private readonly Dictionary<int, int> clients = new Dictionary<int, int>();
        private readonly Dictionary<int, int> players = new Dictionary<int, int>();
        public event Action OnDisconnect;
        public event Action<List<Room>> OnLobbyUpdate;

        private void Awake()
        {
            Instance = this;
            transport.OnClientConnect -= OnClientConnect;
            transport.OnClientDisconnect -= OnClientDisconnect;
            transport.OnClientReceive -= OnClientReceive;
            transport.OnClientConnect += OnClientConnect;
            transport.OnClientDisconnect += OnClientDisconnect;
            transport.OnClientReceive += OnClientReceive;

            void OnClientConnect()
            {
                state = StateMode.Connect;
            }

            void OnClientDisconnect()
            {
                StopLobby();
            }

            void OnClientReceive(ArraySegment<byte> segment, int channel)
            {
                try
                {
                    OnMessageReceive(segment, channel);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }

        private void OnDestroy()
        {
            StopLobby();
        }

        public void StartLobby()
        {
            if (state != StateMode.Disconnect)
            {
                Debug.LogWarning("大厅服务器已经连接！");
                return;
            }

            transport.port = port;
            transport.address = address;
            transport.StartClient();
        }

        public void StopLobby()
        {
            if (state != StateMode.Disconnect)
            {
                Debug.Log("停止大厅服务器。");
                state = StateMode.Disconnect;
                clients.Clear();
                players.Clear();
                isServer = false;
                isClient = false;
                OnDisconnect?.Invoke();
                transport.StopClient();
            }
        }

        private void OnMessageReceive(ArraySegment<byte> segment, int channel)
        {
            using var reader = NetworkReader.Pop(segment);
            var opcode = (OpCodes)reader.ReadByte();
            if (opcode == OpCodes.Connect)
            {
                using var writer = NetworkWriter.Pop();
                writer.WriteByte((byte)OpCodes.Connected);
                writer.WriteString(serverKey);
                transport.SendToServer(writer);
            }
            else if (opcode == OpCodes.Connected)
            {
                state = StateMode.Connected;
                UpdateRoom();
            }
            else if (opcode == OpCodes.CreateRoom)
            {
                serverId = reader.ReadString();
            }
            else if (opcode == OpCodes.JoinRoom)
            {
                if (isServer)
                {
                    playerId++;
                    var clientId = reader.ReadInt();
                    clients.Add(clientId, playerId);
                    players.Add(playerId, clientId);
                    OnServerConnect?.Invoke(playerId);
                }

                if (isClient)
                {
                    OnClientConnect?.Invoke();
                }
            }
            else if (opcode == OpCodes.LeaveRoom)
            {
                if (isClient)
                {
                    isClient = false;
                    OnClientDisconnect?.Invoke();
                }
            }
            else if (opcode == OpCodes.UpdateData)
            {
                var message = reader.ReadArraySegment();
                if (isServer)
                {
                    var clientId = reader.ReadInt();
                    if (clients.TryGetValue(clientId, out var ownerId))
                    {
                        OnServerReceive?.Invoke(ownerId, message, channel);
                    }
                }

                if (isClient)
                {
                    OnClientReceive?.Invoke(message, channel);
                }
            }
            else if (opcode == OpCodes.KickRoom)
            {
                if (isServer)
                {
                    int clientId = reader.ReadInt();
                    if (clients.TryGetValue(clientId, out var ownerId))
                    {
                        OnServerDisconnect?.Invoke(ownerId);
                        clients.Remove(clientId);
                        players.Remove(ownerId);
                    }
                }
            }
        }

        public async void UpdateRoom()
        {
            if (state != StateMode.Connected)
            {
                Debug.Log("您必须连接到大厅以请求房间列表!");
                return;
            }

            var uri = $"http://{address}:{port}/api/compressed/servers";
            using var request = UnityWebRequest.Get(uri);
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("无法获取服务器列表。" + $"{address}:{port}");
                return;
            }

            var rooms = await Compression.DecompressAsync(request.downloadHandler.text);
            OnLobbyUpdate?.Invoke(JsonManager.Read<List<Room>>("{" + "\"value\":" + rooms + "}"));
            Debug.Log("房间信息：" + rooms);
        }

        public void UpdateRoom(string roomName, string roomData, bool isPublic, int maxCount)
        {
            if (isServer)
            {
                using var writer = NetworkWriter.Pop();
                writer.WriteByte((byte)OpCodes.UpdateRoom);
                writer.WriteString(roomName);
                writer.WriteString(roomData);
                writer.WriteBool(isPublic);
                writer.WriteInt(maxCount);
                transport.SendToServer(writer);
            }
        }
    }

    public partial class LobbyTransport
    {
        public override int MessageSize(int channel)
        {
            return transport.MessageSize(channel);
        }

        public override void SendToClient(int clientId, ArraySegment<byte> segment, int channel = Channel.Reliable)
        {
            if (players.TryGetValue(clientId, out var ownerId))
            {
                using var writer = NetworkWriter.Pop();
                writer.WriteByte((byte)OpCodes.UpdateData);
                writer.WriteArraySegment(segment);
                writer.WriteInt(ownerId);
                transport.SendToServer(writer);
            }
        }

        public override void SendToServer(ArraySegment<byte> segment, int channel = Channel.Reliable)
        {
            using var writer = NetworkWriter.Pop();
            writer.WriteByte((byte)OpCodes.UpdateData);
            writer.WriteArraySegment(segment);
            writer.WriteInt(0);
            transport.SendToServer(writer);
        }

        public override void StartServer()
        {
            if (state != StateMode.Connected)
            {
                Debug.Log("没有连接到大厅!");
                return;
            }

            if (isClient || isServer)
            {
                Debug.Log("客户端或服务器已经连接!");
                return;
            }

            playerId = 0;
            clients.Clear();
            players.Clear();
            isServer = true;

            using var writer = NetworkWriter.Pop();
            writer.WriteByte((byte)OpCodes.CreateRoom);
            writer.WriteString(roomName);
            writer.WriteString(roomData);
            writer.WriteInt(NetworkManager.Instance.connection);
            writer.WriteBool(isPublic);
            transport.SendToServer(writer);
        }

        public override void StopServer()
        {
            if (isServer)
            {
                isServer = false;
                using var writer = NetworkWriter.Pop();
                writer.WriteByte((byte)OpCodes.LeaveRoom);
                transport.SendToServer(writer);
            }
        }

        public override void StopClient(int clientId)
        {
            if (players.TryGetValue(clientId, out var ownerId))
            {
                using var writer = NetworkWriter.Pop();
                writer.WriteByte((byte)OpCodes.KickRoom);
                writer.WriteInt(ownerId);
                transport.SendToServer(writer);
            }
        }

        public override void StartClient()
        {
            if (state != StateMode.Connected)
            {
                Debug.Log("没有连接到大厅！");
                OnClientDisconnect?.Invoke();
                return;
            }

            if (isClient || isServer)
            {
                Debug.Log("客户端或服务器已经连接!");
                return;
            }

            isClient = true;
            using var writer = NetworkWriter.Pop();
            writer.WriteByte((byte)OpCodes.JoinRoom);
            writer.WriteString(transport.address);
            transport.SendToServer(writer);
        }

        public override void StartClient(Uri uri)
        {
            if (uri != null)
            {
                address = uri.Host;
            }

            StartClient();
        }

        public override void StopClient()
        {
            if (state != StateMode.Disconnect)
            {
                isClient = false;
                using var writer = NetworkWriter.Pop();
                writer.WriteByte((byte)OpCodes.LeaveRoom);
                transport.SendToServer(writer);
            }
        }

        public override void ClientEarlyUpdate()
        {
            transport.ClientEarlyUpdate();
        }

        public override void ClientAfterUpdate()
        {
            transport.ClientAfterUpdate();
        }

        public override void ServerEarlyUpdate()
        {
        }

        public override void ServerAfterUpdate()
        {
        }
    }

    public partial class LobbyTransport
    {
        [Serializable]
        public struct Room
        {
            public string roomId;
            public string roomName;
            public string roomData;
            public int maxCount;
            public int clientId;
            public bool isPublic;
            public List<int> clients;
        }

        private enum StateMode : byte
        {
            Connect = 0,
            Connected = 1,
            Disconnect = 2,
        }

        private enum OpCodes : byte
        {
            Connect = 1,
            Connected = 2,
            JoinRoom = 3,
            CreateRoom = 4,
            UpdateRoom = 5,
            LeaveRoom = 6,
            UpdateData = 7,
            KickRoom = 8,
        }
    }
}