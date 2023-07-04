using System;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UpdateFunction = UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction;

namespace JFramework.Net
{
    internal static class NetworkLoop
    {
        private static bool AddPlayerLoop(UpdateFunction function, ref PlayerLoopSystem playerLoop, Type systemType)
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
                    type = typeof(NetworkLoop),
                    updateDelegate = function
                };
                return true;
            }

            if (playerLoop.subSystemList != null)
            {
                for (int i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (AddPlayerLoop(function, ref playerLoop.subSystemList[i], systemType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void EarlyUpdate()
        {
            if (!Application.isPlaying) return;
            NetworkServer.EarlyUpdate();
            NetworkClient.EarlyUpdate();
        }

        private static void AfterUpdate()
        {
            if (!Application.isPlaying) return;
            NetworkServer.AfterUpdate();
            NetworkClient.AfterUpdate();
        }
        
        public static void RuntimeInitializeOnLoad()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            AddPlayerLoop(EarlyUpdate, ref playerLoop, typeof(EarlyUpdate));
            AddPlayerLoop(AfterUpdate, ref playerLoop, typeof(PreLateUpdate));
            PlayerLoop.SetPlayerLoop(playerLoop);
        }
    }
}