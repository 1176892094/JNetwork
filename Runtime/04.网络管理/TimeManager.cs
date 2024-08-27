// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  01:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using JFramework;
using JFramework.Event;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace JFramework.Net
{
    internal partial class TimeManager : Component<NetworkManager>
    {
        private bool isActive;
        private double fixedTime;
        private double sinceTime;
        private double roundTripTime;

        internal void Update()
        {
            if (sinceTime + Const.PingInterval <= NetworkManager.TickTime)
            {
                sinceTime = NetworkManager.TickTime;
                var message = new PingMessage(NetworkManager.TickTime);
                NetworkManager.Client.connection.Send(message, Channel.Unreliable);
            }
        }

        internal void Ping(double clientTime)
        {
            if (!isActive)
            {
                isActive = true;
                fixedTime = 2.0 / (Const.PingWindow + 1);
                roundTripTime = NetworkManager.TickTime - clientTime;
            }
            else
            {
                var delta = NetworkManager.TickTime - clientTime - roundTripTime;
                roundTripTime += fixedTime * delta;
            }

            EventManager.Invoke(new OnPingUpdate(roundTripTime));
        }

        internal void Reset()
        {
            sinceTime = 0;
            roundTripTime = 0;
        }
    }

    internal partial class TimeManager
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            AddLoopSystem(EarlyUpdate, ref playerLoop, typeof(EarlyUpdate));
            AddLoopSystem(AfterUpdate, ref playerLoop, typeof(PreLateUpdate));
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        private static void EarlyUpdate()
        {
            if (!NetworkManager.Instance) return;
            NetworkManager.Server.EarlyUpdate();
            NetworkManager.Client.EarlyUpdate();
        }

        private static void AfterUpdate()
        {
            if (!NetworkManager.Instance) return;
            NetworkManager.Server.AfterUpdate();
            NetworkManager.Client.AfterUpdate();
        }

        private static bool AddLoopSystem(PlayerLoopSystem.UpdateFunction function, ref PlayerLoopSystem playerLoop, Type systemType)
        {
            if (playerLoop.type == systemType)
            {
                if (Array.FindIndex(playerLoop.subSystemList, system => system.updateDelegate == function) != -1)
                {
                    return true;
                }

                var oldLength = playerLoop.subSystemList?.Length ?? 0;
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

        internal static bool Ticks(float sendRate, ref double sendTime)
        {
            if (NetworkManager.TickTime >= sendTime + sendRate)
            {
                var fixedTime = (long)(NetworkManager.TickTime / sendRate);
                sendTime = fixedTime * sendRate;
                return true;
            }

            return false;
        }
    }
}