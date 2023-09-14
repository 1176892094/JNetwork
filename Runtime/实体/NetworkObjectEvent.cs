using System;
using JFramework.Interface;
using UnityEngine;
// ReSharper disable All

namespace JFramework.Net
{
    public sealed partial class NetworkObject
    {
        /// <summary>
        /// 仅在客户端调用，当在客户端生成时调用
        /// </summary>
        internal void OnStartClient()
        {
            if (isSpawn) return;
            isSpawn = true;

            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStartClient)?.OnStartClient();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }

        /// <summary>
        /// 仅在客户端调用，当在客户端销毁时调用
        /// </summary>
        internal void OnStopClient()
        {
            if (!isSpawn) return;

            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStopClient)?.OnStopClient();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }

        /// <summary>
        /// 仅在服务器上调用，当在服务器生成时调用
        /// </summary>
        internal void OnStartServer()
        {
            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStartServer)?.OnStartServer();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }

        /// <summary>
        /// 仅在服务器上调用，当在服务器生成时调用
        /// </summary>
        internal void OnStopServer()
        {
            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStopServer)?.OnStopServer();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }

        /// <summary>
        /// 仅在客户端调用，触发Notify则进行权限认证
        /// </summary>
        internal void OnNotifyAuthority()
        {
            if (!isAuthority && isOwner)
            {
                OnStartAuthority();
            }
            else if (isAuthority && !isOwner)
            {
                OnStopAuthority();
            }

            isAuthority = isOwner;
        }

        /// <summary>
        /// 仅在客户端调用，当通过验证时调用
        /// </summary>
        private void OnStartAuthority()
        {
            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStartAuthority)?.OnStartAuthority();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }

        /// <summary>
        /// 仅在客户端调用，当停止验证时调用
        /// </summary>
        private void OnStopAuthority()
        {
            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStopAuthority)?.OnStopAuthority();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }
    }
}