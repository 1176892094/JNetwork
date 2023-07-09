using System;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        /// <summary>
        /// 注册网络事件
        /// </summary>
        /// <param name="isHost">是否是基于主机的连接</param>
        private static void RegisterEvent(bool isHost)
        {
            if (isHost)
            {
                RegisterEvent<SpawnMessage>(SpawnByHost);
                RegisterEvent<ObjectDestroyMessage>(ObjectDestroyByHost);
                RegisterEvent<ObjectHideMessage>(OnEmptyMessageByHost);
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

        /// <summary>
        /// 主机模式下销毁游戏对象
        /// </summary>
        /// <param name="message"></param>
        private static void ObjectDestroyByHost(ObjectDestroyMessage message)
        {
        }

        /// <summary>
        /// 主机模式下空的消息事件
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        private static void OnEmptyMessageByHost<T>(T message) where T : IEvent
        {
        }

        /// <summary>
        /// 主机模式下生成物体的事件
        /// </summary>
        /// <param name="message"></param>
        private static void SpawnByHost(SpawnMessage message)
        {
        }

        /// <summary>
        /// 客户端下隐藏物体的事件
        /// </summary>
        /// <param name="message"></param>
        private static void ObjectHideByClient(ObjectHideMessage message)
        {
            Destroy(message.netId);
        }

        /// <summary>
        /// 客户端下销毁物体的事件
        /// </summary>
        /// <param name="message"></param>
        private static void ObjectDestroyByClient(ObjectDestroyMessage message)
        {
            Destroy(message.netId);
        }

        /// <summary>
        /// 客户端销毁物体的方法
        /// </summary>
        /// <param name="netId"></param>
        private static void Destroy(uint netId)
        {
        }

        /// <summary>
        /// 客户端下生成物体的事件
        /// </summary>
        /// <param name="message"></param>
        private static void SpawnByClient(SpawnMessage message)
        {
        }

        /// <summary>
        /// 客户端从服务器接收的Ping
        /// </summary>
        /// <param name="message"></param>
        private static void PongByClient(PongMessage message)
        {
            NetworkTime.OnClientPong();
        }

        /// <summary>
        /// 客户端下游戏对象开始生成的事件
        /// </summary>
        /// <param name="message"></param>
        private static void ObjectSpawnStartByClient(ObjectSpawnStartMessage message)
        {
        }

        /// <summary>
        /// 客户端下游戏对象生成完成的事件
        /// </summary>
        /// <param name="message"></param>
        private static void ObjectSpawnFinishByClient(ObjectSpawnFinishMessage message)
        {
        }

        /// <summary>
        /// 接收 远程过程调用(RPC) 缓存的事件
        /// </summary>
        /// <param name="message"></param>
        private static void RpcBufferMessage(RpcBufferMessage message)
        {
        }

        /// <summary>
        /// 客户端下当游戏对象权限改变的事件
        /// </summary>
        /// <param name="message"></param>
        private static void OnOwnerChanged(ChangeOwnerMessage message)
        {
        }

        /// <summary>
        /// 客户端下网络消息快照的事件
        /// </summary>
        /// <param name="message"></param>
        private static void OnSnapshotMessage(SnapshotMessage message)
        {
            NetworkSnapshot.OnTimeSnapshot(new TimeSnapshot(connection.timestamp, NetworkTime.localTime));
        }
    }
}