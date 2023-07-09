using System;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        private static void RegisterEvent(bool isHost)
        {
            if (isHost)
            {
                RegisterEvent<SpawnMessage>(SpawnByHost);
                RegisterEvent<ObjectDestroyMessage>(ObjectDestroyByHost);
                RegisterEvent<ObjectHideMessage>(ObjectHideByHost);
                RegisterEvent<ObjectSpawnStartMessage>(OnEmptyMessageByHost);
                RegisterEvent<ObjectSpawnFinishMessage>(OnEmptyMessageByHost);
                RegisterEvent<PongMessage>(OnEmptyMessageByHost);
            }
            else
            {
                RegisterEvent<SpawnMessage>(SpawnByClient);
                RegisterEvent<ObjectDestroyMessage>(ObjectDestroyByClient);
                RegisterEvent<ObjectHideMessage>(ObjectHideByClient);
                RegisterEvent<ObjectSpawnStartMessage>(ObjectSpawnStartByClient);
                RegisterEvent<ObjectSpawnFinishMessage>(ObjectSpawnFinishByClient);
                RegisterEvent<PongMessage>(PongByClient);
            }

            RegisterEvent<SnapshotMessage>(OnSnapshotMessage);
            RegisterEvent<ChangeOwnerMessage>(OnOwnerChanged);
            RegisterEvent<RpcBufferMessage>(RpcBufferMessage);
        }
        
        /// <summary>
        /// 注册网络消息
        /// </summary>
        public static void RegisterEvent<T>(Action<T> handle, bool isAuthority = true) where T : struct, IEvent
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