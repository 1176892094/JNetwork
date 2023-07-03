using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;
        public static NetworkClient Client;
        public static NetworkServer Server;
        public static NetworkSceneManager Scene;
        public Address address => transport.address;
        [SerializeField] private Transport transport;
        [SerializeField] private bool runInBackground = true;
        [SerializeField] private int tickRate = 30;

        private void Awake()
        {
            if (!SetSingleton()) return;
            Client = new NetworkClient();
            Server = new NetworkServer();
            Scene = new NetworkSceneManager();
        }

        /// <summary>
        /// 设置单例
        /// </summary>
        /// <returns>返回是否设置成功</returns>
        private bool SetSingleton()
        {
            if (Instance != null && Instance == this)
            {
                return true;
            }

            if (transport == null)
            {
                if (TryGetComponent(out Transport newTransport))
                {
                    transport = newTransport;
                }
                else
                {
                    Debug.LogError("The NetworkManager has no Transport component.");
                    return false;
                }
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Transport.Instance = transport;
            Application.runInBackground = runInBackground;
            return true;
        }
        
        
        
        /// <summary>
        /// 运行初始化
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            Instance = null;
            Client = null;
            Server = null;
            Scene = null;
        }
    }
}