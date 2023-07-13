using System;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UpdateFunction = UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction;

namespace JFramework.Net
{
    internal static class NetworkLoop
    {
        /// <summary>
        /// 在PlayerLoop中增加网络循环
        /// </summary>
        /// <param name="function">插入的方法</param>
        /// <param name="playerLoop">玩家循环</param>
        /// <param name="systemType">循环系统类型</param>
        /// <returns>返回能否添加循环</returns>
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

        /// <summary>
        /// Update之前的循环
        /// </summary>
        private static void EarlyUpdate()
        {
            if (!Application.isPlaying) return;
            ServerManager.EarlyUpdate();
            ClientManager.EarlyUpdate();
        }

        /// <summary>
        /// Update之后的循环
        /// </summary>
        private static void AfterUpdate()
        {
            if (!Application.isPlaying) return;
            ServerManager.AfterUpdate();
            ClientManager.AfterUpdate();
        }
        
        /// <summary>
        /// 在游戏开始之前进行添加
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RuntimeInitializeOnLoad()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            AddPlayerLoop(EarlyUpdate, ref playerLoop, typeof(EarlyUpdate));
            AddPlayerLoop(AfterUpdate, ref playerLoop, typeof(PreLateUpdate));
            PlayerLoop.SetPlayerLoop(playerLoop);
        }
    }
}