using JFramework.Interface;

namespace JFramework.Net
{
    /// <summary>
    /// 实体的抽象类
    /// </summary>
    public abstract class NetworkEntity : NetworkBehaviour, IEntity
    {
        /// <summary>
        /// 实体初始化注入
        /// </summary>
        protected virtual void Awake() => this.Inject();

        /// <summary>
        /// 实体销毁 (如果能获取到角色接口 则销毁角色的控制器)
        /// </summary>
        protected virtual void OnDestroy() => this.Destroy();
    }
}