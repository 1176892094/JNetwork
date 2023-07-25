using System;
using JFramework.Core;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 实体的抽象类
    /// </summary>
    [Serializable]
    public abstract class NetworkEntity : NetworkBehaviour, IEntity
    {
        /// <summary>
        /// 实体Id
        /// </summary>
        private int Id;

        /// <summary>
        /// 实体销毁
        /// </summary>
        protected virtual void Despawn() { }

        /// <summary>
        /// 实体更新
        /// </summary>`
        protected virtual void OnUpdate() { }

        /// <summary>
        /// 实体启用
        /// </summary>
        protected virtual void OnEnable() => GlobalManager.Listen(this);

        /// <summary>
        /// 实体禁用
        /// </summary>
        protected virtual void OnDisable() => GlobalManager.Remove(this);

        /// <summary>
        /// 实体销毁
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                Despawn();
            }
            catch (Exception e)
            {
                Debug.Log( e.ToString());
            }
        }

        /// <summary>
        /// 实体Id
        /// </summary>
        int IEntity.Id
        {
            get => Id;
            set => Id = value;
        }

        /// <summary>
        /// 实体接口调用实体更新方法
        /// </summary>
        void IEntity.Update() => OnUpdate();
    }
}