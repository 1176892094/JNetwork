using System;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        private static void RegisterHostMessage()
        {
            RegisterMessage<SpawnMessage>(SpawnByHost);
            RegisterMessage<ObjectDestroyMessage>(ObjectDestroyByHost);
            RegisterMessage<ObjectHideMessage>(ObjectHideByHost);
            RegisterMessage<ObjectSpawnStartMessage>(ObjectSpawnStartByHost);
            RegisterMessage<ObjectSpawnFinishMessage>(ObjectSpawnFinishByHost);
            RegisterMessage<ChangeOwnerMessage>(OnOwnerChanged);
            RegisterMessage<RpcBufferMessage>(RpcBufferMessage);
        }

        private static void RegisterMessageHandlers()
        {
            RegisterMessage<SpawnMessage>(SpawnByClient);
            RegisterMessage<ObjectDestroyMessage>(ObjectDestroyByClient);
            RegisterMessage<ObjectHideMessage>(ObjectHideByClient);
            RegisterMessage<ObjectSpawnStartMessage>(ObjectSpawnStartByClient);
            RegisterMessage<ObjectSpawnFinishMessage>(ObjectSpawnFinishByClient);
            RegisterMessage<ChangeOwnerMessage>(OnOwnerChanged);
            RegisterMessage<RpcBufferMessage>(RpcBufferMessage);
        }

        private static void ObjectHideByHost(ObjectHideMessage message)
        {
        }

        private static void ObjectDestroyByHost(ObjectDestroyMessage message)
        {
        }

        private static void ObjectSpawnStartByHost(ObjectSpawnStartMessage message)
        {
        }

        private static void ObjectSpawnFinishByHost(ObjectSpawnFinishMessage message)
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

        internal static void RegisterMessage<T>(Action<T> handle, bool isAuthority = true) where T : struct, NetworkMessage
        {
            messages[MessageId<T>.Id] = NetworkUtils.Register(handle, isAuthority);
        }
    }
}