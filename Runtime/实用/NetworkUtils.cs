using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkUtils
    {
        /// <summary>
        /// 是场景物体
        /// </summary>
        public static bool IsSceneObject(NetworkObject @object)
        {
            if (@object.sceneId == 0) return false;
            if (@object.gameObject.hideFlags == HideFlags.NotEditable) return false;
            return @object.gameObject.hideFlags != HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// 拥有有效的父物体
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidParent(NetworkObject @object)
        {
            var parent = @object.transform.parent;
            return parent == null || parent.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 获取网络对象
        /// </summary>
        /// <param name="objectId">传入网络Id</param>
        /// <returns>返回网络对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkObject GetNetworkObject(uint objectId)
        {
            if (NetworkServer.isActive)
            {
                NetworkServer.spawns.TryGetValue(objectId, out var @object);
                return @object;
            }

            if (NetworkClient.isActive)
            {
                NetworkClient.spawns.TryGetValue(objectId, out var @object);
                return @object;
            }

            return null;
        }

        /// <summary>
        /// 生成随机种子
        /// </summary>
        /// <returns></returns>
        internal static int GenerateRandom()
        {
            using var cryptoRandom = new RNGCryptoServiceProvider();
            var cryptoRandomBuffer = new byte[4];
            cryptoRandom.GetBytes(cryptoRandomBuffer);
            return Math.Abs(BitConverter.ToInt32(cryptoRandomBuffer, 0));
        }

        /// <summary>
        /// 心跳判断
        /// </summary>
        /// <param name="current">当前时间</param>
        /// <param name="sendRate">时间间隔</param>
        /// <param name="lastTime">上次发送的时间</param>
        /// <returns>返回是否能够进行传输</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TimeTick(double current, double sendRate, ref double lastTime)
        {
            if (current < lastTime + sendRate) return false;
            var clientTime = (long)(current / sendRate);
            lastTime = clientTime * sendRate;
            return true;
        }
    }
}