using System;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        private static void RegisterMessage(bool isHost)
        {
            if (isHost)
            {
                NetworkEvent.RegisterMessage<SpawnMessage>(SpawnByHost);
                NetworkEvent.RegisterMessage<ObjectDestroyMessage>(ObjectDestroyByHost);
                NetworkEvent.RegisterMessage<ObjectHideMessage>(ObjectHideByHost);
                NetworkEvent.RegisterMessage<ObjectSpawnStartMessage>(ObjectSpawnStartByHost);
                NetworkEvent.RegisterMessage<ObjectSpawnFinishMessage>(ObjectSpawnFinishByHost);
                NetworkEvent.RegisterMessage<ChangeOwnerMessage>(OnOwnerChanged);
                NetworkEvent.RegisterMessage<RpcBufferMessage>(RpcBufferMessage);
            }
            else
            {
                NetworkEvent.RegisterMessage<SpawnMessage>(SpawnByClient);
                NetworkEvent.RegisterMessage<ObjectDestroyMessage>(ObjectDestroyByClient);
                NetworkEvent.RegisterMessage<ObjectHideMessage>(ObjectHideByClient);
                NetworkEvent.RegisterMessage<ObjectSpawnStartMessage>(ObjectSpawnStartByClient);
                NetworkEvent.RegisterMessage<ObjectSpawnFinishMessage>(ObjectSpawnFinishByClient);
                NetworkEvent.RegisterMessage<ChangeOwnerMessage>(OnOwnerChanged);
                NetworkEvent.RegisterMessage<RpcBufferMessage>(RpcBufferMessage);
            }
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
    }
}