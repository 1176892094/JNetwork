// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2023-12-29  00:37
// # Copyright: 2023, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UpdateFunction = UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction;

namespace JFramework.Net
{
    public class TimeManager : ScriptableObject
    {
        /// <summary>
        /// 上一次发送Ping的时间
        /// </summary>
        [SerializeField] private double sendTime;

        /// <summary>
        /// 当接收Ping
        /// </summary>
        public event Action<double> OnPingUpdate;

        /// <summary>
        /// 客户端回传往返时间
        /// </summary>
        [SerializeField] private NetworkAverage roundTripTime = new NetworkAverage(NetworkConst.PingWindow);

        /// <summary>
        /// 当前网络时间
        /// </summary>
        [ShowInInspector]
        internal double localTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Time.unscaledTimeAsDouble;
        }

        /// <summary>
        /// 
        /// </summary>
        [ShowInInspector]
        internal double fixedTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (NetworkManager.Instance)
                {
                    if (NetworkManager.Server.isActive)
                    {
                        return localTime;
                    }

                    if (NetworkManager.Client.connection != null)
                    {
                        return NetworkManager.Client.connection.localTimeline;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// 添加侦听
        /// </summary>
        internal void Init()
        {
            if (!Application.isPlaying) return;
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            AddLoopSystem(EarlyUpdate, ref playerLoop, typeof(EarlyUpdate));
            AddLoopSystem(AfterUpdate, ref playerLoop, typeof(PreLateUpdate));
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        /// <summary>
        /// Update之前的循环
        /// </summary>
        private void EarlyUpdate()
        {
            NetworkManager.Server.EarlyUpdate();
            NetworkManager.Client.EarlyUpdate();
        }

        /// <summary>
        /// Update之后的循环
        /// </summary>
        private void AfterUpdate()
        {
            NetworkManager.Server.AfterUpdate();
            NetworkManager.Client.AfterUpdate();
        }

        /// <summary>
        /// 在PlayerLoop中增加网络循环
        /// </summary>
        /// <param name="function">插入的方法</param>
        /// <param name="playerLoop">玩家循环</param>
        /// <param name="systemType">循环系统类型</param>
        /// <returns>返回能否添加循环</returns>
        private bool AddLoopSystem(UpdateFunction function, ref PlayerLoopSystem playerLoop, Type systemType)
        {
            if (playerLoop.type == systemType)
            {
                if (Array.FindIndex(playerLoop.subSystemList, system => system.updateDelegate == function) != -1)
                {
                    return true;
                }

                int oldLength = playerLoop.subSystemList?.Length ?? 0;
                Array.Resize(ref playerLoop.subSystemList, oldLength + 1);
                playerLoop.subSystemList[oldLength] = new PlayerLoopSystem
                {
                    type = typeof(NetworkManager),
                    updateDelegate = function
                };
                return true;
            }

            if (playerLoop.subSystemList != null)
            {
                for (int i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (AddLoopSystem(function, ref playerLoop.subSystemList[i], systemType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 客户端发送Ping消息到服务器端
        /// </summary>
        internal void Update()
        {
            if (localTime - sendTime >= NetworkConst.PingInterval)
            {
                var message = new PingMessage(localTime); // 传入客户端时间到服务器
                NetworkManager.Client.Send(message, Channel.Unreliable);
                sendTime = localTime;
            }
        }

        /// <summary>
        /// 客户端从服务器接收的回传信息
        /// </summary>
        /// <param name="clientTime"></param>
        internal void Ping(double clientTime)
        {
            roundTripTime.Calculate(localTime - clientTime);
            OnPingUpdate?.Invoke(roundTripTime.value);
        }

        /// <summary>
        /// 重置发送时间
        /// </summary>
        internal void Reset()
        {
            sendTime = 0;
            roundTripTime = new NetworkAverage(NetworkConst.PingWindow);
        }
    }
}