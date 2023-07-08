using System;
using JFramework.Udp;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        private static void RegisterMessage(bool isHost)
        {
            if (isHost)
            {
                RegisterMessage<SpawnMessage>(SpawnByHost);
                RegisterMessage<ObjectDestroyMessage>(ObjectDestroyByHost);
                RegisterMessage<ObjectHideMessage>(ObjectHideByHost);
                RegisterMessage<ObjectSpawnStartMessage>(OnEmptyMessageByHost);
                RegisterMessage<ObjectSpawnFinishMessage>(OnEmptyMessageByHost);
                RegisterMessage<PongMessage>(OnEmptyMessageByHost);
            }
            else
            {
                RegisterMessage<SpawnMessage>(SpawnByClient);
                RegisterMessage<ObjectDestroyMessage>(ObjectDestroyByClient);
                RegisterMessage<ObjectHideMessage>(ObjectHideByClient);
                RegisterMessage<ObjectSpawnStartMessage>(ObjectSpawnStartByClient);
                RegisterMessage<ObjectSpawnFinishMessage>(ObjectSpawnFinishByClient);
                RegisterMessage<PongMessage>(PongByClient);
            }

            RegisterMessage<SnapshotMessage>(OnSnapshotMessage);
            RegisterMessage<ChangeOwnerMessage>(OnOwnerChanged);
            RegisterMessage<RpcBufferMessage>(RpcBufferMessage);
        }
        
        /// <summary>
        /// 注册网络消息
        /// </summary>
        public static void RegisterMessage<T>(Action<T> handle, bool isAuthority = true) where T : struct, IEvent
        {
            messages[MessageId<T>.Id] = NetworkUtils.Register(handle, isAuthority);
        }

        private static void ObjectHideByHost(ObjectHideMessage message)
        {
        }

        private static void ObjectDestroyByHost(ObjectDestroyMessage message)
        {
        }

        private static void OnEmptyMessageByHost<T>(T message) where T : IEvent
        {
        }

        private static void SpawnByHost(SpawnMessage message)
        {
        }

        private static void ObjectHideByClient(ObjectHideMessage message)
        {
            Destroy(message.netId);
        }

        private static void ObjectDestroyByClient(ObjectDestroyMessage message)
        {
            Destroy(message.netId);
        }

        private static void Destroy(uint netId)
        {
        }

        private static void SpawnByClient(SpawnMessage message)
        {
        }

        private static void PongByClient(PongMessage message)
        {
            NetworkTime.OnClientPong();
        }

        private static void ObjectSpawnStartByClient(ObjectSpawnStartMessage message)
        {
        }

        private static void ObjectSpawnFinishByClient(ObjectSpawnFinishMessage message)
        {
        }

        private static void RpcBufferMessage(RpcBufferMessage message)
        {
        }

        private static void OnOwnerChanged(ChangeOwnerMessage message)
        {
        }

        private static void OnSnapshotMessage(SnapshotMessage message)
        {
            NetworkSnapshot.OnTimeSnapshot(new TimeSnapshot(connection.timestamp, NetworkTime.localTime));
        }
    }
}