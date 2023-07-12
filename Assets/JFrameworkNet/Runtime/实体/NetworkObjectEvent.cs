using System;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkObject
    {
        /// <summary>
        /// 仅在客户端调用，触发Notify则进行权限认证
        /// </summary>
        internal void OnNotifyAuthority()
        {
            if (!hasAuthority && isOwner)
            {
                OnStartAuthority();
            }
            else if (hasAuthority && !isOwner)
            {
                OnStopAuthority();
            }

            hasAuthority = isOwner;
        }

        /// <summary>
        /// 仅在客户端调用，当在客户端生成时调用
        /// </summary>
        internal void OnStartClient()
        {
            if (isStartClient) return;
            isStartClient = true;

            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStartClient>()?.OnStartClient();
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
            if (!isStartClient) return;

            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStopClient>()?.OnStopClient();
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
            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStartServer>()?.OnStartServer();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }

        /// <summary>
        /// 仅在客户端调用，当通过验证时调用
        /// </summary>
        private void OnStartAuthority()
        {
            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStartAuthority>()?.OnStartAuthority();
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
            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStopAuthority>()?.OnStopAuthority();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }
    }
}