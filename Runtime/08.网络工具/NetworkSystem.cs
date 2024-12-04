// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-12-04  16:12
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace JFramework.Net
{
    internal static class NetworkSystem
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
    }
}