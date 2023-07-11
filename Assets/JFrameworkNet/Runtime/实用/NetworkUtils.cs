using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkUtils
    {
        /// <summary>
        /// 是场景物体
        /// </summary>
        public static bool IsSceneObject(NetworkObject entity)
        {
            var gameObject = entity.gameObject;
            if (entity.sceneId == 0) return false;
            return gameObject.hideFlags is not (HideFlags.HideAndDontSave or HideFlags.NotEditable);
        }

        /// <summary>
        /// 拥有有效的父物体
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidParent(NetworkObject entity)
        {
            var parent = entity.transform.parent;
            return parent == null || parent.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 获取网络对象
        /// </summary>
        /// <param name="netId">传入网络Id</param>
        /// <returns>返回网络对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkObject GetNetworkObject(uint netId)
        {
            if (NetworkServer.isActive)
            {
                NetworkServer.spawns.TryGetValue(netId, out var @object);
                return @object;
            }
            
            if (NetworkClient.isActive)
            {
                NetworkClient.spawns.TryGetValue(netId, out var @object);
                return @object;
            }

            return null;
        }
        
        /// <summary>
        /// 心跳判断
        /// </summary>
        /// <param name="current">当前时间</param>
        /// <param name="interval">时间间隔</param>
        /// <param name="lastTime">上次发送的时间</param>
        /// <returns>返回是否能够进行传输</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HeartTick(double current, double interval, ref double lastTime)
        {
            if (current < lastTime + interval) return false;
            var clientTime = (long)(current / interval);
            lastTime = clientTime * interval;
            return true;
        }
    }
}