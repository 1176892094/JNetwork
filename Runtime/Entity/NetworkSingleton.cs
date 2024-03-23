using UnityEngine;

namespace JFramework.Net
{
    public abstract class NetworkSingleton<T> : NetworkBehaviour where T : NetworkSingleton<T>
    {
        private static readonly object locked = typeof(T);

        private static T instance;

        public static T Instance
        {
            get
            {
                if (!GlobalManager.Instance) return null;
                if (instance == null)
                {
                    lock (locked)
                    {
                        instance ??= FindObjectOfType<T>();
                        instance.Register();
                    }
                }

                return instance;
            }
        }

        private void Register()
        {
            if (instance == null)
            {
                instance = (T)this;
            }
            else if (instance != this)
            {
                Debug.LogWarning(typeof(T) + "单例重复！");
                Destroy(this);
            }
        }

        protected virtual void Awake() => Register();

        protected virtual void OnDestroy() => instance = null;
    }
}