using JFramework.Interface;

namespace JFramework.Net
{
    /// <summary>
    /// 实体的抽象类
    /// </summary>
    public abstract class NetworkEntity : NetworkBehaviour, IInject
    {
        /// <summary>
        /// 实体初始化注入
        /// </summary>
        protected virtual void Awake() => this.Inject();

        /// <summary>
        /// 实体启用
        /// </summary>
        protected virtual void OnEnable() => GetComponent<IUpdate>()?.Listen();

        /// <summary>
        /// 实体禁用
        /// </summary>
        protected virtual void OnDisable() => GetComponent<IUpdate>()?.Remove();

        /// <summary>
        /// 实体销毁 (如果能获取到角色接口 则销毁角色的控制器)
        /// </summary>
        protected virtual void OnDestroy() => gameObject.UnRegister();
    }
}